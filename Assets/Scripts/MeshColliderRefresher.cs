using UnityEngine;

[RequireComponent(typeof(MeshCollider))]
public class MeshColliderRefresher : MonoBehaviour
{
    private void Awake()
    {
        var meshCollider = GetComponent<MeshCollider>();
        var mesh = GetComponent<MeshFilter>().sharedMesh;
        
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;
    }
}