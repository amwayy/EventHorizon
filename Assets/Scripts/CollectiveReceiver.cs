using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CollectiveReceiver : MonoBehaviour
{
    [SerializeField] private Image maskImage;

    public void Unlock()
    {
        maskImage.DOFade(0f, 0.5f);
    }
}