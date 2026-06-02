using UnityEngine;

namespace DefaultNamespace
{
    public class JigsawBarrier : MonoBehaviour
    {
        [SerializeField] private ColorType colorType;
        
        private void Awake()
        {
            var rd = GetComponent<Renderer>();
            rd.material.color = Colors.GetBarrierColor(colorType);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.gameObject.TryGetComponent(out CharacterController _)) return;

            var exceptedColor = Colors.GetJigsawColor(colorType);
            CollectedJigsawsUI.Instance.ResetCollection(exceptedColor);
        }
    }
}