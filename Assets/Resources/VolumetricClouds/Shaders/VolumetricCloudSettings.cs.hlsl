//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef VOLUMETRICCLOUDSETTINGS_CS_HLSL
#define VOLUMETRICCLOUDSETTINGS_CS_HLSL
// Generated from Volumetrics.Settings.VolumetricCloudSettings
// PackingRules = Exact
struct VolumetricCloudSettings
{
    float globalDensity;
    float3 cloudCenter;
    float cloudStart;
    float cloudEnd;
    float drawDistance;
    float globalCoverage;
    float shapeNoiseUVScale;
    float detailNoiseUVScale;
    float2 shapeNoiseUVOffset;
    float2 detailNoiseUVOffset;
    float absorption;
    float4 cloudTint; // x: r y: g z: b w: a 
    float atmosphereBlending;
    float sunExtraIntensity;
    float sunExtraIntensityLocalization;
    float minimumShadowing;
    float shadowDetail;
    float powderAmount;
    float inScattering;
    float outScattering;
    float inToOutScatteringInterpolation;
};


#endif
