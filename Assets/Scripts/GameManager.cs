using System.Collections.Generic;
using System.Linq;
using GameEvent;
using GameEvent.Args;
using UnityEngine;

public class GameManager: MonoBehaviour
{
    [SerializeField] private GameObject menu;
    
    public static GameManager Instance { get; private set; }
    
    public bool IsInMenu { get; private set; } 
    
    private readonly Dictionary<SceneType, bool> _levelCompleteState = new ();

    private SceneType _currentLevel;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = 60;
    }

    public void OnEnterLevel(SceneType level)
    {
        _currentLevel = level;
    }

    public void MarkLevelComplete()
    {
        _levelCompleteState[_currentLevel] = true;

        if (_currentLevel != SceneType.Hub)
        {
            LoadSceneManager.Instance.Load(SceneType.Hub);
        }
    }

    public bool IsCompleted(SceneType sceneType)
    {
        return _levelCompleteState.GetValueOrDefault(sceneType, false);
    }

    public int GetCompletedLevelCount()
    {
        return _levelCompleteState.Values.Count(isCompleted => isCompleted);
    }

    public void ToggleOpenMenu()
    {
        IsInMenu = !IsInMenu;
        menu.SetActive(IsInMenu);
        SetGameSpeed(IsInMenu ? 0f : 1f);
        
        EventComponent.Instance.Fire(this, ToggleOpenMenuEventArgs.Create(IsInMenu));
    }

    public void SetGameSpeed(float speed)
    {
        if (IsInMenu)
        {
            Time.timeScale = 0f;
            return;
        }
        if (ScreenshotController.Instance.IsInScreenshot)
        {
            Time.timeScale = Configs.ScreenshotModeGameSpeed;
            return;
        }
        Time.timeScale = speed;
    }

    public Vector3 GetViewportPosition(Vector3 screenPosition)
    {
        var scaleFactorX = (float)Configs.ViewportWidth / Screen.width;
        var scaleFactorY = (float)Configs.ViewportHeight / Screen.height;
        return new Vector3(screenPosition.x * scaleFactorX, screenPosition.y * scaleFactorY, screenPosition.z);
    }

    public Vector3 GetViewportMousePosition()
    {
        var mousePos = Input.mousePosition;
        return GetViewportPosition(mousePos);
    }

    public void ClearSaveData()
    {
        ES3.DeleteFile();
        Application.Quit();
    }
}