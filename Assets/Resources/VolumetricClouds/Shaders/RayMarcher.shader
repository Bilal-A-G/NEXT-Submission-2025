Shader "CustomEffects/Volumetrics"
{
    HLSLINCLUDE

    #pragma target 5.0
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
    #include "HLSLSupport.cginc"
    #include "Math.hlsl"
    #include "VolumetricCloudSettings.cs.hlsl"

    sampler2D _CameraDepthTexture;
    
    StructuredBuffer<VolumetricCloudSettings> settingsArray;
    
    Texture3D<float4> shapeNoise;
    Texture3D<float4> detailNoise;
    Texture2D<float4> blueNoise;

    float2 imageSize;

    float SampleDensity(float3 position, float percentHeight, VolumetricCloudSettings settings)
    {
        float4 noiseSample = SAMPLE_TEXTURE3D_LOD(shapeNoise, sampler_TrilinearRepeat,
            (position.xyz + float3(settings.shapeNoiseUVOffset.x, 0, settings.shapeNoiseUVOffset.y)) *
            settings.shapeNoiseUVScale, 0);
        
        float4 detailSample = SAMPLE_TEXTURE3D_LOD(detailNoise, sampler_TrilinearRepeat,
            (position.xyz + float3(settings.detailNoiseUVOffset.x, 0, settings.detailNoiseUVOffset.y)) *
            settings.detailNoiseUVScale, 0);
        
        float fbm = noiseSample.y * 0.625f + noiseSample.z * 0.25f + noiseSample.w * 0.125f;
        float finalShape = R(noiseSample.x, fbm - 1, 1, 0, 1);
            
        float detailFBM = detailSample.x * 0.625f + detailSample.y * 0.25f + detailSample.z * 0.125f;

        float detailNoiseModification = 0.35f * exp(-settings.globalCoverage * 0.75f) * lerp(detailFBM, 1 - detailFBM,
            clamp(percentHeight * 5.0f, 0, 1));
            
        //float cloudProbability = max(weatherMapSample.x, clamp(threshold - 0.5f, 0, 1) * weatherMapSample.y * 2);
        float shapeAltering = ShapeAlteringHeight(percentHeight, 1.0f);
        float densityAltering = DensityAlteringHeight(percentHeight, 1.0f, settings.globalDensity);
            
        float finalSample = clamp(R(finalShape * shapeAltering, 1 - settings.globalCoverage,
            1, 0, 1), 0, 1);
        float finalDensity = clamp(R(finalSample, detailNoiseModification,
            1, 0, 1), 0, 1) * densityAltering;

        return max(finalDensity - 0.6f, 0);
    }
    
    float4 RayMarch(Varyings input) : SV_Target
    {
        VolumetricCloudSettings settings = settingsArray[0];
        float3 viewVector = mul(unity_CameraInvProjection, float4(input.texcoord * 2.0f - 1.0f, 0.0f, -1.0f)).xyz;
        viewVector = mul(unity_CameraToWorld, float4(viewVector.x, viewVector.y, viewVector.z, 0.0f)).xyz;
        viewVector = normalize(viewVector);

        float2 cloudStartIntersection = RaySphereIntersect(_WorldSpaceCameraPos, viewVector,
            settings.cloudCenter, settings.cloudStart);
        float2 cloudEndIntersection = RaySphereIntersect(_WorldSpaceCameraPos, viewVector,
            settings.cloudCenter, settings.cloudEnd);
        
        float masterIntersectionPoint;
        //We are looking at the clouds from outside of the cloud end
        if(cloudEndIntersection.x >= 0)
        {
            masterIntersectionPoint = cloudEndIntersection.x;
        }
        //We are looking at the clouds from the ground (inside the cloud start)
        else if (cloudStartIntersection.y >= 0 && cloudStartIntersection.x < 0)
        {
            masterIntersectionPoint = cloudStartIntersection.y;
        }
        //We are in between the cloud start and cloud end
        else
        {
            masterIntersectionPoint = 0;
        }
        
        int sunSteps = 3;
        float transmittance = 1;
        float radiance = 0;
        float cloudDepth = 0;
        
        float highDetailStepSize = 1.0f;
        float lowDetailStepSize = 3.0f;

        float highDetailDistanceTravelled = 0.0f;
        float stepSize = lowDetailStepSize;

        float4 blueNoiseOffset = SAMPLE_TEXTURE2D(blueNoise, sampler_LinearRepeat, input.texcoord);
        float distanceTravelled = 0;
        int stepsTaken = 0;
        float lightingStepSize = 2.0f;
        
        float distanceLimit = cloudEndIntersection.y;
        float3 cameraPosition = _WorldSpaceCameraPos + viewVector * (masterIntersectionPoint + blueNoiseOffset.x * stepSize * 2);

        [loop]
        while (distanceTravelled < distanceLimit)
        {
            float3 rayPosition = cameraPosition + viewVector * distanceTravelled;
            float percentInsideCloudLayer = R(length(rayPosition - settings.cloudCenter),settings.cloudStart,
                 settings.cloudEnd, 0, 1);

            //Only render a hemisphere, or a full sphere depending on cutoff settings
            if(dot(normalize(rayPosition - settings.cloudCenter), float3(0, 1, 0)) <= settings.cloudCutoff)
                break;
            
            float densityAtPoint = SampleDensity(rayPosition, percentInsideCloudLayer, settings) * stepSize;
            stepsTaken++;
            
            distanceTravelled += stepSize;
            if(stepSize == highDetailStepSize)
                highDetailDistanceTravelled += highDetailStepSize;
                
            if(densityAtPoint > 0.0f &&
                stepSize == lowDetailStepSize)
            {
                distanceTravelled -= stepSize * 2;
                stepSize = highDetailStepSize;
                highDetailDistanceTravelled = 0.0f;
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

            if(cloudDepth == 0)
                cloudDepth = length(_WorldSpaceCameraPos - rayPosition);
            
            float toSunDensity = 0.0f;
            for (int v = 0; v < sunSteps; v++)
            {
                rayPosition += -_MainLightPosition * lightingStepSize;
                float toSunPercentHeight = rayPosition.y / 500.0f;
                toSunDensity += SampleDensity(rayPosition, toSunPercentHeight, settings) * lightingStepSize;

                if(toSunDensity >= 1.0f)
                    break;
            }
            
            float shadow = clamp(exp(-toSunDensity), settings.minimumShadowing, 1.0f);
            shadow = max(toSunDensity * settings.shadowDetail, shadow);
            float outScattering = 1 - saturate(settings.powderAmount * 2 *
                    pow(abs(densityAtPoint * (1 - transmittance)),
                        R(percentInsideCloudLayer, 0.3f, 0.9f, 0.5, 1.0f))) *
                saturate(pow(abs(R(percentInsideCloudLayer, 0, 0.3f, 0.8f, 1.0f)), 0.8f));

            float dotAngle = dot(-_MainLightPosition, normalize(rayPosition - cameraPosition));
            float sunLocalIntensity = settings.sunExtraIntensity * pow(clamp(dotAngle, 0, 1), settings.sunExtraIntensityLocalization);
            float anisotropicScattering = lerp(max(HenyeyGreenstein(dotAngle, settings.inScattering), sunLocalIntensity),
                HenyeyGreenstein(dotAngle, -settings.outScattering), settings.inToOutScatteringInterpolation);

            transmittance *= exp(-densityAtPoint * settings.absorption);
            radiance += densityAtPoint * transmittance * outScattering * shadow;
            
            if(radiance >= 1.0f)
                break;
                
            if(transmittance <= 0.01f)
                break;    
        }

        float attenuatedTransmittance = clamp(1 - transmittance, 0, 1);
        attenuatedTransmittance = smoothstep(0, 1, attenuatedTransmittance);
        
        return float4(radiance, cloudDepth, attenuatedTransmittance, 0);
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
            #pragma fragment RayMarch;
            
            ENDHLSL
        }
    }
}
