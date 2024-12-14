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
    
    Texture3D<float4> shapeNoise;
    Texture3D<float4> detailNoise;

    float density;
    float threshold;
    float scale;

    float rScale;
    float gScale;
    float bScale;
    float aScale;

    float R(float value, float low, float high, float newLow, float newHigh)
    {
        return newLow + (value - low) * (newHigh - newLow) / (high - low);
    }
    
    float SampleDensity(float3 position)
    {   
        float4 noiseSample = SAMPLE_TEXTURE3D(shapeNoise, sampler_TrilinearRepeat, position.xyz * scale);
        float fbm = noiseSample.y * gScale + noiseSample.z * bScale + noiseSample.w * aScale;

        float combinedSample = R(noiseSample.x, fbm - 1,1, 0, 1);
        return max((threshold - combinedSample) * density, 0);
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
            float stepSize = 0.01f;

            [unroll(300)]
            while (distanceTravelled < distanceLimit)
            {
                float3 rayPosition = camPos + viewVector * (distanceTravelled + intersectData.x);
                totalDensity += SampleDensity(rayPosition) * stepSize;
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
