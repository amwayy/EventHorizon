using System.Collections;
using System.Collections.Generic;
using GameEvent;
using GameEvent.Args;
using UnityEngine;
using UnityEngine.Assertions;

namespace DefaultNamespace
{
    public class CollectedJigsawsUI : MonoBehaviour
    {
        [SerializeField] private JigsawUI jigsawUIPrefab;
        
        public static CollectedJigsawsUI Instance { get; private set; }
        
        private readonly Dictionary<JigsawCollective, JigsawUI> _collectedJigsaws = new();
        private readonly Dictionary<JigsawSlot, JigsawUI> _putJigsaws = new();

        private JigsawUI _lastJigsawUI;

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
            
            var jigsawUI = Instantiate(jigsawUIPrefab, transform);
            jigsawUI.Init(args);
            _lastJigsawUI = jigsawUI;   
        }

        public void AddJigsaw(JigsawCollective collective)
        {
            StartCoroutine(DelayAddJigsaw(collective));
        }

        private IEnumerator DelayAddJigsaw(JigsawCollective collective)
        {
            yield return null;
            
            Assert.IsTrue(_lastJigsawUI);
            if (!_lastJigsawUI) yield break;
            
            _collectedJigsaws[collective] = _lastJigsawUI;
            _lastJigsawUI = null;
        }

        public void PutJigsawOnSlot(JigsawUI jigsawUI, JigsawSlot slot)
        {
            jigsawUI.gameObject.SetActive(false);
            
            _putJigsaws[slot] = jigsawUI;
        }
        
        public void OnResetCollective(JigsawCollective collective)
        {
            if (_collectedJigsaws.TryGetValue(collective, out var jigsawUI))
            {
                jigsawUI.gameObject.SetActive(false);
                
                JigsawSlot targetSlot = null;
                foreach (var (slot, jigsaw) in _putJigsaws)
                {
                    if (jigsaw == jigsawUI)
                    {
                        targetSlot = slot;
                        break;
                    }
                }
                if (targetSlot)
                {
                    targetSlot.ClearJigsaw();
                }
                
                _collectedJigsaws.Remove(collective);
            }
        }

        public void OnResetSlot(JigsawSlot slot)
        {
            if (!_putJigsaws.TryGetValue(slot, out var jigsawUI)) return;
            
            jigsawUI.gameObject.SetActive(false);
            
            JigsawCollective targetCollective = null;
            foreach (var (collective, jigsaw) in _collectedJigsaws)
            {
                if (jigsaw == jigsawUI)
                {
                    targetCollective = collective;
                    break;
                }
            }
            if (targetCollective)
            {
                targetCollective.gameObject.SetActive(true);
                targetCollective.ResetState(sendNotification: false);
            }
            _putJigsaws.Remove(slot);
        }

        public void ResetCollection()
        {
            foreach (var (collective, jigsawUI) in _collectedJigsaws)
            {
                if (!jigsawUI.gameObject.activeSelf) continue;
                jigsawUI.gameObject.SetActive(false);
                collective.ResetState(sendNotification: false);
            }
            _collectedJigsaws.Clear();
        }
    }
}