using System.Collections.Generic;
using GameEvent;
using GameEvent.Args;
using UnityEngine;

namespace DefaultNamespace
{
    public class CollectedJigsawsUI : MonoBehaviour
    {
        [SerializeField] private JigsawUI jigsawUIPrefab;
        
        public static CollectedJigsawsUI Instance { get; private set; }
        
        private HashSet<GameObject> _currentLevelJigsaws = new();

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
            EventComponent.Instance.Subscribe(ExitLevelEventArgs.EventId, OnExitedLevel);
            EventComponent.Instance.Subscribe(LevelResetEventArgs.EventId, OnLevelReset);
        }

        private void OnDestroy()
        {
            EventComponent.Instance.Unsubscribe(CapturedJigsawEventArgs.EventId, OnCapturedJigsaw);
            EventComponent.Instance.Unsubscribe(ExitLevelEventArgs.EventId, OnExitedLevel);
            EventComponent.Instance.Subscribe(LevelResetEventArgs.EventId, OnLevelReset);
        }

        private void OnCapturedJigsaw(object sender, GameEventArgs e)
        {
            if (e is not CapturedJigsawEventArgs args) return;
            
            var jigsawUI = Utility.GetOrAdd(jigsawUIPrefab, transform);
            jigsawUI.Init(args);
            _currentLevelJigsaws.Add(jigsawUI.gameObject);
        }

        private void OnExitedLevel(object sender, GameEventArgs e)
        {
            _currentLevelJigsaws.Clear();
        }

        private void OnLevelReset(object sender, GameEventArgs e)
        {
            foreach (var jigsawGameObject in _currentLevelJigsaws)
            {
                jigsawGameObject.SetActive(false);
            }
            _currentLevelJigsaws.Clear();
        }
    }
}