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
    
    public bool IsActive { get; private set; }

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
        if (!IsActive) return;
        
        ResetLevel();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.TryGetComponent(out CharacterController _)) return;
        
        IsActive = true;
        EventComponent.Instance.Fire(this, EnterLevelEventArgs.Create(levelIndex));
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.gameObject.TryGetComponent(out CharacterController _)) return;
        
        IsActive = false;
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