using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace Rendering
{
    public class VolumetricRenderPass : ScriptableRenderPass
    {
        private RenderTextureDescriptor _textureDescriptor;
        private ComputeBuffer _allBounds;
        private RenderTexture _noise;

        private Material _material;
        
        public VolumetricRenderPass(VolumeBounds[] allBounds, Shader shader, RenderTexture noise)
        {
            _material = new Material(shader);
            
            _textureDescriptor = new RenderTextureDescriptor(Screen.width, 
                Screen.height, RenderTextureFormat.Default, 0);

            _textureDescriptor.enableRandomWrite = true;

            _allBounds =
                new ComputeBuffer(allBounds.Length, Marshal.SizeOf<VolumeBounds>());
            _allBounds.SetData(allBounds);

            _noise = noise;
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
            
            if(!currentScreenHandle.IsValid() || !outputHandle.IsValid())
                return;

            _material.SetBuffer(Shader.PropertyToID("volumeBounds"), _allBounds);
            _material.SetTexture(Shader.PropertyToID("noise"), _noise);

            RenderGraphUtils.BlitMaterialParameters passParams =
                new RenderGraphUtils.BlitMaterialParameters(currentScreenHandle, outputHandle, _material, 0);
                    
            renderGraph.AddBlitPass(passParams);
                
            renderGraph.AddBlitPass(outputHandle, currentScreenHandle, Vector2.one, Vector2.zero);
        }

        ~VolumetricRenderPass()
        {
            _allBounds.Dispose();
            Object.Destroy(_material);
        }
    }
}
