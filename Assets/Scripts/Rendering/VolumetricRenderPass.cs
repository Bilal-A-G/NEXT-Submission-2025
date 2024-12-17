using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;
using Volumetrics;

namespace Rendering
{
    public class VolumetricRenderPass : ScriptableRenderPass
    {
        private RenderTextureDescriptor _textureDescriptor;
        private ComputeBuffer _allBounds;
        
        private RenderTexture _shapeNoise;
        private RenderTexture _detailNoise;

        private Material _material;

        private float _density;
        private float _threshold;
        private float _scale;
        private float _detailScale;

        private Texture2D _weatherMap;
        private VolumeDefinition[] _allVolumes;
        private float _absorption;
        private float _attenuationClamp;
        private float _outScatteringAmbient;
        private float _minimumAttenuationAmbient;

        private Texture2D _blueNoise;
        private float _atmosphericBlending;
        
        private float _inScatter;
        private float _outScatter;
        private float _scatterLerp;
        private float _sunIntensity;
        private float _sunIntensityRadius;
        
        public VolumetricRenderPass(ComputeBuffer allBounds, ref VolumeDefinition[] allVolumes, RenderTexture shapeNoise, 
            RenderTexture detailNoise, float density, float threshold, 
            float scale, Texture2D weatherMap, Material material, float absorption, float attenuationClamp, 
            float outScatteringAmbient, float minimumAttenuationAmbient, float detailScale, Texture2D blueNoise, float atmosphericBlending,
            float inScatter, float outScatter, float scatterLerp, float sunIntensity, float sunIntensityRadius)
        {
            if(!Application.isPlaying)
                return;
            
            _textureDescriptor = new RenderTextureDescriptor(Screen.width, 
                Screen.height, RenderTextureFormat.Default, 0);

            _textureDescriptor.enableRandomWrite = true;
            
            _allBounds = allBounds;
            _allVolumes = allVolumes;
            
            _material = material;   

            _shapeNoise = shapeNoise;
            _detailNoise = detailNoise;

            _density = density;
            _threshold = threshold;
            _scale = scale;
            _detailScale = detailScale;

            _weatherMap = weatherMap;

            _absorption = absorption;
            _attenuationClamp = attenuationClamp;
            _outScatteringAmbient = outScatteringAmbient;
            _minimumAttenuationAmbient = minimumAttenuationAmbient;

            _blueNoise = blueNoise;
            _atmosphericBlending = atmosphericBlending;

            _inScatter = inScatter;
            _outScatter = outScatter;
            _scatterLerp = scatterLerp;
            _sunIntensity = sunIntensity;
            _sunIntensityRadius = sunIntensityRadius;
        }

        private void UpdateSettings()
        {
            VolumeBounds[] allBounds = new VolumeBounds[_allVolumes.Length];
            for (int i = 0; i < _allVolumes.Length; i++)
            {
                Transform currentTransform = _allVolumes[i].transform;
                allBounds[i] = new VolumeBounds(currentTransform.position, currentTransform.localScale);
            }
            
            if(allBounds.Length == 0)
                return;
            
            if(_allBounds == null)
                return;
            
            _allBounds.SetData(allBounds);
            _material.SetBuffer(Shader.PropertyToID("volumeBounds"), _allBounds);
            
            _material.SetTexture(Shader.PropertyToID("shapeNoise"), _shapeNoise);
            _material.SetTexture(Shader.PropertyToID("weatherMap"), _weatherMap);
            _material.SetTexture(Shader.PropertyToID("blueNoise"), _blueNoise);
            _material.SetTexture(Shader.PropertyToID("detailNoise"), _detailNoise);
            
            _material.SetFloat(Shader.PropertyToID("threshold"), _threshold);
            _material.SetFloat(Shader.PropertyToID("density"), _density);
            _material.SetFloat(Shader.PropertyToID("scale"), _scale);
            _material.SetFloat(Shader.PropertyToID("detailScale"), _detailScale);
            _material.SetFloat(Shader.PropertyToID("absorption"), _absorption);
            _material.SetFloat(Shader.PropertyToID("attenuationClamp"), _attenuationClamp);
            _material.SetFloat(Shader.PropertyToID("outScatteringAmbient"), _outScatteringAmbient);
            
            _material.SetFloat(Shader.PropertyToID("minimumAttenuationAmbient"), _minimumAttenuationAmbient);
            _material.SetFloat(Shader.PropertyToID("atmosphericBlending"), _atmosphericBlending);
            
            _material.SetFloat(Shader.PropertyToID("sunIntensity"), _sunIntensity);
            _material.SetFloat(Shader.PropertyToID("sunIntensityRadius"), _sunIntensityRadius);
            _material.SetFloat(Shader.PropertyToID("inScatter"), _inScatter);
            _material.SetFloat(Shader.PropertyToID("outScatter"), _outScatter);
            _material.SetFloat(Shader.PropertyToID("scatterLerp"), _scatterLerp);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if(!Application.isPlaying)
                return;
            
            UniversalResourceData resources = frameData.Get<UniversalResourceData>();
            TextureHandle currentScreenHandle = resources.activeColorTexture;
        
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            if(resources.isActiveTargetBackBuffer)
                return;

            _textureDescriptor.width = cameraData.cameraTargetDescriptor.width;
            _textureDescriptor.height = cameraData.cameraTargetDescriptor.height;
            
            TextureHandle outputHandle = UniversalRenderer.CreateRenderGraphTexture(renderGraph, _textureDescriptor, 
                 "output", false);
            
            if(!currentScreenHandle.IsValid() || !outputHandle.IsValid())
                return;
            
            UpdateSettings();

            RenderGraphUtils.BlitMaterialParameters passParams =
                new RenderGraphUtils.BlitMaterialParameters(currentScreenHandle, outputHandle, _material, 0);
                    
            renderGraph.AddBlitPass(passParams);
                
            renderGraph.AddBlitPass(outputHandle, currentScreenHandle, Vector2.one, Vector2.zero);
        }
    }
}
