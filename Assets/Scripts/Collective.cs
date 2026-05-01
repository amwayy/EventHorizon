using System.Linq;
using DG.Tweening;
using GameEvent;
using GameEvent.Args;
using UnityEngine;

public class Collective : MonoBehaviour
{
    [SerializeField] private ParticleSystem ps;
    [SerializeField] private CollectiveReceiver receiver;
    [SerializeField] private GameObject[] worldObjects;

    private Camera _mainCamera;
    private const float ParticleSystemSpeed = 4f;

    private void Awake()
    {
        ps.gameObject.SetActive(false);
    }

    private void Start()
    {
        _mainCamera = Camera.main;
        
        EventComponent.Instance.Subscribe(GotCollectiveEventArgs.EventId, OnGotCollective);
    }

    private void OnDestroy()
    {
        EventComponent.Instance.Unsubscribe(GotCollectiveEventArgs.EventId, OnGotCollective);
    }

    private void OnGotCollective(object sender, GameEventArgs e)
    {
        var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out var hit)) return;
        
        if (worldObjects.Contains(hit.collider.gameObject))
        {
            DOVirtual.DelayedCall(0.2f, OnCollected);
        }
    }

    private void OnCollected()
    {
        foreach (var worldObject in worldObjects)
        {
            worldObject.SetActive(false);
        }
        
        ps.gameObject.SetActive(true);
        ps.transform.localPosition = Vector3.zero;
        var psMain = ps.main;
        psMain.loop = true;
        var distance = Vector3.Distance(receiver.transform.position, transform.position);
        var psMoveDuration = distance / ParticleSystemSpeed;
        ps.transform.DOMove(receiver.transform.position, psMoveDuration)
            .OnComplete(() =>
            {
                psMain.loop = false;
                receiver.Unlock();
            });
    }
}