using UnityEngine;

namespace DefaultNamespace
{
    public class JigsawManager : MonoBehaviour
    {
        [SerializeField] private JigsawDatabase jigsawDatabase;
        
        public static JigsawManager Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
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