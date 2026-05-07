using System.Linq;
using GameEvent;
using GameEvent.Args;
using UnityEngine;

public class JigsawCollective : MonoBehaviour
{
    [SerializeField] private GameObject[] worldObjects;

    private Camera _mainCamera;

    private void Start()
    {
        _mainCamera = Camera.main;
        
        EventComponent.Instance.Subscribe(CapturedJigsawEventArgs.EventId, OnGotCollective);
    }

    private void OnDestroy()
    {
        EventComponent.Instance.Unsubscribe(CapturedJigsawEventArgs.EventId, OnGotCollective);
    }

    private void OnGotCollective(object sender, GameEventArgs e)
    {
        if (e is not CapturedJigsawEventArgs args) return;
        
        if (worldObjects.Contains(args.HitGameObject))
        {
            foreach (var worldObject in worldObjects)
            {
                worldObject.SetActive(false);
            }
        }
    public void ResetState()
    {
        foreach (var worldObject in worldObjects)
        {
            worldObject.gameObject.SetActive(true);
        }
    }
}