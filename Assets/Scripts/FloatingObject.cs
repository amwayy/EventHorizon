using GameEvent;
using GameEvent.Args;
using UnityEngine;

namespace DefaultNamespace
{
    public class FloatingObject : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        
        private void Start()
        {
            EventComponent.Instance.Subscribe(ScreenshotModeToggleEventArgs.EventId, OnScreenshotModeToggled);
            EventComponent.Instance.Subscribe(LevelResetEventArgs.EventId, OnLevelReset);
        }

        private void OnDestroy()
        {
            EventComponent.Instance.Unsubscribe(ScreenshotModeToggleEventArgs.EventId, OnScreenshotModeToggled);
            EventComponent.Instance.Subscribe(LevelResetEventArgs.EventId, OnLevelReset);
        }

        private void OnScreenshotModeToggled(object sender, GameEventArgs e)
        {
            if (!animator) return;
            if (e is not ScreenshotModeToggleEventArgs args) return;

            animator.enabled = !args.IsOn;
        }

        private void OnLevelReset(object sender, GameEventArgs e)
        {
            if (!gameObject.activeSelf) return;
            // resync the animation
            gameObject.SetActive(false);
            gameObject.SetActive(true);
        }
    }
}