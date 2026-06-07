using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace
{
    public class MenuUI : MonoBehaviour
    {
        [SerializeField] private Button backButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button clearSaveDataButton;
        [SerializeField] private Canvas canvas;
        [SerializeField] private TMP_Text versionText;
        [SerializeField] private Button fullscreenButton;
        [SerializeField] private Button windowedButton;
        
        private void Awake()
        {
            backButton.onClick.AddListener(Resume);
            quitButton.onClick.AddListener(Quit);
            clearSaveDataButton.onClick.AddListener(ClearSaveData);
            fullscreenButton.onClick.AddListener(EnterFullscreen);
            windowedButton.onClick.AddListener(EnterWindowed);
        }

        private void Start()
        {
            versionText.text = Application.version;
            
            UpdateFullscreenButtons();
        }

        private void ClearSaveData()
        {
            GameManager.Instance.ClearSaveData();
        }

        private void Resume()
        {
            GameManager.Instance.ToggleOpenMenu();
        }

        private void Quit()
        {
            Application.Quit();
        }

        private void UpdateFullscreenButtons()
        {
            var isFullscreen = Screen.fullScreenMode is FullScreenMode.ExclusiveFullScreen or FullScreenMode.FullScreenWindow;
            fullscreenButton.gameObject.SetActive(!isFullscreen);
            windowedButton.gameObject.SetActive(isFullscreen);
        }

        private void EnterFullscreen()
        {
            Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
            fullscreenButton.gameObject.SetActive(false);
            windowedButton.gameObject.SetActive(true);
        }
        
        private void EnterWindowed()
        {
            Screen.fullScreenMode = FullScreenMode.Windowed;
            fullscreenButton.gameObject.SetActive(true);
            windowedButton.gameObject.SetActive(false);
        }
    }
}