Shader "CustomEffects/Volumetrics"
{
    HLSLINCLUDE

    #pragma target 5.0
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
    #include "HLSLSupport.cginc"
    #include "Math.hlsl"

    sampler2D _CameraDepthTexture;
    float cloudStart;
    Texture2D<float4> Clouds;
    float2 imageSize;

    float4 Composite(Varyings input) : SV_Target
    {
        float4 originalColour = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord);
        float4 cloudColour = SAMPLE_TEXTURE2D(Clouds, sampler_LinearClamp, input.texcoord);
        
        float3 viewVector = mul(unity_CameraInvProjection, float4(input.texcoord * 2.0f - 1.0f, 0.0f, -1.0f)).xyz;
        viewVector = mul(unity_CameraToWorld, float4(viewVector.x, viewVector.y, viewVector.z, 0.0f)).xyz;
        
        float depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, input.texcoord), _ZBufferParams);
        float3 rayDirection = normalize(viewVector);
        float3 rayOrigin = _WorldSpaceCameraPos;
        float t = max(dot(-rayOrigin, rayDirection), 0);
        float3 p = rayOrigin + rayDirection * t;
        float y = length(p);

        float4 returnColour = originalColour;
        
        if(y < cloudStart)
        {
            float x = sqrt(pow(cloudStart, 2) - pow(y, 2));
            float intersect0 = t - x;
            float intersect1 = t + x;
            //If we can see the cloud start position at this point
            if(intersect1 < depth || intersect0 > 0)
            {
                returnColour = float4((cloudColour.w * cloudColour.xyz + (1 - cloudColour.w) * originalColour.xyz).xyz, 1.0f);
            }
        }
        
        return returnColour;
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
            #pragma fragment Composite;
            
            ENDHLSL
        }
    }
}
