using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Serialization;
using Volumetrics;
using Volumetrics.Noise;

public class Test : MonoBehaviour
{   
    [SerializeField] private Texture3D[] textures;
    [SerializeField] private NoiseTextureData[] noiseTextureConfigs;

    [SerializeField] private float brightness;
    [Range(0, 1)][SerializeField] private float invDensity;

    [SerializeField] private Texture2D myTex;
    [SerializeField] private Material testMat;
    
    async void Start()
    {
        NoiseGenerator.GenerateNoise(ref noiseTextureConfigs);
        textures = new Texture3D[noiseTextureConfigs.Length];

        myTex = new Texture2D(noiseTextureConfigs[0].textureDimensions.x, 
            noiseTextureConfigs[0].textureDimensions.y, TextureFormat.RGBAFloat, false);

        for (int i = 0; i < noiseTextureConfigs.Length; i++)
        { 
            NoiseTextureData currentConfig = noiseTextureConfigs[i];
            Vector3Int textureDimensions = currentConfig.textureDimensions;
           
            Texture3D texture = new Texture3D(textureDimensions.x, textureDimensions.y, 
                textureDimensions.z, TextureFormat.RGBAFloat, false);

            Vector4[][] data = await NoiseGenerator.GPUReadBackFromTexture(textureDimensions, currentConfig.textureOutput);
            
            for (int j = 0; j < textureDimensions.x; j++)
            {
                for (int k = 0; k < textureDimensions.y; k++)
                {
                    for (int l = 0; l < textureDimensions.z; l++)
                    {
                        Vector4 currentData = data[l][j * textureDimensions.y + k];
                        Vector4 processedData = new Vector4(currentData.x - invDensity, currentData.y - invDensity, 
                            currentData.z - invDensity, currentData.w - invDensity) * brightness;
                        
                        texture.SetPixel(j, k, l, new Color(processedData.x, processedData.x, 
                            processedData.x, 1));
                        
                        if(i != 0 || l != 0)
                            continue;
                        
                        myTex.SetPixel(j, k, new Color(processedData.x, 
                            processedData.x, processedData.x, 1.0f));
                    }
                }
            }
           
            texture.Apply();
            textures[i] = texture;
        }
        
        myTex.Apply();
        testMat.SetTexture("_BaseMap", myTex);
    }
}
