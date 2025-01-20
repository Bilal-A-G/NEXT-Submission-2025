using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Volumetrics;
using Volumetrics.Noise;
using Volumetrics.Settings;

namespace Rendering
{
    public class VolumetricCloudsRenderFeature : ScriptableRendererFeature
    {
        [SerializeField] private VolumetricCloudSettingsSo profile;
        private VolumetricCloudsRenderPass _cloudsRenderPass;

        public override void Create()
        {
            if(!Application.isPlaying)
                return;
            
            if (profile == null)
                profile = Resources.Load<VolumetricCloudSettingsSo>("VolumetricClouds/Settings/Default");
            
            NoiseGenerator.GenerateNoise(new NoiseTextureData[] { profile.shapeNoise, 
                profile.detailNoise });
            VolumetricCloudsResourceManager.GetInstance().CreateRayMarchMaterial(profile.RayMarcher);
            VolumetricCloudsResourceManager.GetInstance().CreateCompositorMaterial(profile.Compositor);
            VolumetricCloudsResourceManager.GetInstance().CreateSettingsBuffer();
            
            _cloudsRenderPass = new VolumetricCloudsRenderPass(ref profile);
            _cloudsRenderPass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
            
            Debug.Log("Finished creation");
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if(_cloudsRenderPass == null || !Application.isPlaying)
                return;
            
            renderer.EnqueuePass(_cloudsRenderPass);
        }
    }
}