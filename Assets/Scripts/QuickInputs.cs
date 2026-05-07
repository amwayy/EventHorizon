using GameEvent;
using GameEvent.Args;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        if (Input.GetKey(KeyCode.R))
        {
            EventComponent.Instance.Fire(this, LevelResetEventArgs.Create());
        }
        else if (Input.GetKey(KeyCode.Escape))
        {
            Application.Quit();
        }
    }
}