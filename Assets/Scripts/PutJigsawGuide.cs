using GameEvent;
using GameEvent.Args;
using UnityEngine;

public class PutJigsawGuide : MonoBehaviour
{
    [SerializeField] private GameObject guideText;

    private void Awake()
    {
        guideText.SetActive(false);
    }

    private void Start()
    {
        EventComponent.Instance.Subscribe(CapturedJigsawEventArgs.EventId, OnCapturedJigsaw);
    }

    private void OnDestroy()
    {
        EventComponent.Instance.Unsubscribe(CapturedJigsawEventArgs.EventId, OnCapturedJigsaw);
    }

    private void OnCapturedJigsaw(object sender, GameEventArgs e)
    {
        guideText.SetActive(true);
    }
}