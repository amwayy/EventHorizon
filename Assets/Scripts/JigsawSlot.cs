using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class JigsawSlot : MonoBehaviour
{
    [SerializeField] private Image jigsawImage;
    
    private Image _slotImage;
    private JigsawBoard _board;
    
    private void Awake()
    {
        _slotImage = GetComponent<Image>();
        _board = GetComponentInParent<JigsawBoard>();
    }

    public void Unlock()
    {
        jigsawImage.DOFade(0f, 0.5f);
        GetComponent<Collider>().enabled = false;
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
        return _board.CanPut(jigsawData, this);
    }

    public void PutJigsaw(JigsawRuntimeData jigsawData)
    {
        _slotImage.enabled = false;
        jigsawImage.gameObject.SetActive(true);
        jigsawImage.sprite = Utility.GetOrCreateSprite(jigsawData.Source.texture);
        transform.localRotation = Quaternion.Euler(0, 0, jigsawData.RotateAngle);
        _board.Put(jigsawData, this);
    }
}