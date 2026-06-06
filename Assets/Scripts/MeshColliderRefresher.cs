using UnityEngine;

[RequireComponent(typeof(MeshCollider))]
public class MeshColliderRefresher : MonoBehaviour
{
    private MeshCollider _meshCollider;
    private MeshFilter _meshFilter;
    
    private void Awake()
    {
        _meshCollider = GetComponent<MeshCollider>();
        _meshFilter = GetComponent<MeshFilter>();
        
        RefreshCollider();
    }

    public void RefreshCollider()
    {
        _meshCollider.sharedMesh = null;
        var mesh = _meshFilter.mesh;
        if (mesh != null && mesh.vertexCount > 0)
        {
            _meshCollider.sharedMesh = mesh;
        }
    }
}