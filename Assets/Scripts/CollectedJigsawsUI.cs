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
        
        private readonly Dictionary<int, List<GameObject>> _jigsawsByLevel = new();

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

            var currentLevelIndex = LevelManager.Instance.CurrentLevelIndex;
            if (!_jigsawsByLevel.TryGetValue(currentLevelIndex, out var jigsawGameObjects))
            {
                jigsawGameObjects = new List<GameObject>();
                _jigsawsByLevel.Add(currentLevelIndex, jigsawGameObjects);
            }
            jigsawGameObjects.Add(jigsawUI.gameObject);
        }

        private void OnExitedLevel(object sender, GameEventArgs e)
        {
            if (e is not ExitLevelEventArgs args) return;
            if (!_jigsawsByLevel.TryGetValue(args.LevelIndex, out var jigsawGameObjects)) return;
            jigsawGameObjects.RemoveAll(x => !x.activeSelf);
            if (jigsawGameObjects.Count == 0)
            {
                _jigsawsByLevel.Remove(args.LevelIndex);
            }
        }

        private void OnLevelReset(object sender, GameEventArgs e)
        {
            var currentLevelIndex = LevelManager.Instance.CurrentLevelIndex;
            if (!_jigsawsByLevel.TryGetValue(currentLevelIndex, out var jigsawGameObjects)) return;
            foreach (var jigsawGameObject in jigsawGameObjects)
            {
                jigsawGameObject.SetActive(false);
            }
            jigsawGameObjects.Clear();
        }
    }
}