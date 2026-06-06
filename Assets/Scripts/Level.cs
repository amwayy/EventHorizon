using System;
using System.Collections.Generic;
using System.Threading;
using DefaultNamespace;
using GameEvent;
using GameEvent.Args;
using UnityEngine;

public class Level: MonoBehaviour
{
    [SerializeField] private int levelIndex;
    [SerializeField] private List<int> adjacentLevelIds;
    [SerializeField] private JigsawBoard[] jigsawBoards;
    [SerializeField] private Transform jigsawContainer;
    [SerializeField] private Transform entryPoint;
    [SerializeField] private JigsawBarrier entranceBarrier;
    
    public List<int> AdjacentLevelIds => adjacentLevelIds;
    public int LevelId => levelIndex;
    public Transform EntryPoint => entryPoint;

    private bool _isAdjacentLevelsInitialized;

    private void Start()
    {
        for (var i = 0; i < jigsawContainer.childCount; i++)
        {
            var jigsaw = jigsawContainer.GetChild(i);
            if (jigsaw.TryGetComponent(out JigsawCollective jigsawCollective))
            {
                jigsawCollective.Init(levelIndex, i);
            }
        }

        var isLevelSolved = true;
        foreach (var board in jigsawBoards)
        {
            board.Init(levelIndex);
            isLevelSolved &= board.IsFilled();
        }
        if (entranceBarrier)
        {
            entranceBarrier.Init(levelIndex, isLevelSolved);   
        }
        
        EventComponent.Instance.Subscribe(LevelResetEventArgs.EventId, OnLevelReset);
    }

    private void OnDestroy()
    {
        EventComponent.Instance.Unsubscribe(LevelResetEventArgs.EventId, OnLevelReset);
    }

    private void OnLevelReset(object sender, GameEventArgs e)
    {
        if (LevelManager.Instance.CurrentLevelIndex != levelIndex) return;
        
        ResetLevel();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.TryGetComponent(out CharacterController _)) return;
        
        EventComponent.Instance.Fire(this, EnterLevelEventArgs.Create(levelIndex));
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.gameObject.TryGetComponent(out CharacterController _)) return;
        
        EventComponent.Instance.Fire(this, ExitLevelEventArgs.Create(levelIndex));
    }

    private void ResetLevel()
    {
        foreach (Transform jigsaw in jigsawContainer)
        {
            if (jigsaw.TryGetComponent(out JigsawCollective jigsawCollective))
            {
                jigsawCollective.ResetState();
            }
        }
        
        foreach (var jigsawBoard in jigsawBoards)
        {
            jigsawBoard.ClearJigsaws();   
        }
        
        LevelManager.Instance.TeleportPlayerToLevel(levelIndex);
    }

    public void ResetCollective(int index, bool sendNotification)
    {
        var collectiveTransform = jigsawContainer.GetChild(index);
        if (collectiveTransform && collectiveTransform.TryGetComponent(out JigsawCollective collective))
        {
            collective.ResetState(sendNotification);
        }
    }

    public void ResetSlot(int slotIndex, bool sendNotification)
    {
        var slotTransform = jigsawBoards[0].transform.GetChild(slotIndex);
        if (slotTransform && slotTransform.TryGetComponent(out JigsawSlot jigsawSlot))
        {
            jigsawSlot.ResetState(sendNotification);
        }
    }
    
    public void InitAdjacentLevelIds()
    {
        if (_isAdjacentLevelsInitialized) return;
        
        adjacentLevelIds.Add(levelIndex - 1);
        adjacentLevelIds.Add(levelIndex + 1);
        
        _isAdjacentLevelsInitialized = true;
    }

}