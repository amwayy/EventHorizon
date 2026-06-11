using System;
using System.Collections.Generic;
using System.Linq;
using GameEvent;
using GameEvent.Args;
using LibCSG;
using UnityEngine;

namespace DefaultNamespace
{
    [Serializable]
    public class CuttableJigsawData
    {
        public JigsawSO jigsawData;
        public JigsawCollective jigsawCollective;
        public MeshFilter meshFilter;
    }
    
    public class CuttableWall : MonoBehaviour
    {
        [SerializeField] private int levelId;
        [SerializeField] private MeshFilter wallMeshFilter;
        [SerializeField] private MeshFilter resultBufferA;
        [SerializeField] private MeshFilter resultBufferB;
        [SerializeField] private List<CuttableJigsawData> jigsawCollectives;
        [SerializeField] private float cutScaleFactor = 1f;
        
        private CSGBrush _wallBrush;
        private CSGBrushOperation _csgOp = new();
        private CSGBrush _resultBufferABrush;
        private CSGBrush _resultBufferBBrush;
        private readonly Dictionary<string, CSGBrush> _jigsawBrushes = new();
        private int _bufferParity;
        private bool _hasCut;
        private MeshFilter _activeMeshFilter;
        private Camera _mainCamera;

        private void Awake()
        {
            _activeMeshFilter = wallMeshFilter;
            
            resultBufferA.transform.position = wallMeshFilter.transform.position;
            resultBufferB.transform.position = wallMeshFilter.transform.position;
            resultBufferA.transform.rotation = wallMeshFilter.transform.rotation;
            resultBufferB.transform.rotation = wallMeshFilter.transform.rotation;

            foreach (var collective in jigsawCollectives)
            {
                collective.jigsawCollective.Init(levelId, transform.GetSiblingIndex());
            }
        }

        private void Start()
        {
            _csgOp = new CSGBrushOperation();
            
            _wallBrush = new CSGBrush(wallMeshFilter.gameObject);
            _wallBrush.build_from_mesh(wallMeshFilter.mesh);
            foreach (var jigsawCollectiveData in jigsawCollectives)
            {
                var jigsawBrush = new CSGBrush(jigsawCollectiveData.meshFilter.gameObject);
                jigsawBrush.build_from_mesh(jigsawCollectiveData.meshFilter.mesh);   
                _jigsawBrushes[jigsawCollectiveData.jigsawData.jigsawName] = jigsawBrush;
            }
            
            _resultBufferABrush = new CSGBrush(resultBufferA.gameObject);
            _resultBufferBBrush = new CSGBrush(resultBufferB.gameObject);
            
            _mainCamera = Camera.main;
            
            InitHoles();
            
            EventComponent.Instance.Subscribe(CapturedJigsawEventArgs.EventId, OnCapturedJigsaw);
            EventComponent.Instance.Subscribe(LevelResetEventArgs.EventId, OnLevelReset);
        }

        private void InitHoles()
        {
            var putJigsaws = 
                DataManager.Instance.Load(DataKey.PutJigsaws, new Dictionary<(int, int), SlotJigsawData>());
            foreach (var (_, jigsawData) in putJigsaws)
            {
                if (jigsawData.WallCollectiveDataArray == null) continue;
                foreach (var wallCollectiveData in jigsawData.WallCollectiveDataArray)
                {
                    if (wallCollectiveData.LevelId == levelId &&
                        wallCollectiveData.WallIndex == transform.GetSiblingIndex())
                    {
                        var jigsawCollectiveData = jigsawCollectives.Find(
                            data => data.jigsawData.jigsawName == wallCollectiveData.JigsawName);
                        if (jigsawCollectiveData == null) return;
                        var jigsawCollective = jigsawCollectiveData.jigsawCollective;
                        jigsawCollective.transform.position = wallCollectiveData.Position;
                        jigsawCollective.transform.rotation = wallCollectiveData.Rotation;
                        jigsawCollective.transform.localScale = wallCollectiveData.Scale;
                        
                        // todo: remove repetitive codes
                        var brush = _jigsawBrushes[jigsawCollectiveData.jigsawData.jigsawName];
                        CutJigsawHole(brush);
                        CollectedJigsawsUI.Instance.AddJigsaw(jigsawCollective);
                        (jigsawCollective as WallJigsawCollective).SetJigsawName(jigsawCollectiveData.jigsawData.jigsawName);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            EventComponent.Instance.Unsubscribe(CapturedJigsawEventArgs.EventId, OnCapturedJigsaw);
            EventComponent.Instance.Unsubscribe(LevelResetEventArgs.EventId, OnLevelReset);
        }

        private void OnCapturedJigsaw(object sender, GameEventArgs e)
        {
            if (LevelManager.Instance.CurrentLevelIndex != levelId) return;
            if (e is not CapturedJigsawEventArgs args) return;

            var jigsawCollectiveData = jigsawCollectives.Find(data => data.jigsawData.jigsawName == args.JigsawData.jigsawName);
            if (jigsawCollectiveData == null) return;
            var jigsawCollective = jigsawCollectiveData.jigsawCollective;
            
            if (args.HitGameObject != _activeMeshFilter.gameObject) return;
            
            var ray = _mainCamera.ScreenPointToRay((Vector2)args.BBoxCenter);
            var layerMask = 1 << LayerMask.NameToLayer("Cuttable");
            if (!Physics.Raycast(ray, out var hit, Mathf.Infinity, layerMask, QueryTriggerInteraction.Ignore) || 
                hit.collider.gameObject != _activeMeshFilter.gameObject)
            {
                return;
            }

            jigsawCollective.transform.position = hit.point;
            var isInFront = Vector3.Dot(hit.collider.transform.forward, ray.direction) < 0;
            jigsawCollective.transform.localRotation = Quaternion.Euler(
                jigsawCollective.transform.eulerAngles.x, isInFront ? 180f : 0f, args.Angle);
            jigsawCollective.transform.localScale = Vector3.one;
            var jigsawScreenRect = Utility.GetScreenRect(jigsawCollectiveData.meshFilter.GetComponent<Renderer>(), _mainCamera);
            var scaleFactor = (args.CapturedJigsawRT.width / jigsawScreenRect.width + args.CapturedJigsawRT.height /  jigsawScreenRect.height) * 0.5f;
            scaleFactor *= cutScaleFactor;
            jigsawCollective.transform.localScale = new Vector3(scaleFactor, scaleFactor, 10f);
            
            var brush = _jigsawBrushes[jigsawCollectiveData.jigsawData.jigsawName];
            CutJigsawHole(brush);
            CollectedJigsawsUI.Instance.AddJigsaw(jigsawCollective);
            (jigsawCollective as WallJigsawCollective).SetJigsawName(jigsawCollectiveData.jigsawData.jigsawName);
        }

        private void CutJigsawHole(CSGBrush jigsawBrush)
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
            
            _csgOp.merge_brushes(Operation.OPERATION_SUBTRACTION, subtractedBrush, jigsawBrush, ref resultBrush);
            
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
            foreach (var collectiveData in jigsawCollectives)
            {
                collectiveData.jigsawCollective.ResetState();
                collectiveData.jigsawCollective.gameObject.SetActive(false);
            }
            
            ResetStateInternally();
        }

        public void ResetStateInternally()
        {
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