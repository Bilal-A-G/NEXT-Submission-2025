using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Random = UnityEngine.Random;

namespace Volumetrics
{
    public class NoiseGenerator : MonoBehaviour
    {
        [SerializeField] private ComputeShader noiseShader;
        [SerializeField] private int textureHeight;
        [SerializeField] private int textureWidth;
        [SerializeField] private int tileSize;
    
        private RenderTexture _computeNoise;
        private ComputeBuffer _pointsBuffer;
        public Texture2D _displayNoise;
        
        private void Awake()
        {
            System.DateTime startTime = DateTime.Now;
            GenerateNoise();
            TimeSpan elapsed = DateTime.Now - startTime;
            
            Debug.Log("Finished computing noise in " + elapsed.TotalMilliseconds + " ms");
        }

        public void GenerateNoise()
        {
            if (textureHeight % tileSize != 0 || textureWidth % tileSize != 0)
            {
                Debug.LogError("Error, can't evenly divide texture into tiles of size " + tileSize + 
                               " pick a number that is divisible by the texture dimensions!");
                return;
            }
            
            _computeNoise = new RenderTexture(textureWidth, textureHeight, 0, GraphicsFormat.R32_SFloat);
            _computeNoise.enableRandomWrite = true;
            _computeNoise.Create();

            _displayNoise = new Texture2D(textureWidth, textureHeight, TextureFormat.RFloat, false);

            int tilesX = textureWidth / tileSize;
            int tilesY = textureHeight / tileSize;
            
            Vector3[] points = new Vector3[tilesX * tilesY];
            for (int i = 0; i < tilesX; i++)
            {
                for (int j = 0; j < tilesY; j++)
                {
                    int flattenedIndex = i * tilesX + j;

                    Vector3 center = new Vector3(i * tileSize, j * tileSize, 0);
                    points[flattenedIndex] = center + new Vector3(Random.Range(-tileSize / 2, tileSize / 2),
                        Random.Range(-tileSize / 2, tileSize / 2), 0);
                }
            }

            _pointsBuffer = new ComputeBuffer(tilesX * tilesY, Marshal.SizeOf(typeof(Vector3)));
            _pointsBuffer.SetData(points);
            
            noiseShader.SetBuffer(0, Shader.PropertyToID("points"), _pointsBuffer);
            noiseShader.SetTexture(0, Shader.PropertyToID("output"), _computeNoise);
            noiseShader.SetInt(Shader.PropertyToID("tilesX"), tilesX);
            noiseShader.SetInt(Shader.PropertyToID("tilesY"), tilesY);
            noiseShader.SetInt(Shader.PropertyToID("tileSize"), tileSize);
            noiseShader.Dispatch(0, textureWidth, textureHeight, 1);

            RenderTexture.active = _computeNoise;
            _displayNoise.ReadPixels(new Rect(0, 0, textureWidth, textureHeight), 0, 0);
        
            _displayNoise.Apply();
        }

        private void OnDisable()
        {
            Destroy(_computeNoise);
            Destroy(_displayNoise);
            _pointsBuffer.Dispose();
        }
    }
}
