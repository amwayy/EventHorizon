using System;
using DefaultNamespace;
using GameEvent;
using GameEvent.Args;
using UnityEngine;

public class MenuResetter : MonoBehaviour
{
    [SerializeField] private Level level;
    [SerializeField] private MenuUI menuUI;

    private void Start()
    {
        EventComponent.Instance.Subscribe(LevelResetEventArgs.EventId, OnLevelReset);
    }

    private void OnDestroy()
    {
        EventComponent.Instance.Unsubscribe(LevelResetEventArgs.EventId, OnLevelReset);
    }

    private void OnLevelReset(object sender, EventArgs e)
    {
        if (!level.IsActive) return;
        
        menuUI.ResetState();
    }
}