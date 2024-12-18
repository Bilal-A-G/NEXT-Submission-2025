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
        private ComputeBuffer _allBounds;
        
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
            int sizeX = (int)screenSize.x + maxMotionVectorStep * 2;
            int sizeY = (int)screenSize.y + maxMotionVectorStep * 2;

            _cloudAccumulation = new RenderTexture(sizeX, sizeY, 0, GraphicsFormat.R16G16B16A16_SFloat);
            _cloudAccumulation.enableRandomWrite = true;
            _cloudAccumulation.Create();
        }

        public void CreateAllBounds(int stride, int count)
        {
            _allBounds = new ComputeBuffer(count, stride);
        }

        public ref Material GetCompositeMaterial() => ref _compositeMaterial;
        public ref RenderTexture GetCloudAccumulation() => ref _cloudAccumulation;
        public ref ComputeBuffer GetAllBounds() => ref _allBounds;

        private void OnDisable()
        {
            Debug.Log("De-allocating all resources");
            
            Object.Destroy(_compositeMaterial);
            Object.Destroy(_cloudAccumulation);
            _allBounds.Dispose();
        }
    }
}