using UnityEngine;
using Volumetrics.Noise;

namespace Volumetrics.Settings
{
    //This class is a "profile", it drives the look of the volumetric clouds
    [CreateAssetMenu(menuName = "VolumetricClouds/Settings", fileName = "New Settings")]
    public class VolumetricCloudSettingsSo : ScriptableObject
    {
        public Shader Compositor => Resources.Load<Shader>("VolumetricClouds/Shaders/Compositor");
        public ComputeShader RayMarcher => Resources.Load<ComputeShader>("VolumetricClouds/Shaders/RayMarcher");
        public Texture2D BlueNoise => Resources.Load<Texture2D>("Noise/Textures/BlueNoise");

        [Header("Global")]
        public Texture2D weatherMap;
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
        [Header("Noise")]
        public NoiseTextureData shapeNoise;
        public NoiseTextureData detailNoise;


        [Space(25.0f)]
        [Header("Lighting")] 
        public float absorption;
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
