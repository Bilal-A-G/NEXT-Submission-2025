using UnityEngine;
using UnityEngine.Rendering.Universal;
using Volumetrics;
using Volumetrics.Noise;

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

        [SerializeField] private float density;
        [SerializeField] [Range(0, 1)] private float threshold;
        [SerializeField] private float scale;
        
        [SerializeField] private float rScale;
        [SerializeField] private float gScale;
        [SerializeField] private float bScale;
        [SerializeField] private float aScale;

        [SerializeField] private Texture2D weatherMap;

        private VolumetricRenderPass _renderPass;
        private Material _material;
        
        public override void Create()
        {
            if(volumetricShader == null || !Application.isPlaying)
                return;

            NoiseGenerator.GenerateNoise(ref noiseTextureConfigs);
            
            VolumeDefinition[] allVolumes = FindObjectsByType<VolumeDefinition>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            VolumeBounds[] allBounds = new VolumeBounds[allVolumes.Length];
            for (int i = 0; i < allVolumes.Length; i++)
            {
                Transform currentTransform = allVolumes[i].transform;
                allBounds[i] = new VolumeBounds(currentTransform.position, currentTransform.localScale);
            }
            
            if(allBounds.Length == 0)
                return;
            
            _renderPass = new VolumetricRenderPass(allBounds, volumetricShader, noiseTextureConfigs[0].textureOutput, 
                noiseTextureConfigs[1].textureOutput, ref density, ref threshold, ref scale, 
                ref rScale, ref gScale, ref bScale, ref aScale, weatherMap);
            _renderPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if(_renderPass == null)
                return;
            
            if(renderingData.cameraData.cameraType != CameraType.Game)
                return;
            
            renderer.EnqueuePass(_renderPass);
        }
        
        protected override void Dispose(bool disposing)
        {
            if(_material == null)
                return;
            
            if (Application.isPlaying)
            {
                Destroy(_material);
                return;
            }
            
            DestroyImmediate(_material);
        }
    }
}