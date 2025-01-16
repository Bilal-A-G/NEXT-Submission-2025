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
        private VolumetricCloudSettingsSo _profile;
        private RenderTextureDescriptor _textureDescriptor;
        
        public VolumetricCloudsRenderPass(ref VolumetricCloudSettingsSo profile)
        {
            if(!Application.isPlaying)
                return;
            
            _textureDescriptor = new RenderTextureDescriptor(Screen.width, 
                Screen.height, RenderTextureFormat.Default, 0);

            _textureDescriptor.enableRandomWrite = true;
            _profile = profile;
        }

        private void UpdateSettings(Material material, Vector2Int renderTargetDimensions)
        {
            _profile.BlueNoise.wrapMode = TextureWrapMode.Repeat;
            _profile.BlueNoise.filterMode = FilterMode.Bilinear;
            _profile.weatherMap.wrapMode = TextureWrapMode.Repeat;
            _profile.weatherMap.filterMode = FilterMode.Bilinear;
            
            _profile.shapeNoise.textureOutput.wrapMode = TextureWrapMode.Repeat;
            _profile.shapeNoise.textureOutput.filterMode = FilterMode.Bilinear;
            
            _profile.detailNoise.textureOutput.wrapMode = TextureWrapMode.Repeat;
            _profile.detailNoise.textureOutput.filterMode = FilterMode.Bilinear;
            
            material.SetTexture(Shader.PropertyToID("shapeNoise"), _profile.shapeNoise.textureOutput);
            material.SetTexture(Shader.PropertyToID("detailNoise"), _profile.detailNoise.textureOutput);
            material.SetTexture(Shader.PropertyToID("blueNoise"), _profile.BlueNoise);
            material.SetTexture(Shader.PropertyToID("weatherMap"), _profile.weatherMap);
            
            material.SetVector(Shader.PropertyToID("imageSize"), (Vector2)renderTargetDimensions);

            ComputeBuffer settingsBuffer = VolumetricCloudsResourceManager.GetInstance().GetSettingsBuffer();
            settingsBuffer.SetData(new VolumetricCloudSettings[]{_profile.settings});
            
            material.SetBuffer(Shader.PropertyToID("settingsArray"), settingsBuffer);
        }
        
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if(!Application.isPlaying)
                return;

            VolumetricCloudsResourceManager resourceManager = VolumetricCloudsResourceManager.GetInstance();

            UniversalResourceData resources = frameData.Get<UniversalResourceData>();
            TextureHandle currentScreenHandle = resources.activeColorTexture;
            
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            if(resources.isActiveTargetBackBuffer)
                return;
            RenderTextureDescriptor cameraTargetDescriptor = cameraData.cameraTargetDescriptor;
            Vector2Int renderTargetDimensions = new Vector2Int(cameraTargetDescriptor.width, 
                cameraTargetDescriptor.height);
            _textureDescriptor.width = cameraTargetDescriptor.width;
            _textureDescriptor.height = cameraTargetDescriptor.height;

            TextureHandle outputHandle =
                UniversalRenderer.CreateRenderGraphTexture(renderGraph, _textureDescriptor, "output", false);

            RTHandle cloudQuarterResAccumulationMap = resourceManager.GetQuarterResAccumulationMap(renderTargetDimensions);
            TextureHandle cloudQuarterResAccumulationMapHandle =
                renderGraph.ImportTexture(cloudQuarterResAccumulationMap);
            cloudQuarterResAccumulationMap.rt.filterMode = FilterMode.Trilinear;
            cloudQuarterResAccumulationMap.rt.wrapMode = TextureWrapMode.Clamp;
            
            Material rayMarchMaterial = VolumetricCloudsResourceManager.GetInstance().GetRayMarchMaterial();
            UpdateSettings(rayMarchMaterial, renderTargetDimensions/4);
            RenderGraphUtils.BlitMaterialParameters passParams =
                new RenderGraphUtils.BlitMaterialParameters(currentScreenHandle,
                    cloudQuarterResAccumulationMapHandle, rayMarchMaterial, 0);

            renderGraph.AddBlitPass(passParams);

            Material compositorMaterial = VolumetricCloudsResourceManager.GetInstance().GetCompositorMaterial();
            compositorMaterial.SetTexture(Shader.PropertyToID("Clouds"), cloudQuarterResAccumulationMap);
            passParams = new RenderGraphUtils.BlitMaterialParameters(currentScreenHandle, 
                    outputHandle, compositorMaterial, 0);
            
            renderGraph.AddBlitPass(passParams);
            
            renderGraph.AddBlitPass(outputHandle, currentScreenHandle, Vector2.one, Vector2.zero);
        }
    }
}
