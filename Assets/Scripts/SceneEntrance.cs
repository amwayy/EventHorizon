using UnityEngine;

public class SceneEntrance: MonoBehaviour
{
    [SerializeField] private SceneType sceneType;
    [SerializeField] private GameObject completedIndicator;

    private bool _isCompleted;
    
    private void Start()
    {
        _isCompleted = GameManager.Instance.IsCompleted(sceneType);
        completedIndicator.SetActive(_isCompleted);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isCompleted) return;
        
        Debug.Log($"loading {sceneType}");
        LoadSceneManager.Instance.Load(sceneType);
        GameManager.Instance.OnEnterLevel(sceneType);
    }
}