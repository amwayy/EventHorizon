using System.Collections.Generic;
using GameEvent;
using GameEvent.Args;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private Level[] levels;
    [SerializeField] private Vector3 hubPosition;
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
        EventComponent.Instance.Subscribe(EnterLevelEventArgs.EventId, OnEnterLevel);
    }

    private void OnDestroy()
    {
        EventComponent.Instance.Unsubscribe(EnterLevelEventArgs.EventId, OnEnterLevel);
    }

    private void OnEnterLevel(object sender, GameEventArgs e)
    {
        if (e is not EnterLevelEventArgs args) return;
        
        OnEnterLevel(args.LevelIndex);
    }

    private void OnEnterLevel(int levelId)
    {
        CurrentLevelIndex = levelId;

        var enteredLevel = System.Array.Find(levels, level => level.LevelId == levelId);
        var adjacentLevelIds = enteredLevel.AdjacentLevelIds;
        foreach (var level in levels)
        {
            level.gameObject.SetActive(false);
            if (level == enteredLevel || adjacentLevelIds.Contains(level.LevelId))
            {
                level.gameObject.SetActive(true);
                continue;
            }
            foreach (var neighborLevelId in adjacentLevelIds)
            {
                var neighborLevel = System.Array.Find(levels, lv => lv.LevelId == neighborLevelId);
                if (!neighborLevel) continue;
                if ((neighborLevel.AdjacentLevelIds != null && neighborLevel.AdjacentLevelIds.Contains(level.LevelId)) || 
                    Mathf.Abs(neighborLevel.LevelId - level.LevelId) <= 1)
                {
                    level.gameObject.SetActive(true);
                    break;
                }
            }
        }
    }

    public void GoBackToHub()
    {
        playerController.enabled = false;
        playerController.transform.position = hubPosition;
        playerController.enabled = true;
        
        OnEnterLevel(Configs.HubLevelId);
    }
}