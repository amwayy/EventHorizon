using System;
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
        [SerializeField] private Button backToHubButton;
        [SerializeField] private Canvas canvas;
        
        private void Awake()
        {
            backButton.onClick.AddListener(Resume);
            quitButton.onClick.AddListener(Quit);
            backToHubButton.onClick.AddListener(GoBackToHub);
            
            backToHubButton.gameObject.SetActive(false);
        }

        private void Start()
        {
            EventComponent.Instance.Subscribe(EnterLevelEventArgs.EventId, OnEnterLevel);
        }

        private void OnDestroy()
        {
            EventComponent.Instance.Unsubscribe(EnterLevelEventArgs.EventId, OnEnterLevel);
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

        private void OnEnterLevel(object sender, EventArgs e)
        {
            if (e is not EnterLevelEventArgs args) return;

            if (args.LevelIndex == Configs.HubLevelId)
            {
                backToHubButton.gameObject.SetActive(true);
            }
        }
    }
}