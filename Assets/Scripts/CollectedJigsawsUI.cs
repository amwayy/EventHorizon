using System;
using GameEvent;
using GameEvent.Args;
using UnityEngine;

namespace DefaultNamespace
{
    public class CollectedJigsawsUI : MonoBehaviour
    {
        [SerializeField] private JigsawUI jigsawUIPrefab;
        
        public static CollectedJigsawsUI Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            EventComponent.Instance.Subscribe(CapturedJigsawEventArgs.EventId, OnCapturedJigsaw);
        }

        private void OnDestroy()
        {
            EventComponent.Instance.Unsubscribe(CapturedJigsawEventArgs.EventId, OnCapturedJigsaw);
        }

        private void OnCapturedJigsaw(object sender, GameEventArgs e)
        {
            if (e is not CapturedJigsawEventArgs args) return;
            
            var jigsawUI = Utility.GetOrAdd(jigsawUIPrefab, transform);
            jigsawUI.Init(args);
        }
    }
}