using UnityEngine;

namespace DefaultNamespace
{
    public class JigsawManager : MonoBehaviour
    {
        [SerializeField] private JigsawDatabase jigsawDatabase;
        
        public static JigsawManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public JigsawSO GetJigsawSo(string jigsawName)
        {
            foreach (var jigsawSo in jigsawDatabase.allJigsaws)
            {
                if (jigsawSo.jigsawName == jigsawName)
                {
                    return jigsawSo;
                }
            }
            return null;
        }
    }
}