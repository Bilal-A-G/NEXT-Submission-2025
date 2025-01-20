using System.Collections.Generic;
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
        //Default values from Haggstrom's paper
        public NoiseTextureData shapeNoise = new NoiseTextureData(8, new List<NoiseData>()
        {
            new NoiseData(5, 1, NoiseChannel.Red, NoiseType.Worley, 0.7f),
            new NoiseData(5, 5, NoiseChannel.Red, NoiseType.Perlin, 0.2f),
            new NoiseData(4, 2, NoiseChannel.Green, NoiseType.Worley, 1.0f),
            new NoiseData(3, 2, NoiseChannel.Blue, NoiseType.Worley, 1.0f),
            new NoiseData(2, 2, NoiseChannel.Alpha, NoiseType.Worley, 1.0f)   
        });
        public NoiseTextureData detailNoise = new NoiseTextureData(5, new List<NoiseData>()
        {
            new NoiseData(4, 2, NoiseChannel.Red, NoiseType.Worley, 1.0f),
            new NoiseData(3, 2, NoiseChannel.Green, NoiseType.Worley, 1.0f),
            new NoiseData(2, 2, NoiseChannel.Blue, NoiseType.Worley, 1.0f),
        });

        [Space(25.0f)]
        [Header("Settings")]
        public VolumetricCloudSettings settings;
        
        public Texture2D BlueNoise => Resources.Load<Texture2D>("Noise/Textures/BlueNoise");
    }
}
