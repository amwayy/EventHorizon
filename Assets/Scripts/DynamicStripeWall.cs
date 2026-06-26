using UnityEngine;

namespace DefaultNamespace
{
    public class DynamicStripeWall : MonoBehaviour
    {
        [SerializeField] private float minStripeDensity = 1f;
        [SerializeField] private float maxStripeDensity = 20f;
        [SerializeField] private Vector3 normalDir = Vector3.up;
        [SerializeField] private Vector3 horizontalDir = Vector3.forward;
        
        private static readonly int Density = Shader.PropertyToID("_Density");
        private static readonly int AnchorPos = Shader.PropertyToID("_AnchorWorldPos");
        
        private Renderer _renderer;
        private MaterialPropertyBlock _mpb;
        private Transform _camera;
        private Vector3 _normalDir;
        private Vector3 _horizontalDir;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _mpb = new MaterialPropertyBlock();
            
            _normalDir = transform.TransformDirection(normalDir).normalized;
            _horizontalDir = transform.TransformDirection(horizontalDir).normalized;
        }

        private void Start()
        {
            _camera = Camera.main.transform;
        }

        private void Update()
        {
            var dirToPlayer = _camera.position - transform.position;
            var distanceToPlayer = Mathf.Abs(Vector3.Dot(dirToPlayer, _normalDir.normalized));
            var perspectiveDistance = Vector3.Project(_camera.forward, _normalDir).magnitude;
            var t = (1 / distanceToPlayer) * perspectiveDistance;
            var density = Mathf.Lerp(minStripeDensity, maxStripeDensity, t);
            _mpb.SetFloat(Density, density);
            
            var anchorPos = transform.position + Vector3.Project(dirToPlayer, _horizontalDir);
            _mpb.SetVector(AnchorPos, anchorPos);
            _renderer.SetPropertyBlock(_mpb);
            
            // Debug.Log($"distance to player: {distanceToPlayer}; density: {density}; anchor pos: {anchorPos}");
        }
    }
}