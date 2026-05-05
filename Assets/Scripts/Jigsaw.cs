using System.Linq;
using DefaultNamespace;
using GameEvent;
using GameEvent.Args;
using UnityEngine;

public class Jigsaw : MonoBehaviour
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
        var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out var hit)) return;
        
        if (worldObjects.Contains(hit.collider.gameObject))
        {
            foreach (var worldObject in worldObjects)
            {
                worldObject.SetActive(false);
            }
        }
    }
}