using GameEvent;
using GameEvent.Args;
using LibCSG;
using UnityEngine;

namespace DefaultNamespace
{
    public class CuttableWall : MonoBehaviour
    {
        [SerializeField] private int levelId;
        [SerializeField] private MeshFilter jigsawMeshFilter;
        [SerializeField] private MeshFilter wallMeshFilter;
        [SerializeField] private MeshFilter resultBufferA;
        [SerializeField] private MeshFilter resultBufferB;
        [SerializeField] private JigsawCollective jigsawCollective;
        
        private CSGBrush _wallBrush;
        private CSGBrushOperation _csgOp = new();
        private CSGBrush _resultBufferABrush;
        private CSGBrush _resultBufferBBrush;
        private CSGBrush _jigsawBrush;
        private int _bufferParity;
        private bool _hasCut;
        private MeshFilter _activeMeshFilter;
        private Camera _mainCamera;

        private void Awake()
        {
            _activeMeshFilter = wallMeshFilter;
            
            resultBufferA.transform.position = wallMeshFilter.transform.position;
            resultBufferB.transform.position = wallMeshFilter.transform.position;
        }

        private void Start()
        {
            _csgOp = new CSGBrushOperation();
            
            _wallBrush = new CSGBrush(wallMeshFilter.gameObject);
            _wallBrush.build_from_mesh(wallMeshFilter.mesh);
            _jigsawBrush = new CSGBrush(jigsawMeshFilter.gameObject);
            _jigsawBrush.build_from_mesh(jigsawMeshFilter.mesh);
            
            _resultBufferABrush = new CSGBrush(resultBufferA.gameObject);
            _resultBufferBBrush = new CSGBrush(resultBufferB.gameObject);
            
            _mainCamera = Camera.main;
            
            EventComponent.Instance.Subscribe(CapturedJigsawEventArgs.EventId, OnCapturedJigsaw);
            EventComponent.Instance.Subscribe(LevelResetEventArgs.EventId, OnLevelReset);
        }

        private void OnDestroy()
        {
            EventComponent.Instance.Unsubscribe(CapturedJigsawEventArgs.EventId, OnCapturedJigsaw);
            EventComponent.Instance.Unsubscribe(LevelResetEventArgs.EventId, OnLevelReset);
        }

        private void OnCapturedJigsaw(object sender, GameEventArgs e)
        {
            if (e is not CapturedJigsawEventArgs args) return;
            
            var ray = _mainCamera.ScreenPointToRay((Vector2)args.BBoxCenter);
            var layerMask = 1 << LayerMask.NameToLayer("Cuttable");
            if (!Physics.Raycast(ray, out var hit, Mathf.Infinity, layerMask, QueryTriggerInteraction.Ignore) || 
                hit.collider.gameObject != _activeMeshFilter.gameObject)
            {
                if (hit.collider != null) Debug.Log($"missed cut raycast. collider: {hit.collider}");
                return;
            }

            jigsawCollective.transform.position = hit.point;
            jigsawCollective.transform.localScale = Vector3.one;
            var jigsawScreenRect = Utility.GetScreenRect(jigsawMeshFilter.GetComponent<Renderer>(), _mainCamera);
            var scaleFactor = (args.CapturedJigsawRT.width / jigsawScreenRect.width + args.CapturedJigsawRT.height /  jigsawScreenRect.height) * 0.5f;
            jigsawCollective.transform.localScale = new Vector3(scaleFactor, scaleFactor, 10f);
            CutJigsawHole();
            CollectedJigsawsUI.Instance.AddJigsaw(jigsawCollective);
        }

        private void CutJigsawHole()
        {
            CSGBrush subtractedBrush;
            CSGBrush resultBrush;
            MeshFilter resultMeshFilter;
            if (!_hasCut)
            {
                subtractedBrush = _wallBrush;
                resultBrush = _resultBufferABrush;
                resultMeshFilter = resultBufferA;
                _hasCut = true;
                wallMeshFilter.gameObject.SetActive(false);
            }
            else
            {
                subtractedBrush = _bufferParity == 0 ? _resultBufferABrush : _resultBufferBBrush;   
                resultBrush = _bufferParity == 0 ? _resultBufferBBrush : _resultBufferABrush;
                resultMeshFilter = _bufferParity == 0 ? resultBufferB : resultBufferA;
                var bufferedMeshFilter = _bufferParity == 0 ? resultBufferA : resultBufferB;
                bufferedMeshFilter.gameObject.SetActive(false);
                
                _bufferParity = 1 - _bufferParity;
            }
            resultMeshFilter.gameObject.SetActive(true);
            
            _csgOp.merge_brushes(Operation.OPERATION_SUBTRACTION, subtractedBrush, _jigsawBrush, ref resultBrush);
            
            resultMeshFilter.mesh.Clear();
            resultBrush.getMesh(resultMeshFilter.mesh);
            
            _activeMeshFilter = resultMeshFilter;
            _activeMeshFilter.GetComponent<MeshColliderRefresher>().RefreshCollider();
        }

        private void OnLevelReset(object sender, GameEventArgs e)
        {
            if (LevelManager.Instance.CurrentLevelIndex != levelId) return;
            
            ResetState();
        }

        private void ResetState()
        {
            jigsawCollective.ResetState();

            if (_hasCut)
            {
                _hasCut = false;
                wallMeshFilter.gameObject.SetActive(true);
                _activeMeshFilter.gameObject.SetActive(false);
                _activeMeshFilter = wallMeshFilter;
                _bufferParity = 0;
            }
        }
    }
}