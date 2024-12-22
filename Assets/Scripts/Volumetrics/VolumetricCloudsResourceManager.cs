using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;
using Screen = UnityEngine.Device.Screen;

namespace Volumetrics
{
    public class VolumetricCloudsResourceManager : MonoBehaviour
    {
        private static VolumetricCloudsResourceManager _instance;
        private Material _compositeMaterial;
        private ComputeBuffer _allBounds;

        private RTHandleSystem _rtHandleSystem;
        private RTHandle _cloudAccumulationMap;
        private RTHandle _finalCloudAccumulationMap;
        private RTHandle _accumulatedCloudMotionVectorsMap;
        private RTHandle _cloudDepthMap;
        private RTHandle _cloudTransmittanceMap;

        private RTHandle _cloudDoubleBufferFront;
        private RTHandle _cloudDoubleBufferBack;
        private RTHandle _cloudQuarterResAccumulationMap;

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

        public void CreateCompositeMaterial(Shader shader)
        {
            _compositeMaterial = new Material(shader);
        }

        public RTHandle GetQuarterResAccumulationMap(Vector2 screenSize)
        {
            int sizeX = (int)screenSize.x / 4; 
            int sizeY = (int)screenSize.y / 4;
            
            RenderTextureDescriptor quarterResDescriptor = _renderTextureDescriptor;
            quarterResDescriptor.width = sizeX;
            quarterResDescriptor.height = sizeY;
            
            RenderingUtils.ReAllocateHandleIfNeeded(ref _cloudQuarterResAccumulationMap, quarterResDescriptor);
            return _cloudQuarterResAccumulationMap;
        }

        public RTHandle GetCloudFrontBuffer(Vector2 screenSize)
        {
            int sizeX = (int)screenSize.x;
            int sizeY = (int)screenSize.y;

            _renderTextureDescriptor.width = sizeX;
            _renderTextureDescriptor.height = sizeY;
            
            RenderingUtils.ReAllocateHandleIfNeeded(ref _cloudDoubleBufferFront, _renderTextureDescriptor);
            return _cloudDoubleBufferFront;
        }
        
        public RTHandle GetCloudBackBuffer(Vector2 screenSize)
        {
            int sizeX = (int)screenSize.x;
            int sizeY = (int)screenSize.y;

            _renderTextureDescriptor.width = sizeX;
            _renderTextureDescriptor.height = sizeY;
            
            RenderingUtils.ReAllocateHandleIfNeeded(ref _cloudDoubleBufferBack, _renderTextureDescriptor);
            return _cloudDoubleBufferBack;
        }

        public RTHandle GetCloudAccumulation(Vector2 screenSize)
        {
            int sizeX = (int)screenSize.x;
            int sizeY = (int)screenSize.y;

            _renderTextureDescriptor.width = sizeX;
            _renderTextureDescriptor.height = sizeY;
            
            RenderingUtils.ReAllocateHandleIfNeeded(ref _cloudAccumulationMap, _renderTextureDescriptor);
            return _cloudAccumulationMap;
        }

        public RTHandle GetFinalCloudAccumulation(Vector2 screenSize)
        {
            int sizeX = (int)screenSize.x;
            int sizeY = (int)screenSize.y;

            _renderTextureDescriptor.width = sizeX;
            _renderTextureDescriptor.height = sizeY;
            
            RenderingUtils.ReAllocateHandleIfNeeded(ref _finalCloudAccumulationMap, _renderTextureDescriptor);
            return _finalCloudAccumulationMap;
        }

        public RTHandle GetCloudDepth(Vector2 screenSize)
        {
            int sizeX = (int)screenSize.x;
            int sizeY = (int)screenSize.y;

            _renderTextureDescriptor.width = sizeX;
            _renderTextureDescriptor.height = sizeY;
            
            RenderingUtils.ReAllocateHandleIfNeeded(ref _cloudDepthMap, _renderTextureDescriptor);
            return _cloudDepthMap;
        }
        
        public RTHandle GetCloudTransmission(Vector2 screenSize)
        {
            int sizeX = (int)screenSize.x;
            int sizeY = (int)screenSize.y;

            _renderTextureDescriptor.width = sizeX;
            _renderTextureDescriptor.height = sizeY;
            
            RenderingUtils.ReAllocateHandleIfNeeded(ref _cloudTransmittanceMap, _renderTextureDescriptor);
            return _cloudTransmittanceMap;
        }
        
        public RTHandle GetAccumulatedCloudMotionVectors(Vector2 screenSize)
        {
            int sizeX = (int)screenSize.x;
            int sizeY = (int)screenSize.y;

            _renderTextureDescriptor.width = sizeX;
            _renderTextureDescriptor.height = sizeY;
            
            RenderingUtils.ReAllocateHandleIfNeeded(ref _accumulatedCloudMotionVectorsMap, _renderTextureDescriptor);
            return _accumulatedCloudMotionVectorsMap;
        }

        public void CreateAllBounds(int stride, int count)
        {
            _allBounds?.Dispose();

            _allBounds = new ComputeBuffer(count, stride);
        }

        public ref Material GetCompositeMaterial() => ref _compositeMaterial;
        public ref ComputeBuffer GetAllBounds() => ref _allBounds;

        private void OnDisable()
        {
            Debug.Log("De-allocating all resources");
            
            if(_compositeMaterial != null)
                Object.Destroy(_compositeMaterial);
            
            _cloudAccumulationMap?.Release();
            _cloudTransmittanceMap?.Release();
            _cloudDepthMap?.Release();
            _allBounds?.Dispose();
            _rtHandleSystem?.Dispose();
        }
    }
}