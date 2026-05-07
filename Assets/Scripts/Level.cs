using System;
using GameEvent;
using GameEvent.Args;
using UnityEngine;

public class Level: MonoBehaviour
{
    [SerializeField] private int levelIndex;
    [SerializeField] private int prepositiveLevelIndex;
    [SerializeField] private JigsawBoard jigsawBoard;
    [SerializeField] private Transform jigsawContainer;
    
    public int PrepositiveLevelIndex => prepositiveLevelIndex;
    
    private bool _isActive;

    private void Awake()
    {
        if (levelIndex != 0) gameObject.SetActive(false);
    }

    private void Start()
    {
        EventComponent.Instance.Subscribe(LevelResetEventArgs.EventId, OnLevelReset);
    }

    private void OnDestroy()
    {
        EventComponent.Instance.Unsubscribe(LevelResetEventArgs.EventId, OnLevelReset);
    }

    private void OnLevelReset(object sender, GameEventArgs e)
    {
        if (!_isActive) return;
        
        ResetLevel();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.TryGetComponent(out CharacterController _)) return;
        
        _isActive = true;
        EventComponent.Instance.Fire(this, EnterLevelEventArgs.Create(levelIndex));
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.gameObject.TryGetComponent(out CharacterController _)) return;
        
        _isActive = false;
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
        jigsawBoard.ClearJigsaws();
    }
}