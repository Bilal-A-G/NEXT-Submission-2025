using UnityEngine;
using Volumetrics.Noise;

namespace Volumetrics.Settings
{
    //This class is a "profile", it drives the look of the volumetric clouds
    [CreateAssetMenu(menuName = "VolumetricClouds/Settings", fileName = "New Settings")]
    public class VolumetricCloudSettingsSo : ScriptableObject
    {
        public Shader RayMarcher => Resources.Load<Shader>("VolumetricClouds/Shaders/RayMarcher");
        public Shader Compositor => Resources.Load<Shader>("VolumetricClouds/Shaders/Compositor");
        
        [Space(25.0f)]
        [Header("Noise")]
        public NoiseTextureData shapeNoise;
        public NoiseTextureData detailNoise;

        [Space(25.0f)]
        [Header("Settings")]
        public VolumetricCloudSettings settings;
        
        public Texture2D BlueNoise => Resources.Load<Texture2D>("Noise/Textures/BlueNoise");
    }
}
