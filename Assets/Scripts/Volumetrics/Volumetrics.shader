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

    float density;
    float threshold;
    float scale;

    float R(float value, float low, float high, float newLow, float newHigh)
    {
        return newLow + (value - low) * (newHigh - newLow) / (high - low);
    }

    float ShapeAlteringHeight(float percentHeight, float maxHeightPercent)
    {
        float bottomRounding = clamp(R(percentHeight, 0.0f, 0.07f, 0.0f, 1.0f), 0.0f, 1.0f);
        float topRounding = clamp(R(percentHeight, maxHeightPercent * 0.2f, 
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
        
        float4 noiseSample = SAMPLE_TEXTURE3D(shapeNoise, sampler_TrilinearRepeat, position.xyz * scale);
        float4 detailSample = SAMPLE_TEXTURE3D(detailNoise, sampler_TrilinearRepeat, position.xyz * scale);
        float4 weatherMapSample = SAMPLE_TEXTURE2D(weatherMap, sampler_LinearRepeat, localCoordinates.xz);
        
        float fbm = noiseSample.y * 0.625f + noiseSample.z * 0.25f + noiseSample.w * 0.125f;
        float finalShape = R(noiseSample.x, fbm - 1, 1, 0, 1);
        
        float detailFBM = detailSample.x * 0.625f + detailSample.y * 0.25f + detailSample.z * 0.125f;

        float detailNoiseModification = 0.35f * exp(-threshold * 0.75f) * lerp(detailFBM, 1 - detailFBM,
            clamp(localCoordinates.y * 5.0f, 0, 1));
        
        float cloudProbability = max(weatherMapSample.x, clamp(threshold - 0.5f, 0, 1) * weatherMapSample.y * 2);
        float shapeAltering = ShapeAlteringHeight(localCoordinates.y, weatherMapSample.z);
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

        float totalDensity = 0;

        for (int i = 0; i < numVolumes; i++)
        {
            float3 boundsMin = volumeBounds[i].origin - volumeBounds[i].extents/2.0f;
            float3 boundsMax = volumeBounds[i].origin + volumeBounds[i].extents/2.0f;
            
            float2 intersectData = RayBoxIntersect(boundsMin, boundsMax, camPos, viewVector);

            float distanceTravelled = 0;
            float distanceLimit = min(depth - intersectData.x, intersectData.y);
            float stepSize = 0.1f;

            [unroll(200)]
            while (distanceTravelled < distanceLimit)
            {
                float3 rayPosition = camPos + viewVector * (distanceTravelled + intersectData.x);
                totalDensity += SampleDensity(rayPosition, volumeBounds[i].origin, volumeBounds[i].extents) * stepSize;
                distanceTravelled += stepSize;
            }
        }

        float transmittance = exp(-totalDensity);
        return SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord.xy) * transmittance;
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
