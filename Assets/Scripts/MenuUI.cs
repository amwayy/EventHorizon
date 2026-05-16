using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace
{
    public class MenuUI : MonoBehaviour
    {
        [SerializeField] private Button backButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button backToHubButton;
        [SerializeField] private Canvas canvas;
        
        private void Awake()
        {
            backButton.onClick.AddListener(Resume);
            quitButton.onClick.AddListener(Quit);
            backToHubButton.onClick.AddListener(GoBackToHub);
        }

        private void GoBackToHub()
        {
            GameManager.Instance.ToggleOpenMenu();
            LevelManager.Instance.GoBackToHub();
        }

        private void Resume()
        {
            GameManager.Instance.ToggleOpenMenu();
        }

        private void Quit()
        {
            Application.Quit();
        }
    }
}