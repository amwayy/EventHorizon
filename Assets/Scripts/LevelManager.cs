using DefaultNamespace;
using GameEvent;
using GameEvent.Args;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private Level[] levels;
    [SerializeField] private CharacterController playerController;
    
    public static LevelManager Instance;

    public int CurrentLevelIndex { get; private set; } = -1;

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
        InitPlayerPosition();
        
        EventComponent.Instance.Subscribe(EnterLevelEventArgs.EventId, OnEnterLevel);
    }

    private void OnDestroy()
    {
        EventComponent.Instance.Unsubscribe(EnterLevelEventArgs.EventId, OnEnterLevel);
    }

    private void InitPlayerPosition()
    {
        CurrentLevelIndex = DataManager.Instance.Load(DataKey.CurrentLevelId, Configs.InitialLevelId);
        TeleportPlayerToLevel(CurrentLevelIndex);
    }

    private void TeleportPlayerToLevel(int levelId)
    {
        var level = GetLevel(levelId);
        OnEnterLevelInternal(levelId);
        
        playerController.enabled = false;
        playerController.transform.position = level.EntryPoint.position;
        playerController.transform.rotation = level.EntryPoint.rotation;
        playerController.enabled = true;
    }

    private void OnEnterLevel(object sender, GameEventArgs e)
    {
        if (e is not EnterLevelEventArgs args) return;
        
        OnEnterLevel(args.LevelIndex);
    }

    private void OnEnterLevel(int levelId)
    {
        OnEnterLevelInternal(levelId);
        
        DataManager.Instance.Save(DataKey.CurrentLevelId, CurrentLevelIndex);
    }

    private void OnEnterLevelInternal(int levelId)
    {
        CurrentLevelIndex = levelId;

        var enteredLevel = GetLevel(levelId);
        enteredLevel.InitAdjacentLevelIds();
        var adjacentLevelIds = enteredLevel.AdjacentLevelIds;
        foreach (var level in levels)
        {
            var show = false;
            if (level == enteredLevel || adjacentLevelIds.Contains(level.LevelId))
            {
                level.gameObject.SetActive(true);
                continue;
            }
            foreach (var neighborLevelId in adjacentLevelIds)
            {
                var neighborLevel = GetLevel(neighborLevelId);
                if (!neighborLevel) continue;
                neighborLevel.InitAdjacentLevelIds();
                if ((neighborLevel.AdjacentLevelIds != null && neighborLevel.AdjacentLevelIds.Contains(level.LevelId)) || 
                    Mathf.Abs(neighborLevel.LevelId - level.LevelId) <= 1)
                {
                    show = true;
                    break;
                }
            }
            level.gameObject.SetActive(show);
        }
    }

    private Level GetLevel(int levelId)
    {
        return System.Array.Find(levels, lv => lv.LevelId == levelId);
    }

    public void GoBackToHub()
    {
        TeleportPlayerToLevel(Configs.HubLevelId);
        
        OnEnterLevel(Configs.HubLevelId);
    }

    public void ResetLevelCollective(int levelId, int collectiveIndex, bool sendNotification = true)
    {
        var level = GetLevel(levelId);
        level.ResetCollective(collectiveIndex, sendNotification);
    }

    public void ResetLevelSlot(int levelId, int slotIndex)
    {
        var level = GetLevel(levelId);
        level.ResetSlot(slotIndex, false);
    }
}