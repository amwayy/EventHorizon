using DefaultNamespace;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

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
        
        InitData();
    }

    public bool HasKey(string dataKey)
    {
        return ES3.KeyExists(dataKey);
    }

    public T Load<T>(string dataKey, T defaultValue = default)
    {
        if (ES3.KeyExists(dataKey))
        {
            return ES3.Load<T>(dataKey);
        }
            
        return defaultValue;
    }

    public void Delete(string dataKey)
    {
        ES3.DeleteKey(dataKey);
    }

    public void Save<T>(string dataKey, T data)
    {
        ES3.Save(dataKey, data);
    }

    public void Increase(string dataKey)
    {
        var value = Load(dataKey, 0);
        Save(dataKey, value + 1);
    }

    public void Decrease(string dataKey)
    {
        var value = Load(dataKey, 0);
        Save(dataKey, value - 1);
    }

    public void InitData()
    {
        if (HasKey(DataKey.PutJigsaws)) return;
        
        Save(DataKey.PutJigsaws, Configs.InitialPutJigsaws);
    }
}
