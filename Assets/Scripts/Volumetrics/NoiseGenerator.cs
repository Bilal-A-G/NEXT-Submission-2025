using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace Volumetrics
{
    public class NoiseGenerator : MonoBehaviour
    {
        [SerializeField] private ComputeShader noiseShader;
        
        private RenderTexture _computeNoise;
        private ComputeBuffer _pointsBuffer;

        private async Task<float[][]> ReadBackGPU(int textureHeight, int textureWidth, int textureDepth)
        {
            AsyncGPUReadbackRequest req = await AsyncGPUReadback.RequestAsync(_computeNoise, 0, 
                0, textureWidth, 0, textureHeight, 0, textureDepth, TextureFormat.RFloat);
            
            float[][] data = new float[textureDepth][];
            for (int i = 0; i < textureDepth; i++)
            {
                data[i] = req.GetData<float>(i).ToArray();
            }
            
            return data;
        }
        
        public Task<float[][]> GenerateNoise(int textureHeight, int textureWidth, int textureDepth, int tileSize)
        {
            if (textureHeight % tileSize != 0 || textureWidth % tileSize != 0 || textureDepth % tileSize != 0)
            {
                Debug.LogError("Error, can't evenly divide texture into tiles of size " + tileSize + 
                               " pick a number that is divisible by the texture dimensions!");
                return null;
            }
            
            _computeNoise = new RenderTexture(textureWidth, textureHeight, 0, GraphicsFormat.R32_SFloat);
            _computeNoise.dimension = TextureDimension.Tex3D;
            _computeNoise.volumeDepth = textureDepth;
            _computeNoise.enableRandomWrite = true;
            _computeNoise.Create();
            
            int tilesX = textureWidth / tileSize;
            int tilesY = textureHeight / tileSize;
            int tilesZ = textureDepth / tileSize;
            
            Vector3[] points = new Vector3[tilesX * tilesY * tilesZ];
            for (int i = 0; i < tilesX; i++)
            {
                for (int j = 0; j < tilesY; j++)
                {
                    for (int k = 0; k < tilesZ; k++)
                    {
                        int flattenedIndex = k * tilesX * tilesY + j * tilesX + i;

                        Vector3 center = new Vector3(i + 0.5f, j + 0.5f, k + 0.5f) * tileSize;
                        points[flattenedIndex] = center + new Vector3(
                            Random.Range(-tileSize / 2, tileSize / 2),
                            Random.Range(-tileSize / 2, tileSize / 2), 
                            Random.Range(-tileSize / 2, tileSize / 2));   
                    }
                }
            }

            _pointsBuffer = new ComputeBuffer(tilesX * tilesY * tilesZ, Marshal.SizeOf(typeof(Vector3)));
            _pointsBuffer.SetData(points);
            
            noiseShader.SetBuffer(0, Shader.PropertyToID("points"), _pointsBuffer);
            noiseShader.SetTexture(0, Shader.PropertyToID("output"), _computeNoise);
            noiseShader.SetInt(Shader.PropertyToID("tilesX"), tilesX);
            noiseShader.SetInt(Shader.PropertyToID("tilesY"), tilesY);
            noiseShader.SetInt(Shader.PropertyToID("tilesZ"), tilesZ);
            noiseShader.SetInt(Shader.PropertyToID("tileSize"), tileSize);
            
            noiseShader.Dispatch(0, textureWidth, textureHeight, textureDepth);

            return ReadBackGPU(textureHeight, textureWidth, textureDepth);
        }

        private void OnDisable()
        {
            Destroy(_computeNoise);
            _pointsBuffer.Dispose();
        }
    }
}
