using UnityEngine;
using UnityEngine.Rendering;

namespace Volumetrics.Settings
{
    //This struct is what gets passed to the shader from C#, it is declared separately so Unity will
    //auto generate the HLSL struct for us
    [System.Serializable]
    [GenerateHLSL(PackingRules.Exact, needAccessors = false)]
    public struct VolumetricCloudSettings
    {
        [Header("Global")]
        [Tooltip("How dense the clouds are, can also effect cloud coverage")] public float globalDensity;
        [Tooltip("Center point of the 2 spheres to draw the clouds in between")] public Vector3 cloudCenter;
        [Tooltip("Radius of the first sphere defining cloud bounds")] public float cloudStart;
        [Tooltip("Radius of the second sphere defining cloud bounds")] public float cloudEnd;
        [Tooltip("How much of the sphere to render, -1 = full sphere, > 0.5 = hemisphere")] [Range(-1, 1)] public float cloudCutoff;
        [Tooltip("How much of the sky do the clouds cover, 0 = no clouds, 1 = fully overcast")] [Range(0, 1)] public float globalCoverage;

        [Space(25.0f)] 
        [Header("Quality")] 
        [Tooltip("Step size while ray marching (if we hit a cloud), lower = higher quality, higher = more performant")] public float highDetailStepSize;
        [Tooltip("Step size while ray marching (if we haven't hit a cloud), lower = higher quality, higher = more performant")] public float lowDetailStepSize;
        [Tooltip("Step size while ray marching to determine lighting, lower = higher quality, higher = more performant")] public float lightingStepSize;
        
        [Space(25.0f)]
        [Header("Shaping")]
        [Tooltip("How zoomed in the shape noise is, lower = bigger clouds, higher = smaller clouds")] public float shapeNoiseUVScale;
        [Tooltip("How zoomed in the detail noise is (noise at the edges), lower = bigger clouds, higher = smaller clouds")] public float detailNoiseUVScale;
        
        [Tooltip("Offset of the shape noise, used to move the main cloud shapes")] public Vector2 shapeNoiseUVOffset;
        [Tooltip("Offset of the detail noise, used to cause edge distortions")] public Vector2 detailNoiseUVOffset;

        [Space(25.0f)]
        [Header("Lighting")] 
        [Tooltip("How much light energy the clouds absorb")] public float absorption;
        [Tooltip("The colour of the clouds when lit by the sun")] public Color cloudTint;

        [Tooltip("Extra intensity around the sun, used for dramatic scenes")] public float sunExtraIntensity;
        [Tooltip("How localized the intensity is, lower = more global, higher = more local")] public float sunExtraIntensityLocalization;
        [Tooltip("The amount of blending the clouds should do, lower = more transparent clouds, higher = more opaque clouds")] public float atmosphereBlending;
        [Tooltip("The distance from the center at which blending should start")] public float atmosphereBlendingCutoff;
        [Tooltip("How dark the tops of the clouds are, is more important when step lengths are very high")] [Range(0, 1)] public float powderAmount;
        [Tooltip("The amount of in scattering, ie silver lining")] [Range(0, 1)] public float inScattering;
        [Tooltip("The amount of out scattering, ie black")] [Range(0, 1)] public float outScattering;
        [Tooltip("Whether to use in, isotropic, or out scattering")] [Range(-1, 1)] public float inToOutScatteringInterpolation;

        [Tooltip("The minimum amount of shadowing possible by the sun")] [Range(0,1)] public float minimumShadowing;
    }
}