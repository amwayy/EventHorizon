using System;
using System.Linq;
using DefaultNamespace;
using GameEvent;
using GameEvent.Args;
using UnityEngine;

[Serializable]
public struct JigsawPartsData
{
    public string jigsawName;
    public GameObject[] jigsawParts;
}

public class JigsawCollective : MonoBehaviour
{
    [SerializeField] private GameObject[] worldObjects;
    [SerializeField] private JigsawPartsData[] jigsawPartsData;
    [SerializeField] private bool hasMultipleSolutions;

    private void Start()
    {
        EventComponent.Instance.Subscribe(CapturedJigsawEventArgs.EventId, OnGotCollective);
    }

    private void OnDestroy()
    {
        EventComponent.Instance.Unsubscribe(CapturedJigsawEventArgs.EventId, OnGotCollective);
    }

    private void OnGotCollective(object sender, GameEventArgs e)
    {
        if (e is not CapturedJigsawEventArgs args) return;

        var jigsawParts = worldObjects;
        if (hasMultipleSolutions)
        {
            var result = Array.Find(
                jigsawPartsData,
                data => data.jigsawName == args.JigsawData.jigsawName
            );
            if (string.IsNullOrEmpty(result.jigsawName)) return;
            jigsawParts = result.jigsawParts;
        }
        
        if (jigsawParts.Contains(args.HitGameObject))
        {
            foreach (var worldObject in jigsawParts)
            {
                worldObject.SetActive(false);
            }
            CollectedJigsawsUI.Instance.AddJigsaw(this);
        }
    }

    public void ResetState(bool sendNotification = true)
    {
        foreach (var worldObject in worldObjects)
        {
            // resync animations
            worldObject.gameObject.SetActive(false);
            worldObject.gameObject.SetActive(true);
        }

        if (sendNotification)
        {
            CollectedJigsawsUI.Instance.OnResetCollective(this);   
        }
    }
}