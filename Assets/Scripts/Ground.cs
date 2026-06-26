using UnityEngine;

namespace DefaultNamespace
{
    public class Ground : MonoBehaviour
    {
        [SerializeField] private float minStripeDensity = 1f;
        [SerializeField] private float maxStripeDensity = 10f;
        
        private static readonly int Density = Shader.PropertyToID("_Density");
        private static readonly int AnchorPos = Shader.PropertyToID("_AnchorWorldPos");
        
        private const float perspectiveDistanceThreshold = 0.4f;
        
        private Renderer _renderer;
        private MaterialPropertyBlock _mpb;
        private Transform _camera;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _mpb = new MaterialPropertyBlock();
        }

        private void Start()
        {
            _camera = Camera.main.transform;
        }

        private void Update()
        {
            if (Vector3.Dot(_camera.forward, transform.up) > 0) return;
            var perspectiveDistance = Vector3.Project(_camera.forward, transform.up).magnitude;
            var density = Mathf.Lerp(minStripeDensity, maxStripeDensity, perspectiveDistance);
            _mpb.SetFloat(Density, density);
            _mpb.SetVector(AnchorPos, _camera.position);
            _renderer.SetPropertyBlock(_mpb);
            
            // Debug.Log($"distance to player: {distanceToPlayer}; density: {density}; anchor pos: {anchorPos}");
        }
    }
}