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
        public float cloudStart;
        public float cloudEnd;
        [Range(0, 1)] public float skyCurvature;
        public float drawDistance;
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

        public float atmosphereBlending;
        public float sunExtraIntensity;
        public float sunExtraIntensityLocalization;
        [Range(0,1)] public float minimumShadowing;
        [Range(0,1)] public float shadowDetail;
        [Range(0, 1)] public float powderAmount;
        [Range(0, 1)] public float inScattering;
        [Range(0, 1)] public float outScattering;
        [Range(0, 1)] public float inToOutScatteringInterpolation;
    }
}