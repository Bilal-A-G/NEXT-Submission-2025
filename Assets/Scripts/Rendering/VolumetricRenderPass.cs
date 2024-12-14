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
        
        private RenderTexture _shapeNoise;
        private RenderTexture _detailNoise;

        private Material _material;

        private float _density;
        private float _threshold;
        private float _scale;
        
        private float _rScale;
        private float _gScale;
        private float _bScale;
        private float _aScale;

        
        public VolumetricRenderPass(VolumeBounds[] allBounds, Shader shader, 
            RenderTexture shapeNoise, RenderTexture detailNoise, ref float density, ref float threshold, ref float scale,
            ref float rScale, ref float gScale, ref float bScale, ref float aScale)
        {
            if(!Application.isPlaying)
                return;
            
            _material = new Material(shader);
            
            _textureDescriptor = new RenderTextureDescriptor(Screen.width, 
                Screen.height, RenderTextureFormat.Default, 0);

            _textureDescriptor.enableRandomWrite = true;

            _allBounds =
                new ComputeBuffer(allBounds.Length, Marshal.SizeOf<VolumeBounds>());
            _allBounds.SetData(allBounds);

            _shapeNoise = shapeNoise;
            _detailNoise = detailNoise;

            _density = density;
            _threshold = threshold;
            _scale = scale;

            _rScale = rScale;
            _gScale = gScale;
            _bScale = bScale;
            _aScale = aScale;
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
            
            if(!currentScreenHandle.IsValid() || !outputHandle.IsValid())
                return;

            _material.SetBuffer(Shader.PropertyToID("volumeBounds"), _allBounds);
            
            _material.SetTexture(Shader.PropertyToID("shapeNoise"), _shapeNoise);
            _material.SetTexture(Shader.PropertyToID("detailNoise"), _detailNoise);
            
            _material.SetFloat(Shader.PropertyToID("threshold"), _threshold);
            _material.SetFloat(Shader.PropertyToID("density"), _density);
            _material.SetFloat(Shader.PropertyToID("scale"), _scale);
            
            _material.SetFloat(Shader.PropertyToID("rScale"), _rScale);
            _material.SetFloat(Shader.PropertyToID("gScale"), _gScale);
            _material.SetFloat(Shader.PropertyToID("bScale"), _bScale);
            _material.SetFloat(Shader.PropertyToID("aScale"), _aScale);

            RenderGraphUtils.BlitMaterialParameters passParams =
                new RenderGraphUtils.BlitMaterialParameters(currentScreenHandle, outputHandle, _material, 0);
                    
            renderGraph.AddBlitPass(passParams);
                
            renderGraph.AddBlitPass(outputHandle, currentScreenHandle, Vector2.one, Vector2.zero);
        }
    }
}
