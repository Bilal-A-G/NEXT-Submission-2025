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
        
        return float4((cloudColour.w * cloudColour.xyz + (1 - cloudColour.w) * originalColour.xyz).xyz, 1.0f);
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
