using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;
using Volumetrics;
using Volumetrics.Settings;

namespace Rendering
{
    public class VolumetricCloudsRenderPass : ScriptableRenderPass
    {
        private RenderTextureDescriptor _textureDescriptor;
        private VolumeDefinition[] _allVolumes;

        private RTHandle _input;
        private VolumetricCloudSettingsSo _settings;
        
        public VolumetricCloudsRenderPass(ref VolumetricCloudSettingsSo settings)
        {
            if(!Application.isPlaying)
                return;
            
            _textureDescriptor = new RenderTextureDescriptor(Screen.width, 
                Screen.height, RenderTextureFormat.Default, 0);

            _textureDescriptor.enableRandomWrite = true;
            _allVolumes = Object.FindObjectsByType<VolumeDefinition>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            _settings = settings;
        }

        private void UpdateSettings()
        {
            VolumeBounds[] allBounds = new VolumeBounds[_allVolumes.Length];
            for (int i = 0; i < _allVolumes.Length; i++)
            {
                Transform currentTransform = _allVolumes[i].transform;
                allBounds[i] = new VolumeBounds(currentTransform.position, currentTransform.localScale);
            }

            ComputeBuffer allBoundsBuffer = VolumetricCloudsResourceManager.GetInstance().GetAllBounds();
            allBoundsBuffer.SetData(allBounds);
            Material material = VolumetricCloudsResourceManager.GetInstance().GetCompositeMaterial();
            material.SetBuffer(Shader.PropertyToID("volumeBounds"), allBoundsBuffer);
            
            material.SetTexture(Shader.PropertyToID("shapeNoise"), _settings.shapeNoise.textureOutput);
            material.SetTexture(Shader.PropertyToID("weatherMap"), _settings.weatherMap);
            material.SetTexture(Shader.PropertyToID("blueNoise"), _settings.BlueNoise);
            material.SetTexture(Shader.PropertyToID("detailNoise"), _settings.detailNoise.textureOutput);
            
            material.SetFloat(Shader.PropertyToID("threshold"), _settings.globalCoverage);
            material.SetFloat(Shader.PropertyToID("density"), _settings.globalDensity);
            material.SetFloat(Shader.PropertyToID("scale"), _settings.shapeNoiseUVScale);
            material.SetFloat(Shader.PropertyToID("detailScale"), _settings.detailNoiseUVScale);
            material.SetFloat(Shader.PropertyToID("absorption"), _settings.absorption);
            material.SetFloat(Shader.PropertyToID("attenuationClamp"), _settings.minimumShadowing);
            material.SetFloat(Shader.PropertyToID("outScatteringAmbient"), _settings.powderAmount);
            
            material.SetFloat(Shader.PropertyToID("minimumAttenuationAmbient"), _settings.shadowDetail);
            material.SetFloat(Shader.PropertyToID("atmosphericBlending"), _settings.atmosphereBlending);
            
            material.SetFloat(Shader.PropertyToID("sunIntensity"), _settings.sunExtraIntensity);
            material.SetFloat(Shader.PropertyToID("sunIntensityRadius"), _settings.sunExtraIntensityLocalization);
            material.SetFloat(Shader.PropertyToID("inScatter"), _settings.inScattering);
            material.SetFloat(Shader.PropertyToID("outScatter"), _settings.outScattering);
            material.SetFloat(Shader.PropertyToID("scatterLerp"), _settings.inToOutScatteringInterpolation);
            
            material.SetVector(Shader.PropertyToID("shapeOffset"), _settings.shapeNoiseUVOffset);
            material.SetVector(Shader.PropertyToID("detailOffset"), _settings.detailNoiseUVOffset);
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

            // RenderTextureDescriptor myDesc = new RenderTextureDescriptor(
            //     _textureDescriptor.width + 32, _textureDescriptor.height + 32, 
            //     GraphicsFormat.R16G16B16A16_SFloat, 0);
            // myDesc.enableRandomWrite = true;
            //
            // RenderingUtils.ReAllocateHandleIfNeeded(ref _input, myDesc);
            // _settings.RayMarcher.SetTexture(0, Shader.PropertyToID("input"), _input);
            // _settings.RayMarcher.Dispatch(0, _textureDescriptor.width + 32, 
            //     _textureDescriptor.height + 32, 1);
            //
            // TextureHandle myHandle = renderGraph.ImportTexture(_input);
            // renderGraph.AddBlitPass(myHandle, currentScreenHandle, Vector2.one, Vector2.zero);
            // return;
            
            if(!currentScreenHandle.IsValid() || !outputHandle.IsValid())
                return;
            
            UpdateSettings();
            Material material = VolumetricCloudsResourceManager.GetInstance().GetCompositeMaterial();

            RenderGraphUtils.BlitMaterialParameters passParams =
                new RenderGraphUtils.BlitMaterialParameters(currentScreenHandle, 
                    outputHandle, material, 0);
                    
            renderGraph.AddBlitPass(passParams);
                
            renderGraph.AddBlitPass(outputHandle, currentScreenHandle, Vector2.one, Vector2.zero);
        }
    }
}
