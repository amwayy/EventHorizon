using UnityEngine;
using UnityEngine.SceneManagement;

public class QuickInputs : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
    
    private void Update()
    {
        if (Input.GetKey(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        else if (Input.GetKey(KeyCode.Escape))
        {
            Application.Quit();
        }
    }
}