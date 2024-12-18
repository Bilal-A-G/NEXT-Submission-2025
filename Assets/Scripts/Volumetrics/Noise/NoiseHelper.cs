using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Volumetrics.Noise
{
    public delegate void NoiseCompute(ComputeShader shader, int textureDimension,
        float contribution, int scale, Vector4 channel, ref RenderTexture target);
    
    //Represents a single fractal noise that will be packed into a single render
    //texture via the channel variable
    [System.Serializable]
    public class NoiseData
    {
        [FormerlySerializedAs("powTwoExponentScale")] public int powTwoExponentNoiseScale;
        public int octaves;
        public NoiseChannel channel;

        public NoiseType type;
        [Range(0, 1)] public float contribution;
    }

    [System.Serializable]
    public enum NoiseChannel
    {
        Red,
        Green,
        Blue,
        Alpha
    }
    
    //Represents a collection of fractal noises, all packed into a single render texture
    [System.Serializable]
    public class NoiseTextureData
    {
        [FormerlySerializedAs("powTwoExponentialTextureSize")] [FormerlySerializedAs("powTwoExponentTextureDimension")] public int powTwoExponentTextureSize;
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
    //5. If the noise requires a compute shader to work, add a new case in the EnumToShader function
    public static class NoiseHelper
    {
        private static void DispatchComputeWorley(ComputeShader shader, int textureSize, 
            float contribution, int scale, Vector4 channel, ref RenderTexture target)
        {
            int tiles = textureSize / scale;
            
            Vector3[] points = new Vector3[tiles * tiles * tiles];
            for (int i = 0; i < tiles; i++)
            {
                for (int j = 0; j < tiles; j++)
                {
                    for (int k = 0; k < tiles; k++)
                    {
                        int flattenedIndex = k * tiles * tiles + j * tiles + i;

                        Vector3 center = new Vector3(i + 0.5f, j + 0.5f, k + 0.5f) * scale;
                        points[flattenedIndex] = center + new Vector3(
                            Random.Range(-scale / 2.0f, scale / 2.0f),
                            Random.Range(-scale / 2.0f, scale / 2.0f), 
                            Random.Range(-scale / 2.0f, scale / 2.0f));   
                    }
                }
            }

            ComputeBuffer pointsBuffer = new ComputeBuffer(tiles * tiles * tiles, Marshal.SizeOf(typeof(Vector3)));
            pointsBuffer.SetData(points);
            
            shader.SetBuffer(0, Shader.PropertyToID("points"), pointsBuffer);
            shader.SetTexture(0, Shader.PropertyToID("output"), target);
            shader.SetInt(Shader.PropertyToID("tiles"), tiles);
            shader.SetInt(Shader.PropertyToID("tileSize"), scale);
            
            shader.SetVector(Shader.PropertyToID("channel"), channel);
            shader.SetFloat(Shader.PropertyToID("contribution"), contribution);
            
            shader.Dispatch(0, textureSize, 
                textureSize, textureSize);
            
            pointsBuffer.Dispose();
        }
        
        private static void DispatchComputePerlin(ComputeShader shader, int textureSize, 
            float contribution, int scale, Vector4 channel, ref RenderTexture target)
        {
            shader.SetTexture(0, Shader.PropertyToID("output"), target);
            shader.SetInt(Shader.PropertyToID("scale"), scale);
            shader.SetVector(Shader.PropertyToID("channel"), channel);
            shader.SetFloat(Shader.PropertyToID("contribution"), contribution);
            
            shader.SetFloat(Shader.PropertyToID("seed"), Random.Range(0.0f, 2.0f));
            shader.SetInt(Shader.PropertyToID("textureSize"), textureSize);
            
            shader.Dispatch(0, textureSize, textureSize, textureSize);
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

        public static ComputeShader EnumToShader(NoiseType type)
        {
            switch (type)
            {
                case NoiseType.Worley:
                    return Resources.Load<ComputeShader>("Noise/Shaders/WorleyNoiseGenerator");
                case NoiseType.Perlin:
                    return Resources.Load<ComputeShader>("Noise/Shaders/PerlinNoiseGenerator");
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, 
                        "Error, type is not associated with a noise shader!");
            }
        }

        public static Vector4 NoiseChannelToVector(NoiseChannel channel)
        {
            switch (channel)
            {
                case NoiseChannel.Red:
                    return new Vector4(1, 0, 0, 0);
                case NoiseChannel.Green:
                    return new Vector4(0, 1, 0, 0);
                case NoiseChannel.Blue:
                    return new Vector4(0, 0, 1, 0);
                case NoiseChannel.Alpha:
                    return new Vector4(0, 0, 0, 1);
                default:
                    throw new ArgumentOutOfRangeException(nameof(channel), channel, 
                        "Error, channel is not associated with a Vector4");
            }
        }
    }
}