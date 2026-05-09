using GameEvent;
using GameEvent.Args;
using UnityEngine;

public class QuickInputs : MonoBehaviour
{
    private static QuickInputs _instance;
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            EventComponent.Instance.Fire(this, LevelResetEventArgs.Create());
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            GameManager.Instance.ToggleOpenMenu();
        }
    }
}