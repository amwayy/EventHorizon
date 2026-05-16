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

public class JigsawUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [SerializeField] private JigsawDatabase jigsawDatabase;
    [SerializeField] private RawImage rawImage;
    [SerializeField] private Material subtractMat;
    
    public RectTransform RectTransform => _rectTransform;
    public Color Color => rawImage.color;
    public List<JigsawUI> ConnectedJigsaws { get; private set; } = new();
    
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
    private Rect _visibleRect;
    private RenderTexture _visiblePartRt;
    private bool _isOriginal = true;
    private bool _isBlocked;

    private void Awake()
    {
        _mainCamera = Camera.main;
        
        _rectTransform = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
        _rawImageHandler = rawImage.GetComponent<RawImageAlphaRaycast>();
    }

    private void Update()
    {
        var screenPos = _mainCamera.WorldToScreenPoint(_rectTransform.position);
        var ray = _mainCamera.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out var hit) || !hit.collider.TryGetComponent(out JigsawSlot slot))
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
        _visibleRect = Utility.GetUIRectScreenRect(_rectTransform, _mainCamera);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovering = true;
        _openHandCursorId = CursorStack.Push(NTCursors.OpenHand);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovering = false;
        CursorStack.Pop(_openHandCursorId);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!_isHovering) return;

        _closeHandCursorId = CursorStack.Push(NTCursors.ClosedHand);

        var cam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : _canvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rectTransform.parent as RectTransform,
            eventData.position,
            cam,
            out var localPoint
        );

        _dragOffset = _rectTransform.anchoredPosition - localPoint;
    }

    public void OnDrag(PointerEventData eventData)
    {
        var cam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : _canvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rectTransform.parent as RectTransform,
            eventData.position,
            cam,
            out var localPoint
        );

        _rectTransform.anchoredPosition = localPoint + _dragOffset;
        
        var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out var hit) || !hit.collider.TryGetComponent(out JigsawSlot slot))
        {
            _hoveringSlot = null;
            return;
        }
        _hoveringSlot = slot;
        _hitSlotInFront = Vector3.Dot(hit.collider.transform.forward, ray.direction) < 0;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        CursorStack.Pop(_closeHandCursorId);
        
        _isOriginal = true;
        _isBlocked = false;
        CollectedJigsawsUI.Instance.OnEndDragJigsawUI(this);
        var jigsawRect = Utility.GetUIRectScreenRect(_rectTransform, _mainCamera);
        if (Utility.IsNotFullyInsideScreen(jigsawRect))
        {
            UpdateVisibleArea(isBlocked: false);
        }
        if (_isOriginal)
        {
            _visibleRect = jigsawRect;
            _visibleAreaJigsawData = _originalJigsawData;
        }
        if (!_hoveringSlot) return;

        if (TryPutOnSlot())
        {
            CursorStack.Pop(_openHandCursorId);
            ScreenshotController.Instance.ToggleScreenshotState();
        }
    }

    public void UpdateVisibleArea(bool isBlocked)
    {
        _isBlocked = _isBlocked || isBlocked;
        
        if (!TryGetAnyVisibleScreenPosition(out var visiblePosition)) return;
        _visibleAreaJigsawData = ScreenshotController.Instance.GetSameColorRegionShape(visiblePosition, out var rect, out var rt);
        _visibleRect = rect;
        
        _visiblePartRt?.Release();
        _visiblePartRt = new RenderTexture(rt.width, rt.height, 0, RenderTextureFormat.ARGB32);
        _visiblePartRt.enableRandomWrite = true;
        _visiblePartRt.Create();

        // Use MaskExtract shader to convert: white foreground, transparent background
        var maskMaterial = new Material(Shader.Find("Hidden/MaskToTransparent"));
        Graphics.Blit(rt, _visiblePartRt, maskMaterial);
        Destroy(maskMaterial);
        
        _isOriginal = false;
    }

    private bool TryPutOnSlot()
    {
        if (!_visibleAreaJigsawData.Source) return false;
        var slotRect = Utility.GetUIRectScreenRect(_hoveringSlot.RectTransform, _mainCamera);
        var jigsawRect = Utility.GetJigsawCoreRect(_visibleRect, _visibleAreaJigsawData.Source, _angle);
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
        
        if (_isBlocked)
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
        
        _hoveringSlot.PutJigsaw(putJigsawData, color);
        CollectedJigsawsUI.Instance.PutJigsawOnSlot(this, _hoveringSlot);
        foreach (var connectedJigsaw in ConnectedJigsaws)
        {
            connectedJigsaw.OnPutOnSlot(_hoveringSlot);
        }
        OnPutOnSlot(_hoveringSlot);
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
    
    private bool TryGetAnyVisibleScreenPosition(out Vector2 result)
    {
        var hasVisiblePoint = _rawImageHandler.TryGetAnyVisibleScreenPosition(out result);
        return hasVisiblePoint;
    }
}