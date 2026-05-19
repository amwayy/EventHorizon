using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameEvent;
using GameEvent.Args;
using UnityEngine;
using UnityEngine.Assertions;

namespace DefaultNamespace
{
    public class CollectedJigsawsUI : MonoBehaviour
    {
        [SerializeField] private JigsawUI jigsawUIPrefab;
        [SerializeField] private ComputeShader alphaIoUCompute;

        public static CollectedJigsawsUI Instance { get; private set; }

        private readonly Dictionary<JigsawCollective, JigsawUI> _collectedJigsaws = new();
        private readonly Dictionary<JigsawSlot, List<JigsawUI>> _putJigsaws = new();

        private JigsawUI _lastJigsawUI;
        private Camera _mainCamera;
        private ComputeBuffer _resultsBuffer;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            _mainCamera = Camera.main;
            
            EventComponent.Instance.Subscribe(CapturedJigsawEventArgs.EventId, OnCapturedJigsaw);
        }

        private void OnDestroy()
        {
            EventComponent.Instance.Unsubscribe(CapturedJigsawEventArgs.EventId, OnCapturedJigsaw);
            _resultsBuffer?.Release();
        }

        private void OnCapturedJigsaw(object sender, GameEventArgs e)
        {
            if (e is not CapturedJigsawEventArgs args) return;
            
            CheckCapturedJigsawOverlappingCollection(args);
            
            var jigsawUI = Instantiate(jigsawUIPrefab, transform);
            jigsawUI.Init(args);
            _lastJigsawUI = jigsawUI;   
        }

        private void CheckCapturedJigsawOverlappingCollection(CapturedJigsawEventArgs args)
        {
            var captureRect = new Rect(
                args.BBoxCenter.x - args.CapturedJigsawRT.width * 0.5f,
                args.BBoxCenter.y - args.CapturedJigsawRT.height * 0.5f,
                args.CapturedJigsawRT.width,
                args.CapturedJigsawRT.height
            );

            foreach (var (_, jigsawUI) in _collectedJigsaws)
            {
                if (!jigsawUI.gameObject.activeSelf) continue;

                var collectionRect = Utility.GetUIRectScreenRect(jigsawUI.RectTransform, _mainCamera);

                // First check if rects overlap at all (fast rejection)
                if (!captureRect.Overlaps(collectionRect))
                    continue;

                // Get the existing jigsaw's RenderTexture
                var existingRT = jigsawUI.GetComponent<UnityEngine.UI.RawImage>()?.texture as RenderTexture;
                if (existingRT == null)
                    continue;

                // Check for alpha overlap using IoU
                const float iouThreshold = 0.1f; // Adjust this threshold as needed
                var iou = ComputeAlphaIoU(args.CapturedJigsawRT, captureRect, existingRT, collectionRect);
                if (iou > iouThreshold)
                {
                    HideJigsaw(jigsawUI);
                }
            }
        }

        private float ComputeAlphaIoU(RenderTexture rt1, Rect rect1, RenderTexture rt2, Rect rect2)
        {
            if (alphaIoUCompute == null)
            {
                Debug.LogError("AlphaIoUCompute shader not assigned!");
                return 0f;
            }

            // Calculate the union rect (bounding box of both rects)
            var xMin = Mathf.Min(rect1.xMin, rect2.xMin);
            var xMax = Mathf.Max(rect1.xMax, rect2.xMax);
            var yMin = Mathf.Min(rect1.yMin, rect2.yMin);
            var yMax = Mathf.Max(rect1.yMax, rect2.yMax);

            var unionRect = new Rect(xMin, yMin, xMax - xMin, yMax - yMin);

            const int sampleStep = 4;
            const float alphaThreshold = 0.1f;

            // Initialize compute buffer if needed
            if (_resultsBuffer == null || _resultsBuffer.count != 2)
            {
                _resultsBuffer?.Release();
                _resultsBuffer = new ComputeBuffer(2, sizeof(uint));
            }

            // Clear results buffer
            _resultsBuffer.SetData(new uint[] { 0, 0 });

            // Set compute shader parameters
            int kernel = alphaIoUCompute.FindKernel("CSMain");
            alphaIoUCompute.SetTexture(kernel, "Texture1", rt1);
            alphaIoUCompute.SetTexture(kernel, "Texture2", rt2);
            alphaIoUCompute.SetBuffer(kernel, "Results", _resultsBuffer);
            alphaIoUCompute.SetVector("Rect1", new Vector4(rect1.x, rect1.y, rect1.width, rect1.height));
            alphaIoUCompute.SetVector("Rect2", new Vector4(rect2.x, rect2.y, rect2.width, rect2.height));
            alphaIoUCompute.SetVector("UnionRect", new Vector4(unionRect.x, unionRect.y, unionRect.width, unionRect.height));
            alphaIoUCompute.SetVector("Texture1Size", new Vector2(rt1.width, rt1.height));
            alphaIoUCompute.SetVector("Texture2Size", new Vector2(rt2.width, rt2.height));
            alphaIoUCompute.SetFloat("AlphaThreshold", alphaThreshold);
            alphaIoUCompute.SetInt("SampleStep", sampleStep);

            // Calculate thread groups
            int threadGroupsX = Mathf.CeilToInt(unionRect.width / sampleStep / 8f);
            int threadGroupsY = Mathf.CeilToInt(unionRect.height / sampleStep / 8f);

            // Dispatch compute shader
            alphaIoUCompute.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);

            // Read results back
            var results = new uint[2];
            _resultsBuffer.GetData(results);

            uint intersectionCount = results[0];
            uint unionCount = results[1];

            // Calculate IoU
            if (unionCount == 0)
                return 0f;

            return (float)intersectionCount / unionCount;
        }

        public void AddJigsaw(JigsawCollective collective)
        {
            StartCoroutine(DelayAddJigsaw(collective));
        }

        private IEnumerator DelayAddJigsaw(JigsawCollective collective)
        {
            yield return null;
            
            Assert.IsTrue(_lastJigsawUI);
            if (!_lastJigsawUI) yield break;
            
            _collectedJigsaws[collective] = _lastJigsawUI;
            _lastJigsawUI = null;
        }

        public void PutJigsawOnSlot(JigsawUI jigsawUI, JigsawSlot slot)
        {
            _putJigsaws.TryAdd(slot, new List<JigsawUI>());
            _putJigsaws[slot].Add(jigsawUI);
        }

        public List<JigsawCollective> GetSlotCollective(JigsawSlot slot)
        {
            List<JigsawUI> targetJigsawUIs = null;
            foreach (var (putSlot, jigsawUI) in _putJigsaws)
            {
                if (putSlot == slot)
                {
                    targetJigsawUIs = jigsawUI;
                    break;
                }
            }
            if (targetJigsawUIs == null || targetJigsawUIs.Count == 0) return null;
            List<JigsawCollective> targetCollectives = new();
            foreach (var (collective, collectedJigsawUI) in _collectedJigsaws)
            {
                if (targetJigsawUIs.Contains(collectedJigsawUI))
                {
                    targetCollectives.Add(collective);
                }
            }
            return targetCollectives;
        }
        
        public void OnResetCollective(JigsawCollective collective)
        {
            if (_collectedJigsaws.TryGetValue(collective, out var jigsawUI))
            {
                HideJigsaw(jigsawUI);
                
                _collectedJigsaws.Remove(collective);
            }
            
            var putJigsawsData = 
                DataManager.Instance.Load(DataKey.PutJigsaws, new Dictionary<(int, int), SlotJigsawData>());
            foreach (var (slotIndex, data) in putJigsawsData)
            {
                if (data.CollectiveIndexes.Contains((collective.LevelId, collective.CollectiveIndex)))
                {
                    LevelManager.Instance.ResetLevelSlot(slotIndex.Item1, slotIndex.Item2);
                    foreach (var collectiveIndex in data.CollectiveIndexes)
                    {
                        if (collectiveIndex == (collective.LevelId, collective.CollectiveIndex)) continue;
                        LevelManager.Instance.ResetLevelCollective(collectiveIndex.Item1, collectiveIndex.Item2, false);
                    }
                    break;
                }
            }
        }

        public void OnResetSlot(JigsawSlot slot)
        {
            if (!_putJigsaws.TryGetValue(slot, out var jigsaws)) return;

            foreach (var jigsaw in jigsaws)
            {
                HideJigsaw(jigsaw);
            }
            
            _putJigsaws.Remove(slot);
            
            var putJigsawsData = 
                DataManager.Instance.Load(DataKey.PutJigsaws, new Dictionary<(int, int), SlotJigsawData>());
            foreach (var (slotIndex, data) in putJigsawsData)
            {
                if (slotIndex == (slot.LevelId, slot.Index))
                {
                    foreach (var collectiveIndex in data.CollectiveIndexes)
                    {
                        LevelManager.Instance.ResetLevelCollective(collectiveIndex.Item1, collectiveIndex.Item2);
                    }
                    break;
                }
            }
        }

        public void ResetCollection()
        {
            foreach (var (collective, jigsawUI) in _collectedJigsaws)
            {
                if (!jigsawUI.gameObject.activeSelf) continue;
                HideJigsaw(jigsawUI);
                collective.ResetState(sendNotification: false);
            }

            foreach (Transform jigsaw in transform)
            {
                jigsaw.gameObject.SetActive(false);
            }
        }

        public void OnEndDragJigsawUI(JigsawUI jigsawUI)
        {
            foreach (var (_, collectedJigsawUI) in _collectedJigsaws)
            {
                if (!collectedJigsawUI.gameObject.activeSelf) continue;
                collectedJigsawUI.UpdateVisibleArea();
                if (collectedJigsawUI == jigsawUI) continue;

                if (collectedJigsawUI.ConnectedJigsaws.Contains(jigsawUI))
                {
                    collectedJigsawUI.ConnectedJigsaws.Remove(jigsawUI);
                }
                
                var collectedJigsawRect = Utility.GetUIRectScreenRect(collectedJigsawUI.RectTransform, _mainCamera);
                var draggingJigsawRect = Utility.GetUIRectScreenRect(jigsawUI.RectTransform, _mainCamera);
                if (!collectedJigsawRect.Overlaps(draggingJigsawRect))
                {
                    continue;
                }
                
                if (Utility.IsSameColor(jigsawUI.Color, collectedJigsawUI.Color))
                {
                    jigsawUI.MarkBlocked(false);
                    collectedJigsawUI.MarkBlocked(false);
                    collectedJigsawUI.MarkOriginal(false);
                    collectedJigsawUI.MarkOriginal(false);
                    
                    jigsawUI.ConnectedJigsaws.Add(collectedJigsawUI);
                    collectedJigsawUI.ConnectedJigsaws.Add(jigsawUI);
                }
                else
                {
                    if (jigsawUI.transform.GetSiblingIndex() < collectedJigsawUI.transform.GetSiblingIndex())
                    {
                        // dragging jigsaw is underneath
                        jigsawUI.MarkBlocked(true);
                    }
                    else
                    {
                        // dragging jigsaw is above
                        collectedJigsawUI.MarkBlocked(true);
                    }
                }
            }
        }

        private void HideJigsaw(JigsawUI jigsawUI)
        {
            jigsawUI.Hide();
        }
    }
}