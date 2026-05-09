using GameEvent;
using GameEvent.Args;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace
{
    public class MenuUI : MonoBehaviour
    {
        [SerializeField] private Button backButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private RectTransform jigsaw;
        [SerializeField] private RectTransform knob;
        [SerializeField] private GameObject worldCube;
        [SerializeField] private Canvas canvas;

        private void Awake()
        {
            backButton.onClick.AddListener(Resume);
            quitButton.onClick.AddListener(Quit);
        }

        private void Start()
        {
            EventComponent.Instance.Subscribe(CapturedJigsawEventArgs.EventId, OnCapturedJigsaw);
        }

        private void OnDestroy()
        {
            EventComponent.Instance.Unsubscribe(CapturedJigsawEventArgs.EventId, OnCapturedJigsaw);
        }

        private void Resume()
        {
            GameManager.Instance.ToggleOpenMenu();
        }

        private void Quit()
        {
            Application.Quit();
        }

        private void OnCapturedJigsaw(object sender, GameEventArgs e)
        {
            var isOnUiJigsaw = RectTransformUtility.RectangleContainsScreenPoint(
                jigsaw,
                Input.mousePosition,
                canvas.worldCamera
            );
            if (isOnUiJigsaw)
            {
                jigsaw.gameObject.SetActive(false);
                return;
            }
            
            var isOnUiKnob = RectTransformUtility.RectangleContainsScreenPoint(
                knob,
                Input.mousePosition,
                canvas.worldCamera
            );
            if (isOnUiKnob)
            {
                knob.gameObject.SetActive(false);
                worldCube.gameObject.SetActive(false);
            }
        }

        public void ResetState()
        {
            jigsaw.gameObject.SetActive(true);
            knob.gameObject.SetActive(true);
        }
    }
}