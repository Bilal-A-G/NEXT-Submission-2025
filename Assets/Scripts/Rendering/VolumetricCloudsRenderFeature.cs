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
        private int _frameCount;
        private int _frameCountLastFrame;
        private const int TextureRefreshRate = 16;

        public override void Create()
        {
            if(!Application.isPlaying)
                return;
            
            if (profile == null)
                profile = Resources.Load<VolumetricCloudSettingsSo>("VolumetricClouds/Settings/Default");
            
            NoiseGenerator.GenerateNoise(new NoiseTextureData[] { profile.shapeNoise, profile.detailNoise });
            VolumetricCloudsResourceManager.GetInstance().CreateCompositeMaterial(profile.Compositor);
            
            _cloudsRenderPass = new VolumetricCloudsRenderPass(ref profile);
            _cloudsRenderPass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
            
            Debug.Log("Finished creation");
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if(_cloudsRenderPass == null || !Application.isPlaying)
                return;
            
            _frameCount += Time.frameCount - _frameCountLastFrame;
            _frameCount %= TextureRefreshRate;
            int framesElapsed = Mathf.Abs(_frameCountLastFrame - _frameCount) > 0 ? 1 : 0;

            _cloudsRenderPass.frameCounter = _frameCount;
            _cloudsRenderPass.framesElapsed = framesElapsed;
            _cloudsRenderPass.ConfigureInput(ScriptableRenderPassInput.Motion);
            renderer.EnqueuePass(_cloudsRenderPass);
            
            _frameCountLastFrame = _frameCount;
        }
    }
}