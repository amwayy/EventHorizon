using System.Linq;
using GameEvent;
using GameEvent.Args;
using UnityEngine;

public class JigsawCollective : MonoBehaviour
{
    [SerializeField] private Renderer[] worldObjects;

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

        var isHit = false;
        foreach (var worldObject in worldObjects)
        {
            var objectRect = Utility.GetScreenRect(worldObject, _mainCamera);
            if (objectRect.Contains(args.BBoxCenter))
            {
                isHit = true;
                break;
            }
        }

        if (isHit)
        {
            foreach (var worldObject in worldObjects)
            {
                worldObject.gameObject.SetActive(false);
            }   
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