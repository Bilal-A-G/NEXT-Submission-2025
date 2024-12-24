using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;
using Volumetrics;
using Volumetrics.Settings;

namespace Rendering
{
<<<<<<< Updated upstream
    public class VolumetricCloudsRenderPass : ScriptableRenderPass
    {
        private RenderTextureDescriptor _textureDescriptor;
        private VolumeDefinition[] _allVolumes;

        private RTHandle _input;
=======
    public class PassData
    {
        public TextureHandle CloudAccumulationBuffer;
        public TextureHandle CloudDepthMap;
        public TextureHandle CloudTransmittanceMap;
        public Vector2Int RenderTargetDimensions;
    }

    public class TemporalPassData
    {
        public TextureHandle CurrentFrameAccumulationBuffer;
        public TextureHandle PrevFrameAccumulationBuffer;
        public TextureHandle QuarterResAccumulationBuffer;
        public Vector2Int RenderTargetDimensions;
    }
    
    public class VolumetricCloudsRenderPass : ScriptableRenderPass
    {
>>>>>>> Stashed changes
        private VolumetricCloudSettingsSo _settings;
        
        public VolumetricCloudsRenderPass(ref VolumetricCloudSettingsSo settings)
        {
            if(!Application.isPlaying)
                return;
            
            _textureDescriptor = new RenderTextureDescriptor(Screen.width, 
                Screen.height, RenderTextureFormat.Default, 0);

            _textureDescriptor.enableRandomWrite = true;
            _settings = settings;
        }

        private void UpdateSettings()
        {
<<<<<<< Updated upstream
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
=======
            context.cmd.SetComputeFloatParam(_settings.RayMarcher, Shader.PropertyToID("cloudStartHeight"), 
                _settings.cloudStartHeight);
            context.cmd.SetComputeFloatParam(_settings.RayMarcher, Shader.PropertyToID("cloudEndHeight"), 
                _settings.cloudEndHeight);

            context.cmd.SetComputeTextureParam(_settings.RayMarcher, 0, 
                Shader.PropertyToID("input"), data.CloudAccumulationBuffer);
            _settings.RayMarcher.SetTexture(0, 
                Shader.PropertyToID("cloudDepthMap"), data.CloudDepthMap);
            _settings.RayMarcher.SetTexture(0, 
                Shader.PropertyToID("transmittanceMap"), data.CloudTransmittanceMap);
>>>>>>> Stashed changes
            
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
            
<<<<<<< Updated upstream
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
=======
            TextureHandle cloudAccumulationBufferHandle = renderGraph.ImportTexture(cloudAccumulationBuffer);
            TextureHandle cloudDepthHandle = renderGraph.ImportTexture(cloudDepthMap);
            TextureHandle cloudTransmittanceHandle = renderGraph.ImportTexture(cloudTransmittance);
            TextureHandle finalCloudAccumulationBufferHandle = renderGraph.ImportTexture(finalCloudAccumulationBuffer);
            TextureHandle cloudAccumulatedMotionVectorsMapHandle =
                renderGraph.ImportTexture(cloudAccumulatedMotionVectorsMap);

            RTHandle cloudQuarterResAccumulationMap = resourceManager.GetQuarterResAccumulationMap(renderTargetDimensions);
            TextureHandle cloudQuarterResAccumulationMapHandle =
                renderGraph.ImportTexture(cloudQuarterResAccumulationMap);

            //This frame's result up sampled
            RTHandle cloudFrontBuffer = resourceManager.GetCloudFrontBuffer(renderTargetDimensions);
            TextureHandle cloudFrontBufferHandle = renderGraph.ImportTexture(cloudFrontBuffer);
            
            using (IComputeRenderGraphBuilder builder = renderGraph.AddComputePass("RayMarch", out PassData passData))
            {
                passData.CloudAccumulationBuffer = cloudQuarterResAccumulationMapHandle;
                passData.RenderTargetDimensions = renderTargetDimensions;
                passData.CloudDepthMap = cloudDepthHandle;
                passData.CloudTransmittanceMap = cloudTransmittanceHandle;

                builder.UseTexture(passData.CloudAccumulationBuffer, AccessFlags.ReadWrite);
                builder.SetRenderFunc((PassData data, ComputeGraphContext context) => ExecuteRayMarch(data, context, cameraData));
            }
            
            cloudQuarterResAccumulationMap.rt.filterMode = FilterMode.Trilinear;
            cloudQuarterResAccumulationMap.rt.wrapMode = TextureWrapMode.Clamp;

            // using (IComputeRenderGraphBuilder builder =
            //        renderGraph.AddComputePass("TemporalAccumulation", out TemporalPassData passData))
            // {
            //     passData.CurrentFrameAccumulationBuffer = cloudFrontBufferHandle;
            //     passData.PrevFrameAccumulationBuffer = cloudBackBufferHandle;
            //     passData.QuarterResAccumulationBuffer = cloudQuarterResAccumulationMapHandle;
            //     passData.RenderTargetDimensions = renderTargetDimensions;
            //
            //     builder.UseTexture(passData.CurrentFrameAccumulationBuffer, AccessFlags.ReadWrite);
            //     builder.UseTexture(passData.PrevFrameAccumulationBuffer, AccessFlags.ReadWrite);
            //     builder.UseTexture(passData.QuarterResAccumulationBuffer, AccessFlags.Read);
            //     builder.SetRenderFunc((TemporalPassData data, ComputeGraphContext context) =>
            //         ExecuteTemporalReProjection(data, context));
            // }      

            // Material material = VolumetricCloudsResourceManager.GetInstance().GetCompositeMaterial();
            // TextureHandle outputHandle = UniversalRenderer.CreateRenderGraphTexture(renderGraph, _textureDescriptor,
            //     "output", false);
            //
            // material.SetTexture(Shader.PropertyToID("cloudAccumulation"), cloudFrontBuffer);
            // material.SetTexture(Shader.PropertyToID("cloudDepth"), cloudDepthMap);
            // material.SetTexture(Shader.PropertyToID("cloudTransmittance"), cloudTransmittance);
            // material.SetFloat(Shader.PropertyToID("maxCloudDepth"), 200.0f);
            // material.SetVector(Shader.PropertyToID("textureSize"),
            //     new Vector2(renderTargetDimensions.x, renderTargetDimensions.y));
            //
            // RenderGraphUtils.BlitMaterialParameters passParams =
            //     new RenderGraphUtils.BlitMaterialParameters(currentScreenHandle,
            //         outputHandle, material, 0);
            //
            // renderGraph.AddBlitPass(passParams);
            
            renderGraph.AddBlitPass(cloudQuarterResAccumulationMapHandle, currentScreenHandle, Vector2.one, Vector2.zero);
        }

        private void ExecuteRayMarch(PassData data, ComputeGraphContext context, UniversalCameraData cameraData)
        {
            UpdateSettings(cameraData, context, data);

            Vector2 threadGroups = new Vector2(data.RenderTargetDimensions.x, 
                data.RenderTargetDimensions.y)/ThreadsPerGroup;
            
            context.cmd.DispatchCompute(_settings.RayMarcher, 0, 
                Mathf.CeilToInt(threadGroups.x), Mathf.CeilToInt(threadGroups.y), 1);
        }

        private void ExecuteTemporalReProjection(TemporalPassData data, ComputeGraphContext context)
        {
            context.cmd.SetComputeTextureParam(_settings.TemporalReprojector, 0, 
                Shader.PropertyToID("currentFrameAccumulation"), data.CurrentFrameAccumulationBuffer);
            context.cmd.SetComputeTextureParam(_settings.TemporalReprojector, 0, 
                Shader.PropertyToID("prevFrameAccumulation"), data.PrevFrameAccumulationBuffer);
            context.cmd.SetComputeTextureParam(_settings.TemporalReprojector, 0, 
                Shader.PropertyToID("quarterResAccumulation"), data.QuarterResAccumulationBuffer);
            context.cmd.SetComputeVectorParam(_settings.TemporalReprojector, Shader.PropertyToID("textureSize"), 
                (Vector2)data.RenderTargetDimensions);
            
            context.cmd.SetComputeVectorParam(_settings.TemporalReprojector, 
                Shader.PropertyToID("jitterOffset"), (Vector2)_jitters[frameCounter]);
            
            Vector2 threadGroups = new Vector2(data.RenderTargetDimensions.x, 
                data.RenderTargetDimensions.y)/ThreadsPerGroup;
            context.cmd.DispatchCompute(_settings.TemporalReprojector, 0, 
                Mathf.CeilToInt(threadGroups.x), Mathf.CeilToInt(threadGroups.y), 1);
        }
>>>>>>> Stashed changes
    }
}
