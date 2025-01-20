using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Volumetrics.Noise;

namespace Volumetrics
{
    //This class handles the generation of noise from a set of parameters
    public static class NoiseGenerator
    {
        //Computes several octaves of noise for a single channel
        private static void DispatchComputeOctaves(ref NoiseTextureData textureData)
        {
            int textureSize = (int)Mathf.Pow(2, textureData.powTwoExponentTextureSize);
            
            RenderTexture noise = new RenderTexture(textureSize, textureSize, 
                0, GraphicsFormat.R8G8B8A8_SNorm);
            noise.dimension = TextureDimension.Tex3D;
            noise.volumeDepth = textureSize;
            noise.enableRandomWrite = true;
            noise.Create();
            
            for (int i = 0; i < textureData.channelNoises.Count; i++)
            {
                NoiseData channelData = textureData.channelNoises[i];
                int noiseScale = (int)Mathf.Pow(2, channelData.powTwoExponentNoiseScale);
                
                if (textureSize % noiseScale != 0)
                {
                    throw new InvalidDataException("Error, cannot divide texture with dimensions " + 
                                                   textureSize + " int tiles of size " + noise);
                }
             
                for (int v = 0; v < channelData.octaves; v++)
                {
                    float contribution = Mathf.Pow(2, -v) * channelData.contribution;
                    int scale = (int)(noiseScale / Mathf.Pow(2, v));
                    
                    if (scale < 1)
                        scale = 1;
                    
                    NoiseHelper.EnumToNoiseFunction(channelData.type).Invoke(NoiseHelper.EnumToShader(channelData.type), 
                        textureSize, contribution, scale, 
                        NoiseHelper.NoiseChannelToVector(channelData.channel), ref noise);
                }
            }

            textureData.textureOutput = noise;
        }

        //Scale has to be a power of 2
        //Texture size has to be a power of 2 also
        //This is the only configuration that guarantees divisibility across all octaves 
        public static void GenerateNoise(NoiseTextureData[] textureConfigs)
        {
            for (int i = 0; i < textureConfigs.Length; i++)
            {
                DispatchComputeOctaves(ref textureConfigs[i]);
            }
        }
        
        //Texture size has to be a power of 2
        public static async Task<Vector4[][]> GPUReadBackFromTexture(Vector3Int dimensions, RenderTexture texture)
        {
            AsyncGPUReadbackRequest req = await AsyncGPUReadback.RequestAsync(texture, 0, 
                0, dimensions.x, 0, dimensions.y, 0, dimensions.z, TextureFormat.RGBAHalf);
            
            Vector4[][] data = new Vector4[dimensions.z][];
            for (int i = 0; i < dimensions.z; i++)
            {
                data[i] = req.GetData<Vector4>(i).ToArray();
            }
            
            return data;
        }
    }
}
