using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace Rendering
{
    public class PassData
    {
        public TextureHandle outputHandle;
        public TextureHandle inputHandle;
        
        public ComputeShader shader;

        public int outputTextureProperty;
        public int inputTextureProperty;

        public int screenWidth;
        public int screenHeight;
    }
    
    public class VolumetricRenderPass : ScriptableRenderPass
    {
        private ComputeShader _shader;
        private RenderTextureDescriptor _textureDescriptor;
        
        public VolumetricRenderPass(ComputeShader shader)
        {
            _shader = shader;
            _textureDescriptor = new RenderTextureDescriptor(Screen.width, 
                Screen.height, RenderTextureFormat.Default, 0);

            _textureDescriptor.enableRandomWrite = true;
        }
        
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resources = frameData.Get<UniversalResourceData>();
            TextureHandle currentScreenHandle = resources.activeColorTexture;
        
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            if(resources.isActiveTargetBackBuffer)
                return;

            _textureDescriptor.width = cameraData.cameraTargetDescriptor.width;
            _textureDescriptor.height = cameraData.cameraTargetDescriptor.height;
            
            TextureHandle outputHandle = UniversalRenderer.CreateRenderGraphTexture(renderGraph, _textureDescriptor, 
                 "output", false);
            TextureHandle inputHandle = UniversalRenderer.CreateRenderGraphTexture(renderGraph, _textureDescriptor,
                "input", false);
            
            if(!currentScreenHandle.IsValid() || !outputHandle.IsValid() || !inputHandle.IsValid())
                return;
            
            renderGraph.AddBlitPass(currentScreenHandle, inputHandle, Vector2.one, Vector2.zero);

            using (IComputeRenderGraphBuilder builder = renderGraph.AddComputePass<PassData>("Compute Volumetric Clouds", 
                       out PassData passData))
            {
                passData.outputHandle = outputHandle;
                passData.inputHandle = inputHandle;
                passData.screenWidth = cameraData.cameraTargetDescriptor.width;
                passData.screenHeight = cameraData.cameraTargetDescriptor.height;

                passData.outputTextureProperty = Shader.PropertyToID("output");
                passData.inputTextureProperty = Shader.PropertyToID("input");
                
                passData.shader = _shader;
                
                builder.AllowPassCulling(true);
                builder.UseTexture(passData.outputHandle, AccessFlags.Write);
                builder.UseTexture(passData.inputHandle, AccessFlags.Read);
                
                builder.SetRenderFunc((PassData data, ComputeGraphContext context) => ExecutePass(data, context));
            }
                
            renderGraph.AddBlitPass(outputHandle, currentScreenHandle, Vector2.one, Vector2.zero);
        }

        private void ExecutePass(PassData data, ComputeGraphContext context)
        {
            context.cmd.SetComputeTextureParam(data.shader, 0, data.outputTextureProperty, data.outputHandle);
            context.cmd.SetComputeTextureParam(data.shader, 0, data.inputTextureProperty, data.inputHandle);

            context.cmd.DispatchCompute(data.shader, 0, 
                data.screenWidth, data.screenHeight, 1);
        }
    }
}
