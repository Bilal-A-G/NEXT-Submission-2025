using System;
using UnityEngine;
using UnityEngine.Serialization;

public class BoundsSetter : MonoBehaviour
{
    [SerializeField] private new MeshRenderer renderer;
    [SerializeField] private Vector3 bounds;
    [SerializeField] private Vector3 center;
    
    void Start()
    {
        renderer.bounds = new Bounds(renderer.transform.position + center, bounds);
    }

    private void OnDrawGizmos()
    {
        renderer.bounds = new Bounds(renderer.transform.position + center, bounds);
        Gizmos.DrawWireCube(renderer.bounds.center, renderer.bounds.extents);
    }
}
