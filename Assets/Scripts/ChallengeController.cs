using GameEvent;
using GameEvent.Args;
using UnityEngine;

public class ChallengeController : MonoBehaviour
{
    [SerializeField] private int prepositiveLevelIndex;
    [SerializeField] private GameObject wallBeforeChallenge;

    private void Start()
    {
        EventComponent.Instance.Subscribe(EnterLevelEventArgs.EventId, OnEnterLevel);
    }

    private void OnDestroy()
    {
        EventComponent.Instance.Subscribe(EnterLevelEventArgs.EventId, OnEnterLevel);
    }

    private void OnEnterLevel(object sender, GameEventArgs e)
    {
        if (e is not EnterLevelEventArgs args) return;
        if (args.LevelIndex == prepositiveLevelIndex)
        {
            wallBeforeChallenge.SetActive(false);
        }
    }
}