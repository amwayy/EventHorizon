using UnityEngine;
using UnityEngine.UI;

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

        public void AddJigsaw(Vector3 screenPosition, int angle, Vector2Int size, Texture2D texture)
        {
            var jigsawUI = Utility.GetOrAdd(jigsawUIPrefab, transform);
            jigsawUI.Init(texture, angle);
            jigsawUI.RectTransform.position = screenPosition;
            jigsawUI.RectTransform.sizeDelta = size;
        }
    }
}