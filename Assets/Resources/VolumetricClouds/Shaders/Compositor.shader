Shader "CustomEffects/Volumetrics"
{
    HLSLINCLUDE

    #pragma target 5.0
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
    
    Texture2D<float4> cloudAccumulation;
    SamplerState sampler_cloudAccumulation;
    
    float4 Composite(Varyings input) : SV_Target
    {
        float4 currentFrameContrib = SAMPLE_TEXTURE2D(cloudAccumulation, sampler_cloudAccumulation, input.texcoord.xy);
        return currentFrameContrib;
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
