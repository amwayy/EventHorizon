using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class JigsawSlot : MonoBehaviour
{
    [SerializeField] private JigsawDatabase jigsawDatabase;
    [SerializeField] private Transform jigsawContainer;
    
    private static readonly int DissolveStrength = Shader.PropertyToID("_DissolveStrength");
    
    public RectTransform RectTransform => _slotImage.rectTransform;
    
    private Image _slotImage;
    private JigsawBoard _board;
    private bool _isUnlocked;
    private GameObject _jigsaw;
    private JigsawSO _jigsawSO;
    private Collider _collider;
    private MaterialPropertyBlock _mpb;
    private Renderer _rd;

    private void Awake()
    {
        _slotImage = GetComponent<Image>();
        _board = GetComponentInParent<JigsawBoard>();
        _collider = GetComponent<Collider>();
        _mpb = new MaterialPropertyBlock();
    }

    public void Unlock()
    {
        _slotImage.enabled = false;
        _isUnlocked = true;
        Dissolve();
        _collider.enabled = false;
    }

    public void Highlight()
    {
        _slotImage.color = Color.gray;
    }

    public void Unhighlight()
    {
        _slotImage.color = Color.black;
    }

    public bool CanPut(JigsawRuntimeData jigsawData)
    {
        if (_isUnlocked) return false;
        return _board.CanPut(jigsawData, this);
    }

    public void PutJigsaw(JigsawRuntimeData jigsawData)
    {
        if (!_jigsawSO || _jigsawSO.jigsawName != jigsawData.Source.jigsawName)
        {
            if (_jigsaw)
            {
                _jigsaw.gameObject.SetActive(false);   
            }
            _jigsaw = Instantiate(jigsawData.Source.prefab, jigsawContainer);
            _jigsaw.transform.localPosition = Vector3.zero;
            _jigsaw.transform.localRotation = Quaternion.identity;
            _jigsawSO = jigsawData.Source;
            _rd = _jigsaw.GetComponentInChildren<Renderer>();
        }
        transform.localRotation = Quaternion.Euler(0, 0, jigsawData.RotateAngle);
        _board.Put(jigsawData, this);
    }

    private void Dissolve()
    {
        var dissolveStrength = 0f;
        DOTween.To(() => dissolveStrength, x =>
        {
            dissolveStrength = x;

            _rd.GetPropertyBlock(_mpb);
            _mpb.SetFloat(DissolveStrength, dissolveStrength);
            _rd.SetPropertyBlock(_mpb);

        }, 1f, 1f).OnComplete(
            () => _jigsaw.gameObject.SetActive(false));
    }
}