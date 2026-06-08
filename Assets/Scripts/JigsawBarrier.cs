using GameEvent;
using GameEvent.Args;
using UnityEngine;

namespace DefaultNamespace
{
    public class JigsawBarrier : MonoBehaviour
    {
        [SerializeField] private ColorType colorType;
        [SerializeField] private Renderer rd;
        [SerializeField] private float opacity = 0.5f;
        
        private static readonly int Alpha = Shader.PropertyToID("_Alpha");
        
        private int _levelId;

        private void Start()
        {
            UpdateColor();
            
            EventComponent.Instance.Subscribe(BoardStateChangedEventArgs.EventId, OnBoardStateChanged);
        }

        public void Init(int levelId, bool isLevelSolved)
        {
            _levelId = levelId;
            colorType = isLevelSolved ? ColorType.Green : ColorType.Red;
            UpdateColor();
        }

        private void OnEnable()
        {
            if (EventComponent.Instance)
            {
                EventComponent.Instance.Subscribe(BoardStateChangedEventArgs.EventId, OnBoardStateChanged);   
            }
        }

        private void OnDisable()
        {
            EventComponent.Instance.Unsubscribe(BoardStateChangedEventArgs.EventId, OnBoardStateChanged);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.gameObject.TryGetComponent(out CharacterController _)) return;

            CollectedJigsawsUI.Instance.OnGoThroughBarrier();
        }

        private void OnBoardStateChanged(object sender, GameEventArgs e)
        {
            if (LevelManager.Instance.CurrentLevelIndex != _levelId) return;
            if (e is not BoardStateChangedEventArgs args) return;

            colorType = args.IsFilled ? ColorType.Green : ColorType.Red;
            UpdateColor();
        }

        private void UpdateColor()
        {
            rd.material.color = Colors.GetBarrierColor(colorType);
            rd.material.SetFloat(Alpha, opacity);
        }
    }
}