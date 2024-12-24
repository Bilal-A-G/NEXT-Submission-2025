using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Object = UnityEngine.Object;

namespace Volumetrics
{
    public class VolumetricCloudsResourceManager : MonoBehaviour
    {
        private static VolumetricCloudsResourceManager _instance;
        private RenderTexture _cloudAccumulation;
        private Material _compositeMaterial;
<<<<<<< Updated upstream
        private ComputeBuffer _allBounds;
=======

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
>>>>>>> Stashed changes
        
        public static VolumetricCloudsResourceManager GetInstance() => _instance;

        private void Awake()
        {
            if (_instance == null)
                _instance = this;
            else
            {
                Destroy(gameObject);
            }
        }

        public void CreateCompositeMaterial(Shader shader)
        {
            _compositeMaterial = new Material(shader);
        }

        public void CreateCloudAccumulation(Vector2 screenSize, int maxMotionVectorStep)
        {
<<<<<<< Updated upstream
            int sizeX = (int)screenSize.x + maxMotionVectorStep * 2;
            int sizeY = (int)screenSize.y + maxMotionVectorStep * 2;
=======
            int sizeX = (int)screenSize.x; 
            int sizeY = (int)screenSize.y;
            
            RenderTextureDescriptor quarterResDescriptor = _renderTextureDescriptor;
            quarterResDescriptor.width = sizeX;
            quarterResDescriptor.height = sizeY;
            
            RenderingUtils.ReAllocateHandleIfNeeded(ref _cloudQuarterResAccumulationMap, quarterResDescriptor);
            return _cloudQuarterResAccumulationMap;
        }
>>>>>>> Stashed changes

            _cloudAccumulation = new RenderTexture(sizeX, sizeY, 0, GraphicsFormat.R16G16B16A16_SFloat);
            _cloudAccumulation.enableRandomWrite = true;
            _cloudAccumulation.Create();
        }

<<<<<<< Updated upstream
        public void CreateAllBounds(int stride, int count)
        {
            _allBounds = new ComputeBuffer(count, stride);
        }

        public ref Material GetCompositeMaterial() => ref _compositeMaterial;
        public ref RenderTexture GetCloudAccumulation() => ref _cloudAccumulation;
        public ref ComputeBuffer GetAllBounds() => ref _allBounds;
=======
        public ref Material GetCompositeMaterial() => ref _compositeMaterial;
>>>>>>> Stashed changes

        private void OnDisable()
        {
            Debug.Log("De-allocating all resources");
            
<<<<<<< Updated upstream
            Object.Destroy(_compositeMaterial);
            Object.Destroy(_cloudAccumulation);
            _allBounds.Dispose();
=======
            if(_compositeMaterial != null)
                Object.Destroy(_compositeMaterial);
            
            _cloudAccumulationMap?.Release();
            _cloudTransmittanceMap?.Release();
            _cloudDepthMap?.Release();
            _rtHandleSystem?.Dispose();
>>>>>>> Stashed changes
        }
    }
}