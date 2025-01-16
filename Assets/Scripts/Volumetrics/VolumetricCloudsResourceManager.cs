using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Volumetrics.Settings;
using Object = UnityEngine.Object;
using Screen = UnityEngine.Device.Screen;

namespace Volumetrics
{
    public class VolumetricCloudsResourceManager : MonoBehaviour
    {
        private static VolumetricCloudsResourceManager _instance;
        private Material _rayMarchMaterial;
        private Material _compositorMaterial;

        private RTHandleSystem _rtHandleSystem;
        private RTHandle _cloudQuarterResAccumulationMap;

        private ComputeBuffer _settingsBuffer;
        
        private RenderTextureDescriptor _renderTextureDescriptor;
        
        public static VolumetricCloudsResourceManager GetInstance() => _instance;

        private void Awake()
        {
            if (_instance == null)
                _instance = this;
            else
            {
                Destroy(gameObject);
            }

            _rtHandleSystem = new RTHandleSystem();
            _rtHandleSystem.Initialize(Screen.width, Screen.height);
            _renderTextureDescriptor = new RenderTextureDescriptor(1, 1, 
                GraphicsFormat.R16G16B16A16_SFloat, 0);
            
            _renderTextureDescriptor.enableRandomWrite = true;
            _renderTextureDescriptor.useDynamicScale = true;
        }

        public void CreateRayMarchMaterial(Shader shader) => _rayMarchMaterial = new Material(shader);
        public ref Material GetRayMarchMaterial() => ref _rayMarchMaterial;
        
        public void CreateCompositorMaterial(Shader shader) => _compositorMaterial = new Material(shader);
        public ref Material GetCompositorMaterial() => ref _compositorMaterial;

        public void CreateSettingsBuffer() => 
            _settingsBuffer = new ComputeBuffer(1, Marshal.SizeOf<VolumetricCloudSettings>());
        public ref ComputeBuffer GetSettingsBuffer() => ref _settingsBuffer;

        
        private RTHandle GetRenderTexture(Vector2 screenSize, ref RTHandle handle, Vector2Int scale)
        {
            int sizeX = (int)screenSize.x / scale.x;
            int sizeY = (int)screenSize.y / scale.y;
            
            _renderTextureDescriptor.width = sizeX;
            _renderTextureDescriptor.height = sizeY;

            RenderingUtils.ReAllocateHandleIfNeeded(ref handle, _renderTextureDescriptor);
            return handle;
        }

        public RTHandle GetQuarterResAccumulationMap(Vector2 screenSize) =>
            GetRenderTexture(screenSize, ref _cloudQuarterResAccumulationMap, new Vector2Int(1, 1));

        private void OnDisable()
        {
            Debug.Log("De-allocating all resources");
            
            if(_rayMarchMaterial != null)
                Object.Destroy(_rayMarchMaterial);
            
            if(_compositorMaterial != null)
                Object.Destroy(_compositorMaterial);
            
            _cloudQuarterResAccumulationMap?.Release();
            _settingsBuffer?.Dispose();
            _rtHandleSystem?.Dispose();
        }
    }
}