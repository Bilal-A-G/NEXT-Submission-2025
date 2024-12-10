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
        
        for (int i = 0; i < numVolumes; i++)
        {
            float3 boundsMin = volumeBounds[i].origin - volumeBounds[i].extents/2.0f;
            float3 boundsMax = volumeBounds[i].origin + volumeBounds[i].extents/2.0f;
            
            float2 intersectData = RayBoxIntersect(boundsMin, boundsMax, camPos, viewVector);
            if(intersectData.y > 0 && depth > intersectData.x)
            {
                return float4(0, 0, 0, 1);
            }
        }
        
        return float4(SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord).rgb, 1.0f);
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
