using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public enum SceneType
{
    Hub,
    ShrinkingMask,
    MultiCamera,
}

public class LoadSceneManager : MonoBehaviour
{
    public static LoadSceneManager Instance;

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

    public void Load(SceneType scene)
    {
        var sceneName = scene.ToString();
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        var op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.3f);

        op.allowSceneActivation = true;
    }
}