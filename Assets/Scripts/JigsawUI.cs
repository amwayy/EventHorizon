using System.Collections.Generic;
using DefaultNamespace;
using GameEvent.Args;
using Riten.Native.Cursors;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public struct JigsawRuntimeData
{
    public int RotateAngle;
    public JigsawEdgeType UpEdgeType;
    public JigsawEdgeType DownEdgeType;
    public JigsawEdgeType LeftEdgeType;
    public JigsawEdgeType RightEdgeType;

    public JigsawSO Source;
}

public class JigsawUI : MonoBehaviour
{
    [SerializeField] private JigsawDatabase jigsawDatabase;
    [SerializeField] private RawImage rawImage;
    [SerializeField] private Material subtractMat;
    
    public RectTransform RectTransform => _rectTransform;
    public Color Color => rawImage.color;
    public readonly List<JigsawUI> ConnectedJigsaws = new();
    public Rect VisibleRect { get; private set; }
    
    private int _openHandCursorId;
    private int _closeHandCursorId;
    private bool _isHovering;
    private Vector2 _dragOffset;
    private RectTransform _rectTransform;
    private Canvas _canvas;
    private Camera _mainCamera;
    private JigsawSlot _hoveringSlot;
    private JigsawRuntimeData _originalJigsawData;
    private RenderTexture _renderTexture;
    private int _angle;
    private bool _hitSlotInFront;
    private RawImageAlphaRaycast _rawImageHandler;
    private JigsawRuntimeData _visibleAreaJigsawData;
    private RenderTexture _visiblePartRt;
    private bool _isBlocked;
    
    private bool _isDragging;
    private bool _mouseDown;

    private void Awake()
    {
        _mainCamera = Camera.main;
        
        _rectTransform = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
        _rawImageHandler = rawImage.GetComponent<RawImageAlphaRaycast>();
    }

    private void Update()
    {
        HandleHover();
        HandleDrag();
        
        var screenPos = _mainCamera.WorldToScreenPoint(_rectTransform.position);
        var ray = _mainCamera.ScreenPointToRay(screenPos);var layerMask = LayerMask.GetMask("Slot");
        if (!Physics.Raycast(ray, out var hit, Mathf.Infinity, layerMask, QueryTriggerInteraction.Ignore)
            || !hit.collider.TryGetComponent(out JigsawSlot slot))
        {
            _hoveringSlot = null;
        }
        else
        {
            _hoveringSlot = slot;
            _hitSlotInFront = Vector3.Dot(hit.collider.transform.forward, ray.direction) < 0;
            TryPutOnSlot();
        }
    }
    
    private void HandleHover()
    {
        Vector2 mousePos = GameManager.Instance.GetViewportMousePosition();
        var isInside = Utility.IsUIOnTop(mousePos, gameObject) &&
                       _rawImageHandler.IsRaycastLocationValid(mousePos, _canvas.worldCamera);

        if (isInside && !_isHovering)
        {
            OnPointerEnter();
        }
        else if (!isInside && _isHovering)
        {
            OnPointerExit();
        }
    }
    
    private void HandleDrag()
    {
        if (Input.GetMouseButtonDown(0) && _isHovering)
        {
            _isDragging = true;
            OnBeginDrag();
        }

        if (_isDragging)
        {
            OnDrag();
        }

        if ((Input.GetMouseButtonUp(0) || Input.GetMouseButtonDown(1)) && _isDragging)
        {
            _isDragging = false;

            CursorStack.Pop(_closeHandCursorId);

            OnEndDrag();
        }
    }

    public void Init(CapturedJigsawEventArgs args)
    {
        _renderTexture = args.CapturedJigsawRT;
        rawImage.texture = _renderTexture;
        rawImage.rectTransform.sizeDelta = new Vector2(args.CapturedJigsawRT.width, args.CapturedJigsawRT.height);
        rawImage.rectTransform.anchoredPosition = args.BBoxCenter;
        rawImage.color = args.Color;
        _angle = args.Angle;
        _originalJigsawData = Utility.Rotate(args.JigsawData, args.Angle);
        _visibleAreaJigsawData = _originalJigsawData;
        VisibleRect = Utility.GetUIRectScreenRect(_rectTransform, _mainCamera);
    }

    private void OnPointerEnter()
    {
        _isHovering = true;
        _openHandCursorId = CursorStack.Push(NTCursors.OpenHand);
    }

    private void OnPointerExit()
    {
        _isHovering = false;
        CursorStack.Pop(_openHandCursorId);
    }

    private void OnBeginDrag()
    {
        if (!_isHovering) return;

        _closeHandCursorId = CursorStack.Push(NTCursors.ClosedHand);

        var cam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : _canvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rectTransform.parent as RectTransform,
            GameManager.Instance.GetViewportMousePosition(),
            cam,
            out var localPoint
        );

        _dragOffset = _rectTransform.anchoredPosition - localPoint;
    }

    private void OnDrag()
    {
        var cam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : _canvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rectTransform.parent as RectTransform,
            GameManager.Instance.GetViewportMousePosition(),
            cam,
            out var localPoint
        );

        _rectTransform.anchoredPosition = localPoint + _dragOffset;
        
        var ray = _mainCamera.ScreenPointToRay(GameManager.Instance.GetViewportMousePosition());
        var layerMask = LayerMask.GetMask("Slot");
        if (!Physics.Raycast(ray, out var hit, Mathf.Infinity, layerMask, QueryTriggerInteraction.Ignore) 
            || !hit.collider.TryGetComponent(out JigsawSlot slot))
        {
            _hoveringSlot = null;
            return;
        }
        _hoveringSlot = slot;
        _hitSlotInFront = Vector3.Dot(hit.collider.transform.forward, ray.direction) < 0;
    }

    private void OnEndDrag()
    {
        CursorStack.Pop(_closeHandCursorId);
        
        MarkBlocked(false);
        CollectedJigsawsUI.Instance.OnEndDragJigsawUI(this);
        var jigsawRect = Utility.GetUIRectScreenRect(_rectTransform, _mainCamera);
        if (Utility.IsNotFullyInsideScreen(jigsawRect))
        {
            MarkBlocked(true);
        }
        if (!_isBlocked && ConnectedJigsaws.Count == 0)
        {
            VisibleRect = jigsawRect;
            _visibleAreaJigsawData = _originalJigsawData;
        }
        if (!_hoveringSlot) return;

        if (TryPutOnSlot())
        {
            CursorStack.Pop(_openHandCursorId);
            ScreenshotController.Instance.ToggleScreenshotState();
        }
    }

    public void MarkBlocked(bool isBlocked)
    {
        _isBlocked = isBlocked;
    }

    public void UpdateUIVisibleArea()
    {
        if (!TryGetAnyVisibleScreenPosition(out var visiblePosition)) return;
        var layerMask = LayerMask.GetMask("JigsawUI");
        _visibleAreaJigsawData = ScreenshotController.Instance.GetSameColorRegionShape(
            visiblePosition, out var rect, out var rt, out var angle, clearOutline: true, layerMask);
        VisibleRect = rect;
        _angle = angle;
        if (!rt) return;
        
        _visiblePartRt?.Release();
        _visiblePartRt = new RenderTexture(rt.width, rt.height, 0, RenderTextureFormat.ARGB32);
        _visiblePartRt.enableRandomWrite = true;
        _visiblePartRt.Create();

        // Use MaskExtract shader to convert: white foreground, transparent background
        var maskMaterial = new Material(Shader.Find("Hidden/MaskToTransparent"));
        Graphics.Blit(rt, _visiblePartRt, maskMaterial);
        Destroy(maskMaterial);
    }

    private bool TryPutOnSlot()
    {
        if (!_visibleAreaJigsawData.Source) return false;
        var slotRect = Utility.GetUIRectScreenRect(_hoveringSlot.RectTransform, _mainCamera);
        var jigsawRect = Utility.GetJigsawCoreRect(VisibleRect, _visibleAreaJigsawData.Source, _angle);
        var iou = Utility.IoU(slotRect, jigsawRect);

        if (Mathf.Abs(iou - 1) > 0.15f)
        {
            return false;
        }
        var angle = _angle + (_hitSlotInFront ? 180 : 0);
        var putJigsawData = Utility.Rotate(_visibleAreaJigsawData.Source, angle);
        if (!_hoveringSlot.CanPut(putJigsawData))
        {
            return false;
        }
        
        var color = rawImage.color;
        PutOnSlot(putJigsawData, color);
        return true;
    }

    private void OnPutOnSlot(JigsawSlot slot)
    {
        CollectedJigsawsUI.Instance.PutJigsawOnSlot(this, slot);

        var isIndependent = ConnectedJigsaws.Count == 0;
        if (_isBlocked && !isIndependent)
        {
            DoMask(rawImage.texture as RenderTexture, _visiblePartRt);   
        }
        else
        {
            Hide();
        }
    }

    private void PutOnSlot(JigsawRuntimeData putJigsawData, Color color)
    {
        if (!_hoveringSlot) return;
        
        OnPutOnSlot(_hoveringSlot);
        foreach (var connectedJigsaw in ConnectedJigsaws)
        {
            connectedJigsaw.OnPutOnSlot(_hoveringSlot);
        }
        _hoveringSlot.PutJigsaw(putJigsawData, color);
        
        AudioManager.Instance.Play(SoundGroup.Put);
    }

    public void Hide()
    {
        (rawImage.texture as RenderTexture)?.Release();
        gameObject.SetActive(false);
    }

    private void DoMask(RenderTexture rtA, RenderTexture rtB)
    {
        var result = new RenderTexture(rtA.width, rtA.height, 0, RenderTextureFormat.ARGB32);
        result.enableRandomWrite = true;
        result.Create();

        subtractMat.SetTexture("_MainTex", rtA);
        subtractMat.SetTexture("_MaskTex", rtB);
        Graphics.Blit(rtA, result, subtractMat);
        
        rtA.Release();
        rtB.Release();

        rawImage.texture = result;
    }
    
    public bool TryGetAnyVisibleScreenPosition(out Vector2 result)
    {
        var hasVisiblePoint = _rawImageHandler.TryGetAnyVisibleScreenPosition(out result);
        return hasVisiblePoint;
    }

    public bool IsConnectedWithWorld(bool clearOutline = false)
    {
        if (!TryGetAnyVisibleScreenPosition(out var visiblePosition)) return false;
        ScreenshotController.Instance.GetSameColorRegionShape(
            visiblePosition, out _, out var uiRt, out _, clearOutline: true, layer: LayerMask.GetMask("JigsawUI"));
        ScreenshotController.Instance.GetSameColorRegionShape(
            visiblePosition, out _, out var worldRt, out _, clearOutline: clearOutline);
        var uiRtPixelCount = GPUComputeHelper.Instance.GetPixelCount(uiRt);
        var worldRtPixelCount = GPUComputeHelper.Instance.GetPixelCount(worldRt);
        return Mathf.Abs(1 - (float)worldRtPixelCount / uiRtPixelCount) > 0.1f;
    }
}