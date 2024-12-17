Shader "CustomEffects/Volumetrics"
{
    HLSLINCLUDE

    #pragma target 5.0
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

    struct Bounds
    {
        float3 origin;
        float3 extents;
    };

    float2 RayBoxIntersect(float3 boundsMin, float3 boundsMax, float3 rayOrigin, float3 rayDirection)
    {
        float3 t0 = (boundsMin - rayOrigin)/rayDirection;
        float3 t1 = (boundsMax - rayOrigin)/rayDirection;

        float3 tMin = min(t0, t1);
        float3 tMax = max(t0, t1);

        float dstA = max(max(tMin.x, tMin.y), tMin.z); 
        float dstB = min(tMax.x, min(tMax.y, tMax.z));

        float dstToBox = max(0, dstA);
        float dstInsideBox = max(0, dstB - dstToBox);

        return float2(dstToBox, dstInsideBox);
    }
    
    StructuredBuffer<Bounds> volumeBounds;
    Texture2D<float4> _CameraDepthTexture;
    Texture2D<float4> weatherMap;
    
    Texture3D<float4> shapeNoise;
    Texture3D<float4> detailNoise;
    Texture2D<float4> blueNoise;

    float density;
    float threshold;
    float scale;
    float detailScale;
    float absorption;
    float attenuationClamp;
    float minimumAttenuationAmbient;
    float outScatteringAmbient;

    float sunIntensity;
    float sunIntensityRadius;
    float inScatter;
    float outScatter;
    float scatterLerp;

    float xOffset;
    float zOffset;
    float detailXOffset;
    float detailZOffset;

    float atmosphericBlending;
    
    float R(float value, float low, float high, float newLow, float newHigh)
    {
        return newLow + (value - low) * (newHigh - newLow) / (high - low);
    }

    float HenyeyGreenstein(float dotAngle, float g)
    {
        return 1.0f/(4.0f * PI) * ((1.0f - pow(g, 2.0f)) /
            pow(1.0f + pow(g, 2.0f) - g * 2.0f * cos(dotAngle), 3.0f/2.0f));
    }

    float ShapeAlteringHeight(float percentHeight, float maxHeightPercent)
    {
        float bottomRounding = clamp(R(percentHeight, 0.0f, 0.07f, 0.0f, 1.0f), 0.0f, 1.0f);
        float topRounding = clamp(R(percentHeight, maxHeightPercent * 0.1f, 
                maxHeightPercent, 1, 0), 0.0f, 1.0f);

        return bottomRounding * topRounding;
    }

    float DensityAlteringHeight(float percentHeight, float localDensity, float globalDensity)
    {
        float bottomDensity = percentHeight * clamp(R(percentHeight, 0,
            0.15f, 0, 1),0.0f, 1.0f);
        float topDensity = clamp(R(percentHeight, 0.9f, 1, 1, 0), 0.0f, 1.0f);

        return globalDensity * bottomDensity * topDensity * localDensity * 2;
    }
    
    float SampleDensity(float3 position, float3 boundsOrigin, float3 boundsExtents)
    {
        float3 localCoordinates = (position.xyz - boundsOrigin)/boundsExtents * 2;
        localCoordinates = float3(
            R(localCoordinates.x, -1, 1, 0, 1),
            R(localCoordinates.y, -1, 1, 0, 1),
            R(localCoordinates.z, -1, 1, 0, 1));
        
        float4 noiseSample = SAMPLE_TEXTURE3D_LOD(shapeNoise, sampler_TrilinearRepeat,
            (position.xyz + float3(xOffset, 0, zOffset)) * scale, 0);
        float4 detailSample = SAMPLE_TEXTURE3D_LOD(detailNoise, sampler_TrilinearRepeat,
            (position.xyz + float3(detailXOffset, 0, detailZOffset)) * detailScale, 0);
        float4 weatherMapSample = SAMPLE_TEXTURE2D_LOD(weatherMap, sampler_LinearRepeat, localCoordinates.xz, 0);
        
        float fbm = noiseSample.y * 0.625f + noiseSample.z * 0.25f + noiseSample.w * 0.125f;
        float finalShape = R(noiseSample.x, fbm - 1, 1, 0, 1);
        
        float detailFBM = detailSample.x * 0.625f + detailSample.y * 0.25f + detailSample.z * 0.125f;

        float detailNoiseModification = 0.35f * exp(-threshold * 0.75f) * lerp(detailFBM, 1 - detailFBM,
            clamp(localCoordinates.y * 5.0f, 0, 1));
        
        float cloudProbability = max(weatherMapSample.x, clamp(threshold - 0.5f, 0, 1) * weatherMapSample.y * 2);
        float shapeAltering = ShapeAlteringHeight(localCoordinates.y, 1.0f);
        float densityAltering = DensityAlteringHeight(localCoordinates.y, weatherMapSample.w, density);
        
        float finalSample = clamp(R(finalShape * shapeAltering, 1 - threshold * cloudProbability,
            1, 0, 1), 0, 1);
        float finalDensity = clamp(R(finalSample, detailNoiseModification,
            1, 0, 1), 0, 1) * densityAltering;

        return max(finalDensity - 0.6f, 0);
    }

    float4 ComputeVolumetrics(Varyings input) : SV_Target
    {
        float3 viewVector = mul(unity_CameraInvProjection, float4(input.texcoord * 2 - 1, 0, -1));
        viewVector = mul(unity_CameraToWorld, float4(viewVector,0));

        float3 camPos = _WorldSpaceCameraPos;
        
        uint numVolumes;
        uint _;
        volumeBounds.GetDimensions(numVolumes, _);

        float nonLinearDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,sampler_LinearClamp, input.texcoord.xy);
        float depth = LinearEyeDepth(nonLinearDepth, _ZBufferParams);
        
        int sunSteps = 3;
        
        float transmittance = 1;
        float radiance = 0;
        float distanceFade = 0;
    
        float highDetailStepSize = 1.0f;
        float lowDetailStepSize = 3.0f;

        float highDetailDistanceTravelled = 0.0f;
        float stepSize = lowDetailStepSize;

        float blueNoiseOffset = SAMPLE_TEXTURE2D(blueNoise, sampler_TrilinearRepeat, input.texcoord.xy * 1.1f);
        float distanceTravelled = 0;

        for (int i = 0; i < numVolumes; i++)
        {
            float3 boundsOrigin = volumeBounds[i].origin;
            float3 boundsExtents = volumeBounds[i].extents;
            
            float3 boundsMin = boundsOrigin - boundsExtents/2.0f;
            float3 boundsMax = boundsOrigin + boundsExtents/2.0f;
            
            float2 intersectData = RayBoxIntersect(boundsMin, boundsMax, camPos, viewVector);
            float lightingStepSize = 1.0f;

            float distanceLimit = min(depth - intersectData.x, intersectData.y);

            camPos += viewVector * ((blueNoiseOffset - 0.5f) * 2 * stepSize);
            
            [loop]
            while (distanceTravelled < distanceLimit)
            {
                float3 rayPosition = camPos + viewVector * (distanceTravelled + intersectData.x);
                float3 localCoordinates = (rayPosition.xyz - boundsOrigin)/boundsExtents * 2;
                localCoordinates = float3(
                    R(localCoordinates.x, -1, 1, 0, 1),
                    R(localCoordinates.y, -1, 1, 0, 1),
                    R(localCoordinates.z, -1, 1, 0, 1));
                
                float densityAtPoint = SampleDensity(rayPosition, boundsOrigin, boundsExtents) * stepSize;
                distanceTravelled += stepSize;
                if(stepSize == highDetailStepSize)
                    highDetailDistanceTravelled += highDetailStepSize;
                
                if(densityAtPoint > 0.0f &&
                    stepSize == lowDetailStepSize)
                {
                     distanceTravelled -= stepSize * 2;
                     stepSize = highDetailStepSize;
                     highDetailDistanceTravelled = 0.0f;
                     distanceFade = length(camPos - rayPosition);
                    
                     continue;
                }
                if (densityAtPoint <= 0.0f && highDetailDistanceTravelled > highDetailStepSize * 10.0f &&
                    stepSize == highDetailStepSize)
                {
                    stepSize = lowDetailStepSize;
                    continue;
                }

                if(densityAtPoint <= 0)
                    continue;

                float3 lightDirection = normalize(_MainLightPosition.xyz);

                float toSunDensity = 0.0f;
                for (int v = 0; v < sunSteps; v++)
                {
                    rayPosition += lightDirection * lightingStepSize;
                    toSunDensity += SampleDensity(rayPosition, boundsOrigin, boundsExtents) * lightingStepSize;

                    if(toSunDensity >= 1.0f)
                        break;
                }
                
                float shadow = clamp(exp(-toSunDensity), attenuationClamp, 1.0f);
                shadow = max(toSunDensity * minimumAttenuationAmbient, shadow);
                float outScattering = 1 - saturate(outScatteringAmbient * 2 *
                    pow(densityAtPoint * (1 - transmittance), R(localCoordinates.y, 0.3f, 0.9f, 0.5, 1.0f))) *
                        saturate(pow(R(localCoordinates.y, 0, 0.3f, 0.8f, 1.0f), 0.8f));

                float dotAngle = dot(lightDirection, normalize(rayPosition - camPos));
                float sunLocalIntensity = sunIntensity * pow(clamp(dotAngle, 0, 1), sunIntensityRadius);
                float anisotropicScattering = lerp(max(HenyeyGreenstein(dotAngle, inScatter), sunLocalIntensity),
                    HenyeyGreenstein(dotAngle, -outScatter), scatterLerp);
                
                radiance += densityAtPoint * transmittance * outScattering * shadow * anisotropicScattering;
                transmittance *= exp(-densityAtPoint * absorption);

                if(radiance >= 1.0f)
                    break;
                
                if(transmittance <= 0.01f)
                    break;
            }
        }

        float4 ambient = unity_AmbientSky + unity_AmbientGround + unity_AmbientEquator;
        float attenuatedTransmittance = clamp(exp(-distanceFade / (atmosphericBlending * 100)) *
            (1 - transmittance), 0, 1);
        attenuatedTransmittance = Smootherstep01(attenuatedTransmittance);
        
        return SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord.xy) * (1 - attenuatedTransmittance) +
          attenuatedTransmittance * _MainLightColor * radiance + ambient * (1 - radiance);
    }
    
    ENDHLSL

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        ZWrite Off Cull Off
        
        Pass
        {
            Name "Volumetric Pass"

            HLSLPROGRAM
            
            #pragma vertex Vert;
            #pragma fragment ComputeVolumetrics;
            
            ENDHLSL
        }
    }
}
