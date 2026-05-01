using GameEvent;
using GameEvent.Args;
using UnityEngine;

public class ScreenshotModeUI : MonoBehaviour
{
    [SerializeField] private GameObject border;

    private void Start()
    {
        border.SetActive(false);
        
        EventComponent.Instance.Subscribe(ScreenshotModeToggleEventArgs.EventId, OnScreenshotModeToggled);
    }

    private void OnDestroy()
    {
        EventComponent.Instance.Unsubscribe(ScreenshotModeToggleEventArgs.EventId, OnScreenshotModeToggled);
    }

    private void OnScreenshotModeToggled(object sender, GameEventArgs e)
    {
        if (e is not ScreenshotModeToggleEventArgs args) return;

        border.SetActive(args.IsOn);
    }
}