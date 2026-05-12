using UnityEngine;

namespace DefaultNamespace
{
    public class JigsawBarrier : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (!other.gameObject.TryGetComponent(out CharacterController _)) return;

            CollectedJigsawsUI.Instance.ResetCollection();
        }
    }
}