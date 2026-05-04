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
    private Image _image;
    private JigsawRuntimeData _jigsawData;

    private void Awake()
    {
        _mainCamera = Camera.main;
        
        _outline = GetComponent<Outline>();
        _rectTransform = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
        _image = GetComponent<Image>();
    }

    private void OnEnable()
    {
        _outline.enabled = false;
    }

    public void Init(Texture2D texture, int rotateAngle)
    {
        _image.sprite = Utility.GetOrCreateSprite(texture);
        transform.localRotation = Quaternion.Euler(0, 0, rotateAngle);
        var jigsawData = jigsawDatabase.allJigsaws.Find(data => data.texture == texture);
        _jigsawData = Rotate(jigsawData, rotateAngle);
        _jigsawData.RotateAngle = rotateAngle;
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

        if (_hoveringSlot && _hoveringSlot.CanPut(_jigsawData))
        {
            _hoveringSlot.PutJigsaw(_jigsawData);
            ScreenshotController.Instance.ToggleScreenshotState();
            CursorStack.Pop(_openHandCursorId);
            gameObject.SetActive(false);
        }
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