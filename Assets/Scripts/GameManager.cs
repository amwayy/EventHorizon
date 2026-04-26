using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager: MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
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
}