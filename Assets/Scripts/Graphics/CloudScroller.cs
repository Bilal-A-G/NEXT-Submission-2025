using UnityEngine;
using Volumetrics.Settings;

public class CloudScroller : MonoBehaviour
{
    [SerializeField] private float scrollSpeed;
    [SerializeField] private float detailScrollSpeed;
    
    [SerializeField] private Vector2 scrollDirection;
    [SerializeField] private Vector2 detailScrollDirection;
    
    [SerializeField] private VolumetricCloudSettingsSo cloudSettings;
    
    void Update()
    {
        cloudSettings.settings.shapeNoiseUVOffset += scrollDirection * (scrollSpeed * Time.deltaTime);
        cloudSettings.settings.detailNoiseUVOffset += detailScrollDirection * (detailScrollSpeed * Time.deltaTime);
    }
}
