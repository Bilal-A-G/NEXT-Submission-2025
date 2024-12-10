using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Volumetrics;

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
        [SerializeField] private Shader shader;

        private VolumetricRenderPass _renderPass;
        private Material _material;
        
        public override void Create()
        {
            if(shader == null)
                return;
            
            VolumeDefinition[] allVolumes = FindObjectsByType<VolumeDefinition>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            VolumeBounds[] allBounds = new VolumeBounds[allVolumes.Length];
            for (int i = 0; i < allVolumes.Length; i++)
            {
                Transform currentTransform = allVolumes[i].transform;
                allBounds[i] = new VolumeBounds(currentTransform.position, currentTransform.localScale);
            }
            
            _material = new Material(shader);

            _renderPass = new VolumetricRenderPass(allBounds, _material);
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