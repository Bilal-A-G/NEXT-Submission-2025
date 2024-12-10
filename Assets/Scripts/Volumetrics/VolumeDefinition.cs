using UnityEngine;

namespace Volumetrics
{
    public class VolumeDefinition : MonoBehaviour
    {
        [SerializeField] private Color visualizeColor;
    
        private void OnDrawGizmos()
        {
            Gizmos.color = visualizeColor;
            Gizmos.DrawWireCube(transform.position, transform.localScale);
        }
    }
}
