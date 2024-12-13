using System.Linq;
using UnityEngine;
using Volumetrics;
using Volumetrics.Noise;

public class Test : MonoBehaviour
{   
    [SerializeField] private float[] brightnesses;
    [SerializeField] private Texture3D[] textures;
    [SerializeField] private NoiseTextureData[] noiseTextureConfigs;
    
    async void Start()
    {
       NoiseGenerator.GenerateNoise(ref noiseTextureConfigs);
       textures = new Texture3D[noiseTextureConfigs.Length];

       for (int i = 0; i < noiseTextureConfigs.Length; i++)
       { 
           NoiseTextureData currentConfig = noiseTextureConfigs[i];
           Vector3Int textureDimensions = currentConfig.textureDimensions;
           
           Texture3D texture = new Texture3D(textureDimensions.x, textureDimensions.y, 
               textureDimensions.z, TextureFormat.RGBAFloat, false);

           Vector4[][] data = await NoiseGenerator.GPUReadBackFromTexture(textureDimensions, currentConfig.textureOutput);

           Vector4 thresholds = new Vector4();
           foreach (NoiseData currentChannel in currentConfig.channelNoises)
           {
               thresholds += currentChannel.scale * currentChannel.channel;
           }

           thresholds /= 4.0f;
           
           for (int j = 0; j < textureDimensions.x; j++)
           {
               for (int k = 0; k < textureDimensions.y; k++)
               {
                   for (int l = 0; l < textureDimensions.z; l++)
                   {
                       Vector4 currentData = data[l][j * textureDimensions.y + k];
                       Vector4 processedData = new Vector4(thresholds.x - currentData.x, thresholds.y - currentData.y, 
                           thresholds.z - currentData.z, thresholds.w - currentData.w) * brightnesses[i];
                       
                       texture.SetPixel(j, k, l, new Color(processedData.y, processedData.y, 
                           processedData.y, 1));
                   }
               }
           }
           
           texture.Apply();
           textures[i] = texture;
       }
    }
}
