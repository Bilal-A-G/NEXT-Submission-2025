using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;
using Volumetrics;
using Volumetrics.Settings;
using Object = UnityEngine.Object;

namespace Rendering
{
    public class PassData
    {
        public TextureHandle CloudAccumulationBuffer;
        public TextureHandle CloudDepthMap;
        public TextureHandle CloudTransmittanceMap;
        public Vector2Int RenderTargetDimensions;
    }

    public class TemporalPassData
    {
        public TextureHandle CurrentCloudAccumulationBuffer;
        public TextureHandle NextCloudAccumulationBuffer;
        public TextureHandle CameraMotionVectors;
        public TextureHandle AccumulatedMotionVectorsMap;
        public Vector2Int RenderTargetDimensions;
    }
    
    public class VolumetricCloudsRenderPass : ScriptableRenderPass
    {
        private VolumeDefinition[] _allVolumes;

        private VolumetricCloudSettingsSo _settings;
        public int frameCounter = 0;
        public int framesElapsed = 0;
        
        private const int MaxMotionVectorStepPow2Exponent = 4;
        private const int ThreadsPerGroup = 8;
        
        private RenderTextureDescriptor _textureDescriptor;
        
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

        private void UpdateSettings(UniversalCameraData cameraData, ComputeGraphContext context, PassData data)
        {
            VolumeBounds[] allBounds = new VolumeBounds[_allVolumes.Length];
            for (int i = 0; i < _allVolumes.Length; i++)
            {
                Transform currentTransform = _allVolumes[i].transform;
                allBounds[i] = new VolumeBounds(currentTransform.position, currentTransform.localScale);
            }

            ComputeBuffer allBoundsBuffer = VolumetricCloudsResourceManager.GetInstance().GetAllBounds();
            allBoundsBuffer.SetData(allBounds);
            context.cmd.SetComputeBufferParam(_settings.RayMarcher, 0, 
                Shader.PropertyToID("volumeBounds"), allBoundsBuffer);

            context.cmd.SetComputeTextureParam(_settings.RayMarcher, 0, 
                Shader.PropertyToID("input"), data.CloudAccumulationBuffer);
            _settings.RayMarcher.SetTexture(0, 
                Shader.PropertyToID("cloudDepthMap"), data.CloudDepthMap);
            _settings.RayMarcher.SetTexture(0, 
                Shader.PropertyToID("transmittanceMap"), data.CloudTransmittanceMap);
            
            context.cmd.SetComputeIntParam(_settings.RayMarcher, Shader.PropertyToID("frameOffset"), frameCounter);
            
            _settings.BlueNoise.wrapMode = TextureWrapMode.Repeat;
            _settings.BlueNoise.filterMode = FilterMode.Bilinear;
            _settings.weatherMap.wrapMode = TextureWrapMode.Repeat;
            _settings.weatherMap.filterMode = FilterMode.Bilinear;

            _settings.shapeNoise.textureOutput.wrapMode = TextureWrapMode.Repeat;
            _settings.shapeNoise.textureOutput.filterMode = FilterMode.Bilinear;
            
            _settings.detailNoise.textureOutput.wrapMode = TextureWrapMode.Repeat;
            _settings.detailNoise.textureOutput.filterMode = FilterMode.Bilinear;
            
            _settings.RayMarcher.SetTexture(0, 
                Shader.PropertyToID("shapeNoise"), _settings.shapeNoise.textureOutput);
            _settings.RayMarcher.SetTexture(0, 
                Shader.PropertyToID("detailNoise"), _settings.detailNoise.textureOutput);
            _settings.RayMarcher.SetTexture(0, 
                Shader.PropertyToID("blueNoise"), _settings.BlueNoise);
            _settings.RayMarcher.SetTexture(0, 
                Shader.PropertyToID("weatherMap"), _settings.weatherMap);
            
            context.cmd.SetComputeFloatParam(_settings.RayMarcher, Shader.PropertyToID("threshold"), 
                _settings.globalCoverage);
            context.cmd.SetComputeFloatParam(_settings.RayMarcher, Shader.PropertyToID("density"), 
                _settings.globalDensity);
            context.cmd.SetComputeFloatParam(_settings.RayMarcher, Shader.PropertyToID("scale"), 
                _settings.shapeNoiseUVScale);
            context.cmd.SetComputeFloatParam(_settings.RayMarcher, Shader.PropertyToID("detailScale"), 
                _settings.detailNoiseUVScale);
            context.cmd.SetComputeFloatParam(_settings.RayMarcher, Shader.PropertyToID("absorption"), 
                _settings.absorption);
            context.cmd.SetComputeFloatParam(_settings.RayMarcher, Shader.PropertyToID("attenuationClamp"), 
                _settings.minimumShadowing);
            context.cmd.SetComputeFloatParam(_settings.RayMarcher, Shader.PropertyToID("outScatteringAmbient"), 
                _settings.powderAmount);
            
            context.cmd.SetComputeFloatParam(_settings.RayMarcher, Shader.PropertyToID("minimumAttenuationAmbient"), 
                _settings.shadowDetail);
            context.cmd.SetComputeFloatParam(_settings.RayMarcher, Shader.PropertyToID("atmosphericBlending"),
                _settings.atmosphereBlending);
            
            context.cmd.SetComputeFloatParam(_settings.RayMarcher, Shader.PropertyToID("sunIntensity"), 
                _settings.sunExtraIntensity);
            context.cmd.SetComputeVectorParam(_settings.RayMarcher, Shader.PropertyToID("imageSize"), 
                new Vector2(data.RenderTargetDimensions.x, data.RenderTargetDimensions.y));
            context.cmd.SetComputeFloatParam(_settings.RayMarcher, Shader.PropertyToID("sunIntensityRadius"), 
                _settings.sunExtraIntensityLocalization);
            context.cmd.SetComputeFloatParam(_settings.RayMarcher, Shader.PropertyToID("inScatter"), 
                _settings.inScattering);
            context.cmd.SetComputeFloatParam(_settings.RayMarcher, Shader.PropertyToID("outScatter"), 
                _settings.outScattering);
            context.cmd.SetComputeFloatParam(_settings.RayMarcher, Shader.PropertyToID("scatterLerp"), 
                _settings.inToOutScatteringInterpolation);
            
            context.cmd.SetComputeVectorParam(_settings.RayMarcher, Shader.PropertyToID("shapeOffset"), 
                _settings.shapeNoiseUVOffset);
            context.cmd.SetComputeVectorParam(_settings.RayMarcher, Shader.PropertyToID("detailOffset"), 
                _settings.detailNoiseUVOffset);
            
            context.cmd.SetComputeVectorParam(_settings.RayMarcher, 
                Shader.PropertyToID("startingCameraPosition"), cameraData.worldSpaceCameraPos);
            context.cmd.SetComputeVectorParam(_settings.RayMarcher, 
                Shader.PropertyToID("mainLightDirection"), -RenderSettings.sun.transform.forward);
            context.cmd.SetComputeVectorParam(_settings.RayMarcher, 
                Shader.PropertyToID("mainLightColour"), RenderSettings.sun.color * RenderSettings.sun.intensity);
            context.cmd.SetComputeVectorParam(_settings.RayMarcher, 
                Shader.PropertyToID("ambient"), RenderSettings.ambientSkyColor * RenderSettings.ambientIntensity);
            context.cmd.SetComputeMatrixParam(_settings.RayMarcher, 
                Shader.PropertyToID("unityCameraInverseProjection"), cameraData.GetProjectionMatrix().inverse);
            context.cmd.SetComputeMatrixParam(_settings.RayMarcher, 
                Shader.PropertyToID("unityCameraToWorld"), cameraData.camera.cameraToWorldMatrix);
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
            
            RTHandle cloudAccumulationBuffer = resourceManager.GetCloudAccumulation(renderTargetDimensions);
            RTHandle cloudDepthMap = resourceManager.GetCloudDepth(renderTargetDimensions);
            RTHandle cloudTransmittance = resourceManager.GetCloudTransmission(renderTargetDimensions);
            RTHandle finalCloudAccumulationBuffer = resourceManager.GetFinalCloudAccumulation(renderTargetDimensions);
            RTHandle cloudAccumulatedMotionVectorsMap =
                resourceManager.GetAccumulatedCloudMotionVectors(renderTargetDimensions);
            
            TextureHandle cloudAccumulationBufferHandle = renderGraph.ImportTexture(cloudAccumulationBuffer);
            TextureHandle cloudDepthHandle = renderGraph.ImportTexture(cloudDepthMap);
            TextureHandle cloudTransmittanceHandle = renderGraph.ImportTexture(cloudTransmittance);
            TextureHandle finalCloudAccumulationBufferHandle = renderGraph.ImportTexture(finalCloudAccumulationBuffer);
            TextureHandle cloudAccumulatedMotionVectorsMapHandle =
                renderGraph.ImportTexture(cloudAccumulatedMotionVectorsMap);
            
            using (IComputeRenderGraphBuilder builder = renderGraph.AddComputePass("RayMarch", out PassData passData))
            {
                passData.CloudAccumulationBuffer = cloudAccumulationBufferHandle;
                passData.RenderTargetDimensions = renderTargetDimensions;
                passData.CloudDepthMap = cloudDepthHandle;
                passData.CloudTransmittanceMap = cloudTransmittanceHandle;

                builder.UseTexture(passData.CloudAccumulationBuffer, AccessFlags.ReadWrite);
                builder.SetRenderFunc((PassData data, ComputeGraphContext context) => ExecuteRayMarch(data, context, cameraData));
            }

            renderGraph.AddBlitPass(cloudAccumulationBufferHandle, 
                finalCloudAccumulationBufferHandle, Vector2.one, Vector2.zero);

            if (framesElapsed == 1)
            {
                using (IComputeRenderGraphBuilder builder =
                       renderGraph.AddComputePass("TemporalAccumulation", out TemporalPassData passData))
                {
                    passData.CurrentCloudAccumulationBuffer = cloudAccumulationBufferHandle;
                    passData.NextCloudAccumulationBuffer = finalCloudAccumulationBufferHandle;
                    passData.RenderTargetDimensions = renderTargetDimensions;
                    passData.CameraMotionVectors = resources.motionVectorColor;
                    passData.AccumulatedMotionVectorsMap = cloudAccumulatedMotionVectorsMapHandle;
            
                    builder.UseTexture(passData.NextCloudAccumulationBuffer, AccessFlags.ReadWrite);
                    builder.UseTexture(passData.CurrentCloudAccumulationBuffer, AccessFlags.ReadWrite);
                    builder.UseTexture(passData.AccumulatedMotionVectorsMap, AccessFlags.ReadWrite);
                    builder.SetRenderFunc((TemporalPassData data, ComputeGraphContext context) =>
                        ExecuteTemporalReProjection(data, context));
                    
                    Debug.Log("Temporally reprojected");
                }      
            }
            

            Material material = VolumetricCloudsResourceManager.GetInstance().GetCompositeMaterial();
            TextureHandle outputHandle = UniversalRenderer.CreateRenderGraphTexture(renderGraph, _textureDescriptor,
                "output", false);

            material.SetTexture(Shader.PropertyToID("cloudAccumulation"), finalCloudAccumulationBuffer);
            material.SetTexture(Shader.PropertyToID("cloudDepth"), cloudDepthMap);
            material.SetTexture(Shader.PropertyToID("cloudTransmittance"), cloudTransmittance);
            material.SetFloat(Shader.PropertyToID("maxCloudDepth"), 200.0f);
            material.SetVector(Shader.PropertyToID("textureSize"),
                new Vector2(renderTargetDimensions.x, renderTargetDimensions.y));

            RenderGraphUtils.BlitMaterialParameters passParams =
                new RenderGraphUtils.BlitMaterialParameters(currentScreenHandle,
                    outputHandle, material, 0);

            renderGraph.AddBlitPass(passParams);

            renderGraph.AddBlitPass(outputHandle, currentScreenHandle, Vector2.one, Vector2.zero);
            renderGraph.AddBlitPass(finalCloudAccumulationBufferHandle,
                 cloudAccumulationBufferHandle, Vector2.one, Vector2.zero);
        }

        private void ExecuteRayMarch(PassData data, ComputeGraphContext context, UniversalCameraData cameraData)
        {
            UpdateSettings(cameraData, context, data);

            Vector2 threadGroups = new Vector2(data.RenderTargetDimensions.x, 
                data.RenderTargetDimensions.y)/4/ThreadsPerGroup;
            
            context.cmd.DispatchCompute(_settings.RayMarcher, 0, 
                Mathf.CeilToInt(threadGroups.x), Mathf.CeilToInt(threadGroups.y), 1);
        }

        private void ExecuteTemporalReProjection(TemporalPassData data, ComputeGraphContext context)
        {
            context.cmd.SetComputeTextureParam(_settings.TemporalReprojector, 0, 
                Shader.PropertyToID("finalAccumulation"), data.NextCloudAccumulationBuffer);
            context.cmd.SetComputeTextureParam(_settings.TemporalReprojector, 0, 
                Shader.PropertyToID("accumulation"), data.CurrentCloudAccumulationBuffer);
            context.cmd.SetComputeVectorParam(_settings.TemporalReprojector, Shader.PropertyToID("textureSize"), 
                (Vector2)data.RenderTargetDimensions);
            context.cmd.SetComputeIntParam(_settings.TemporalReprojector, 
                Shader.PropertyToID("frameOffset"), frameCounter);
            _settings.TemporalReprojector.SetTexture(0, Shader.PropertyToID("motionVectors"), 
                data.CameraMotionVectors);
            context.cmd.SetComputeTextureParam(_settings.TemporalReprojector, 0, 
                Shader.PropertyToID("accumulatedMotionVectors"), data.AccumulatedMotionVectorsMap);
            
            Vector2 threadGroups = new Vector2(data.RenderTargetDimensions.x, 
                data.RenderTargetDimensions.y)/ThreadsPerGroup;
            context.cmd.DispatchCompute(_settings.TemporalReprojector, 0, 
                Mathf.CeilToInt(threadGroups.x), Mathf.CeilToInt(threadGroups.y), 1);
        }
    }
}
