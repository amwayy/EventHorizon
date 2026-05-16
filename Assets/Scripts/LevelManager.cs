using GameEvent;
using GameEvent.Args;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private Level[] levels;
    [SerializeField] private Vector3 hubPosition;
    [SerializeField] private CharacterController playerController;
    
    public static LevelManager Instance;

    public int CurrentLevelIndex { get; private set; } = 0;

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
        EventComponent.Instance.Subscribe(EnterLevelEventArgs.EventId, OnEnterLevel);
    }

    private void OnDestroy()
    {
        EventComponent.Instance.Unsubscribe(EnterLevelEventArgs.EventId, OnEnterLevel);
    }

    private void OnEnterLevel(object sender, GameEventArgs e)
    {
        if (e is not EnterLevelEventArgs args) return;
        
        CurrentLevelIndex = args.LevelIndex;

        foreach (var level in levels)
        {
            if (level.PrepositiveLevelIndex == args.LevelIndex)
            {
                level.gameObject.SetActive(true);
                break;
            } 
        }
    }

    public void GoBackToHub()
    {
        playerController.enabled = false;
        playerController.transform.position = hubPosition;
        playerController.enabled = true;
    }
}