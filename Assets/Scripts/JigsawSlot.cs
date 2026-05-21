using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public struct SlotJigsawData
{
    public (int, int)[] CollectiveIndexes;
    public int RotationAngle;
    public string JigsawName;
    public Color JigsawColor;
    public WallCollectiveData[] WallCollectiveDataArray;
}

[RequireComponent(typeof(Image))]
public class JigsawSlot : MonoBehaviour
{
    [SerializeField] private JigsawDatabase jigsawDatabase;
    [SerializeField] private Transform jigsawContainer;
    [SerializeField] protected GameObject texture;
    
    private static readonly int DissolveStrength = Shader.PropertyToID("_DissolveStrength");
    private static readonly int Color1 = Shader.PropertyToID("_Color");

    public RectTransform RectTransform => _slotImage.transform as RectTransform;
    public int Index => transform.GetSiblingIndex();
    
    private Image _slotImage;
    private JigsawBoard _board;
    private bool _isUnlocked;
    private GameObject _jigsaw;
    private MaterialPropertyBlock _mpb;
    private Renderer _rd;
    private Tween _dissolveTween;
    public int LevelId { get; private set; }
    public JigsawRuntimeData JigsawData;

    private void Awake()
    {
        _slotImage = GetComponent<Image>();
        _board = GetComponentInParent<JigsawBoard>();
        _mpb = new MaterialPropertyBlock();
    }

    public void Init(int levelId)
    {
        LevelId = levelId;
        
        var putJigsaws = 
            DataManager.Instance.Load(DataKey.PutJigsaws, new Dictionary<(int, int), SlotJigsawData>());
        if (putJigsaws.TryGetValue((LevelId, Index), out var slotJigsawData))
        {
            var jigsawSo = JigsawManager.Instance.GetJigsawSo(slotJigsawData.JigsawName);
            var jigsawData = Utility.Rotate(jigsawSo, slotJigsawData.RotationAngle);
            PutJigsawInternal(jigsawData, slotJigsawData.JigsawColor);
        }
    }

    public void Unlock()
    {
        _slotImage.enabled = false;
        texture.SetActive(false);
        _isUnlocked = true;
        Dissolve();
    }

    public bool CanPut(JigsawRuntimeData jigsawData)
    {
        if (_isUnlocked) return false;
        return _board.CanPut(jigsawData, this);
    }

    public void PutJigsaw(JigsawRuntimeData jigsawData, Color color)
    {
        PutJigsawInternal(jigsawData, color);
        
        var targetCollectives = CollectedJigsawsUI.Instance.GetSlotCollective(this);
        if (targetCollectives is { Count: > 0 })
        {
            var putJigsaws = 
                DataManager.Instance.Load(DataKey.PutJigsaws, new Dictionary<(int, int), SlotJigsawData>());
            putJigsaws[(LevelId, Index)] = new SlotJigsawData
            {
                CollectiveIndexes = targetCollectives.Where(collective => collective is not WallJigsawCollective)
                    .Select(collective => (collective.LevelId, collective.CollectiveIndex)).ToArray(),
                RotationAngle = jigsawData.RotateAngle,
                JigsawName = jigsawData.Source.jigsawName,
                JigsawColor = color,
                WallCollectiveDataArray = targetCollectives.Where(collective => collective is WallJigsawCollective)
                    .Select(collective => (collective as WallJigsawCollective).GetCollectiveData()).ToArray(),
            };   
            DataManager.Instance.Save(DataKey.PutJigsaws, putJigsaws);
        }
    }

    private void PutJigsawInternal(JigsawRuntimeData jigsawData, Color color)
    {
        if (!JigsawData.Source || JigsawData.Source.jigsawName != jigsawData.Source.jigsawName)
        {
            if (_jigsaw)
            {
                _jigsaw.gameObject.SetActive(false);   
            }
            _jigsaw = Instantiate(jigsawData.Source.prefab, jigsawContainer);
            _jigsaw.transform.localPosition = Vector3.zero;
            JigsawData = jigsawData;
            _rd = _jigsaw.GetComponentInChildren<Renderer>();
            
            _rd.GetPropertyBlock(_mpb);
            _mpb.SetColor(Color1, color);
            _rd.SetPropertyBlock(_mpb);
        }
        transform.localRotation = Quaternion.Euler(0, 0, jigsawData.RotateAngle);
        _board.OnPutOnSlot();
    }

    private void Dissolve()
    {
        var dissolveStrength = 0f;
        _dissolveTween = DOTween.To(() => dissolveStrength, x =>
        {
            dissolveStrength = x;

            _rd.GetPropertyBlock(_mpb);
            _mpb.SetFloat(DissolveStrength, dissolveStrength);
            _rd.SetPropertyBlock(_mpb);

        }, 1f, 1f).OnComplete(
            () => gameObject.SetActive(false));
    }

    public void ResetState(bool sendNotification = true)
    {
        CollectedJigsawsUI.Instance.OnResetSlot(this, sendNotification);
        ResetInternal();
    }

    public void ClearJigsaw()
    {
        ResetInternal();
        _board.ClearSlot(this);
    }

    private void ResetInternal()
    {
        _dissolveTween?.Kill();
        
        Show();
        
        if (_jigsaw)
        {
            _jigsaw.SetActive(false);
            _jigsaw = null;
        }
        
        _isUnlocked = false;
        JigsawData.Source = null;
        
        var putJigsaws = 
            DataManager.Instance.Load(DataKey.PutJigsaws, new Dictionary<(int, int), SlotJigsawData>());
        if (putJigsaws.ContainsKey((LevelId, Index)))
        {
            putJigsaws.Remove((LevelId, Index));
            DataManager.Instance.Save(DataKey.PutJigsaws, putJigsaws);
        }
        
        _board.ShowSlots();
    }

    public virtual void Show()
    {
        gameObject.SetActive(true);
        
        if (_jigsaw)
        {
            _rd.GetPropertyBlock(_mpb);
            _mpb.SetFloat(DissolveStrength, 0f);
            _rd.SetPropertyBlock(_mpb);
        }

        if (_slotImage)
        {
            _slotImage.enabled = true;   
        }
        texture.SetActive(true);
    }
}