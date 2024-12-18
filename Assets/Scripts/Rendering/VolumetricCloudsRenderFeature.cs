using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Volumetrics;
using Volumetrics.Noise;
using Volumetrics.Settings;

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
            
            NoiseGenerator.GenerateNoise(new NoiseTextureData[] { profile.shapeNoise, profile.detailNoise });
            VolumeDefinition[] allVolumes = FindObjectsByType<VolumeDefinition>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                
            VolumetricCloudsResourceManager.GetInstance().CreateCompositeMaterial(profile.Compositor);
            VolumetricCloudsResourceManager.GetInstance().CreateAllBounds(Marshal.SizeOf<VolumeBounds>(), 
                allVolumes.Length);
            
            _cloudsRenderPass = new VolumetricCloudsRenderPass(ref profile);
            _cloudsRenderPass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
            
            Debug.Log("Finished creation");
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if(_cloudsRenderPass == null)
                return;
            
            renderer.EnqueuePass(_cloudsRenderPass);
        }
    }
}