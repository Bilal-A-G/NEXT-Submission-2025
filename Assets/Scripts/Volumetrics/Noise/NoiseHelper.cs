using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Volumetrics.Noise
{
    public delegate void NoiseCompute(ComputeShader shader, Vector3Int textureDimensions,
        float contribution, int scale, Vector4 channel, ref RenderTexture target);
    
    //Represents a single fractal noise that will be packed into a single render
    //texture via the channel variable
    [System.Serializable]
    public class NoiseData
    {
        public int scale;
        public int octaves;
        public Vector4 channel;

        public NoiseType type;
        public ComputeShader shader;
        [Range(0, 1)] public float contribution;
    }
    
    //Represents a collection of fractal noises, all packed into a single render texture
    [System.Serializable]
    public class NoiseTextureData
    {
        public Vector3Int textureDimensions;
        public List<NoiseData> channelNoises;

        [HideInInspector] public RenderTexture textureOutput;
    }
    
    public enum NoiseType
    {
        Worley,
        Perlin
    }

    //This class holds various helper function to allow the noise generator to work
    //If you want to add more noise functions, then
    
    //1. Add an entry in the NoiseType enum
    //2. Add a case in the EnumToNoiseFunction function
    //3. Create a function that matches the signature of the delegate NoiseCompute
    //4. Return the created function in the EnumToNoiseFunction function
    public static class NoiseHelper
    {
        private static void DispatchComputeWorley(ComputeShader shader, Vector3Int textureDimensions, 
            float contribution, int scale, Vector4 channel, ref RenderTexture target)
        {
            Vector3Int tiles = textureDimensions / scale;
            
            Vector3[] points = new Vector3[tiles.x * tiles.y * tiles.z];
            for (int i = 0; i < tiles.x; i++)
            {
                for (int j = 0; j < tiles.y; j++)
                {
                    for (int k = 0; k < tiles.z; k++)
                    {
                        int flattenedIndex = k * tiles.x * tiles.y + j * tiles.x + i;

                        Vector3 center = new Vector3(i + 0.5f, j + 0.5f, k + 0.5f) * scale;
                        points[flattenedIndex] = center + new Vector3(
                            Random.Range(-scale / 2, scale / 2),
                            Random.Range(-scale / 2, scale / 2), 
                            Random.Range(-scale / 2, scale / 2));   
                    }
                }
            }

            ComputeBuffer pointsBuffer = new ComputeBuffer(tiles.x * tiles.y * tiles.z, Marshal.SizeOf(typeof(Vector3)));
            pointsBuffer.SetData(points);
            
            shader.SetBuffer(0, Shader.PropertyToID("points"), pointsBuffer);
            shader.SetTexture(0, Shader.PropertyToID("output"), target);
            shader.SetInts(Shader.PropertyToID("tiles"), new int[]{tiles.x, tiles.y, tiles.z});
            shader.SetInts(Shader.PropertyToID("imageDimensions"), new int[]{textureDimensions.x, 
                textureDimensions.y, textureDimensions.z});
            shader.SetInt(Shader.PropertyToID("tileSize"), scale);
            
            shader.SetVector(Shader.PropertyToID("channel"), channel);
            shader.SetFloat(Shader.PropertyToID("contribution"), contribution);
            
            shader.Dispatch(0, textureDimensions.x, textureDimensions.y, textureDimensions.z);
            
            pointsBuffer.Dispose();
        }
        
        private static void DispatchComputePerlin(ComputeShader shader, Vector3Int textureDimensions, 
            float contribution, int scale, Vector4 channel, ref RenderTexture target)
        {
            shader.SetTexture(0, Shader.PropertyToID("output"), target);
            shader.SetInt(Shader.PropertyToID("scale"), scale);
            shader.SetVector(Shader.PropertyToID("channel"), channel);
            shader.SetFloat(Shader.PropertyToID("contribution"), contribution);
            
            shader.Dispatch(0, textureDimensions.x, textureDimensions.y, textureDimensions.z);
        } 
        
        public static NoiseCompute EnumToNoiseFunction(NoiseType type)
        {
            switch (type)
            {
                case NoiseType.Worley:
                    return DispatchComputeWorley;
                case NoiseType.Perlin:
                    return DispatchComputePerlin;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, 
                        "Error, type is not associated with a noise function!");
            }
        }
    }
}