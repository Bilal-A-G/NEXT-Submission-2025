using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Rendering
{
    public class VolumetricRenderFeature : ScriptableRendererFeature
    {
        [SerializeField] private ComputeShader volumetricShader;

        private VolumetricRenderPass _renderPass;
        
        public override void Create()
        {
            _renderPass = new VolumetricRenderPass(volumetricShader)
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if(_renderPass == null)
                return;
            
            if(renderingData.cameraData.cameraType != CameraType.Game)
                return;
            
            renderer.EnqueuePass(_renderPass);
        }
    }
}