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
        
        private void Awake()
        {
            backButton.onClick.AddListener(Resume);
            quitButton.onClick.AddListener(Quit);
            clearSaveDataButton.onClick.AddListener(ClearSaveData);
        }

        private void Start()
        {
            versionText.text = Application.version;
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
    }
}