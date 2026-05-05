using DG.Tweening;
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

[RequireComponent(typeof(Outline))]
public class JigsawUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [SerializeField] private JigsawDatabase jigsawDatabase;
    [SerializeField] private RawImage rawImage;
    
    public RectTransform RectTransform => _rectTransform;
    
    private Outline _outline;
    private int _openHandCursorId;
    private int _closeHandCursorId;
    private bool _isHovering;
    private Vector2 _dragOffset;
    private RectTransform _rectTransform;
    private Canvas _canvas;
    private Camera _mainCamera;
    private JigsawSlot _hoveringSlot;
    private JigsawRuntimeData _jigsawData;
    private RenderTexture _renderTexture;

    private void Awake()
    {
        _mainCamera = Camera.main;
        
        _outline = GetComponent<Outline>();
        _rectTransform = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
    }

    private void OnEnable()
    {
        _outline.enabled = false;
    }

    public void Init(CapturedJigsawEventArgs args)
    {
        _renderTexture = args.CapturedJigsawRT;
        rawImage.texture = _renderTexture;
        rawImage.rectTransform.sizeDelta = new Vector2(args.CapturedJigsawRT.width, args.CapturedJigsawRT.height);
        rawImage.rectTransform.anchoredPosition = args.BBoxCenter;
        rawImage.color = args.Color;
        _jigsawData = Rotate(args.JigsawData, args.Angle);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovering = true;
        _outline.enabled = true;
        _openHandCursorId = CursorStack.Push(NTCursors.OpenHand);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovering = false;
        _outline.enabled = false;
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
        
        if (_hoveringSlot)
        {
            _hoveringSlot.Unhighlight();
        }
        var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out var hit)) return;
        if (!hit.collider.TryGetComponent(out JigsawSlot slot))
        {
            _hoveringSlot = null;
            return;
        }
        slot.Highlight();
        _hoveringSlot = slot;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        CursorStack.Pop(_closeHandCursorId);

        if (!_hoveringSlot) return;

        var slotRect = Utility.GetUIRectScreenRect(_hoveringSlot.RectTransform, _mainCamera);
        var jigsawRect = Utility.GetUIRectScreenRect(RectTransform, null);
        jigsawRect = Utility.GetJigsawCoreRect(jigsawRect, _jigsawData.Source);
        var iou = Utility.IoU(slotRect, jigsawRect);
        Debug.Log("iou: " + iou);

        if (Mathf.Abs(iou - 1) > 0.1f || !_hoveringSlot.CanPut(_jigsawData))
        {
            _outline.DOColor(Color.red, 0.2f).SetLoops(4, LoopType.Yoyo).SetUpdate(true).SetEase(Ease.Linear);
            _hoveringSlot.Unhighlight();
            return;
        }
        
        _hoveringSlot.PutJigsaw(_jigsawData);
        ScreenshotController.Instance.ToggleScreenshotState();
        CursorStack.Pop(_openHandCursorId);
        gameObject.SetActive(false);
        _renderTexture.Release();
    }
    
    public static JigsawRuntimeData Rotate(JigsawSO data, int angle)
    {
        int steps = ((angle % 360) + 360) % 360 / 90;

        var result = new JigsawRuntimeData
        {
            UpEdgeType = data.upEdgeType,
            DownEdgeType = data.downEdgeType,
            LeftEdgeType = data.leftEdgeType,
            RightEdgeType = data.rightEdgeType,
            Source = data
        };

        for (int i = 0; i < steps; i++)
        {
            result = Rotate90(result);
        }
        
        result.RotateAngle = angle;

        return result;
    }
    
    private static JigsawRuntimeData Rotate90(JigsawRuntimeData d)
    {
        return new JigsawRuntimeData
        {
            UpEdgeType = d.LeftEdgeType,
            RightEdgeType = d.UpEdgeType,
            DownEdgeType = d.RightEdgeType,
            LeftEdgeType = d.DownEdgeType,
            Source = d.Source
        };
    }
}