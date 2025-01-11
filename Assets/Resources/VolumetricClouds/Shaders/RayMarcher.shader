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
    Texture2D<float4> weatherMap;

    float2 imageSize;

    float SampleDensity(float3 position, float percentHeight, VolumetricCloudSettings settings)
    {
        float4 noiseSample = SAMPLE_TEXTURE3D(shapeNoise, sampler_TrilinearRepeat,
            (position.xyz + float3(settings.shapeNoiseUVOffset.x, 0, settings.shapeNoiseUVOffset.y)) *
            settings.shapeNoiseUVScale);
        
        float4 detailSample = SAMPLE_TEXTURE3D(detailNoise, sampler_TrilinearRepeat,
            (position.xyz + float3(settings.detailNoiseUVOffset.x, 0, settings.detailNoiseUVOffset.y)) *
            settings.detailNoiseUVScale);
        
        //float4 weatherMapSample = weatherMap.SampleLevel(sampler_weatherMap, localCoordinates.xz, 0);
            
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
        float4 originalColour = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord);
        
        float3 viewVector = mul(unity_CameraInvProjection, float4(input.texcoord * 2.0f - 1.0f, 0.0f, -1.0f)).xyz;
        viewVector = mul(unity_CameraToWorld, float4(viewVector.x, viewVector.y, viewVector.z, 0.0f)).xyz;
        
        float depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, input.texcoord), _ZBufferParams);
        viewVector = normalize(viewVector);
        float3 startingCameraPosition = _WorldSpaceCameraPos;
        
        int sunSteps = 3;
        float transmittance = 1;
        float radiance = 0;
        float distanceFade = 0;
        float atmosphereFadeDistance = 0;
        
        float highDetailStepSize = 0.5f;
        float lowDetailStepSize = 2.0f;

        float highDetailDistanceTravelled = 0.0f;
        float stepSize = lowDetailStepSize;

        float4 blueNoiseOffset = SAMPLE_TEXTURE2D(blueNoise, sampler_LinearRepeat, input.texcoord);
        float distanceTravelled = 0;
        int stepsTaken = 0;
        float lightingStepSize = 2.0f;
        float distanceLimit = settings.cloudEnd - settings.cloudStart;
        
        float3 cameraPosition = startingCameraPosition + viewVector * settings.cloudStart + viewVector * (blueNoiseOffset * stepSize * 2).xyz;
        [loop]
        while (distanceTravelled < settings.cloudStart + settings.drawDistance)
        {
            float3 rayPosition = cameraPosition + viewVector * distanceTravelled;
            float percentHeight = saturate(((rayPosition.y - settings.cloudStart) / distanceLimit) +
                (((distanceTravelled / distanceLimit) * settings.skyCurvature)));
            float densityAtPoint = SampleDensity(rayPosition, percentHeight, settings) * stepSize;

            distanceFade += length(cameraPosition - rayPosition) * transmittance;
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
                if(atmosphereFadeDistance == 0)
                    atmosphereFadeDistance = length(startingCameraPosition - rayPosition);
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
                        R(percentHeight, 0.3f, 0.9f, 0.5, 1.0f))) *
                saturate(pow(abs(R(percentHeight, 0, 0.3f, 0.8f, 1.0f)), 0.8f));

            float dotAngle = dot(-_MainLightPosition, normalize(rayPosition - cameraPosition));
            float sunLocalIntensity = settings.sunExtraIntensity * pow(clamp(dotAngle, 0, 1), settings.sunExtraIntensityLocalization);
            float anisotropicScattering = lerp(max(HenyeyGreenstein(dotAngle, settings.inScattering), sunLocalIntensity),
                HenyeyGreenstein(dotAngle, -settings.outScattering), settings.inToOutScatteringInterpolation);
                
            radiance += densityAtPoint * transmittance * outScattering * shadow;
            transmittance *= exp(-densityAtPoint * settings.absorption);

            if(radiance >= 1.0f)
                break;
                
            if(transmittance <= 0.01f)
                break;    
        }

        float attenuatedTransmittance = clamp(exp(-atmosphereFadeDistance / (settings.atmosphereBlending * 100.0f)) *
            (1 - transmittance), 0, 1);
        attenuatedTransmittance = smoothstep(0, 1, attenuatedTransmittance);
        distanceFade/= distanceLimit;
        
        return float4((attenuatedTransmittance * settings.cloudTint * radiance + unity_AmbientSky * (1 - radiance)).xyz,
            attenuatedTransmittance);
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
