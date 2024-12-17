using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;
using Volumetrics;
using Volumetrics.Noise;
using Object = UnityEngine.Object;

namespace Rendering
{
    public struct VolumeBounds
    {
        public Vector3 origin;
        public Vector3 extents;

        public VolumeBounds(Vector3 origin, Vector3 extents)
        {
            this.origin = origin;
            this.extents = extents;
        }
    }
    
    public class VolumetricRenderFeature : ScriptableRendererFeature
    {
        [SerializeField] private Shader volumetricShader;
        [SerializeField] private NoiseTextureData[] noiseTextureConfigs;
        [SerializeField] private Texture2D blueNoise;

        [SerializeField] private float absorption;
        [SerializeField] [Range(0, 1)] private float attenuationClamp;
        [SerializeField] [Range(0, 1)] private float minimumAttenuationAmbient;
        [SerializeField] private float density;
        [SerializeField] [Range(0, 1)] private float coverage;
        [SerializeField] [Range(0, 1)] private float outScatteringAmbient;
        [SerializeField] private float uvScale;
        [SerializeField] private float uvDetailScale;
        [SerializeField] private float atmosphericBlending;

        [SerializeField] [Range(0, 1)] private float inScatter;
        [SerializeField] [Range(0, 1)] private float outScatter;
        [SerializeField] [Range(0, 1)] private float scatterLerp;
        [SerializeField] private float sunIntensity;
        [SerializeField] private float sunIntensityRadius;
        
        [SerializeField] private Texture2D weatherMap;

        private VolumetricRenderPass _renderPass;
        private VolumeDefinition[] _allVolumes;

        private ComputeBuffer _allBounds;
        private Material _material;
        
        public override void Create()
        {
            if(volumetricShader == null || !Application.isPlaying)
                return;

            if (_renderPass == null)
            {
                Debug.Log("Allocating");
                
                NoiseGenerator.GenerateNoise(ref noiseTextureConfigs);
                _allVolumes = FindObjectsByType<VolumeDefinition>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                _material = new Material(volumetricShader);
                _allBounds =  new ComputeBuffer(_allVolumes.Length, Marshal.SizeOf<VolumeBounds>());
            }
            
            _renderPass = new VolumetricRenderPass(_allBounds, ref _allVolumes, noiseTextureConfigs[0].textureOutput, 
                noiseTextureConfigs[1].textureOutput, density, coverage, uvScale, weatherMap, _material, 
                absorption, attenuationClamp, outScatteringAmbient, minimumAttenuationAmbient, uvDetailScale, blueNoise, 
                atmosphericBlending, inScatter, outScatter, scatterLerp, sunIntensity, sunIntensityRadius);
            _renderPass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
            
            Debug.Log("Finished creation");
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if(_renderPass == null)
                return;
            
            renderer.EnqueuePass(_renderPass);
        }

        protected override void Dispose(bool disposing)
        {
            if(Application.isPlaying || _allBounds == null || _material == null)
                return;
            
            _allBounds.Dispose();
            Object.DestroyImmediate(_material);
            
            Debug.Log("De allocating");
        }
    }
}