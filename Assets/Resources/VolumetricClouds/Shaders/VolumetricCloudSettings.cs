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
        public float globalDensity;
        public Vector3 cloudCenter;
        public float cloudStart;
        public float cloudEnd;
        [Range(-1, 1)] public float cloudCutoff;
        [Range(0, 1)] public float globalCoverage;

        [Space(25.0f)]
        [Header("Shaping")]
        public float shapeNoiseUVScale;

        public float detailNoiseUVScale;
        public Vector2 shapeNoiseUVOffset;
        public Vector2 detailNoiseUVOffset;

        [Space(25.0f)]
        [Header("Lighting")] 
        public float absorption;
        public Color cloudTint;

        public float sunExtraIntensity;
        public float sunExtraIntensityLocalization;
        public float atmosphereBlending;
        public float atmosphereBlendingCutoff;
        [Range(0, 1)] public float powderAmount;
        [Range(0, 1)] public float inScattering;
        [Range(0, 1)] public float outScattering;
        [Range(-1, 1)] public float inToOutScatteringInterpolation;

        [Range(0,1)] public float minimumShadowing;
    }
}