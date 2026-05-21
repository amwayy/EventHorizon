using UnityEngine;

namespace DefaultNamespace
{
    public struct WallCollectiveData
    {
        public int LevelId;
        public int WallIndex;
        public string JigsawName;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
    }
    
    public class WallJigsawCollective : JigsawCollective
    {
        [SerializeField] private CuttableWall cuttableWall;
        
        public string JigsawName { get; private set; }
        
        public override void ResetState(bool sendNotification = true)
        {
            base.ResetState(sendNotification);
            
            cuttableWall.ResetStateInternally();
        }

        public override void Init(int levelId, int wallIndex)
        {
            LevelId = levelId;
            CollectiveIndex = wallIndex;
        }

        public WallCollectiveData GetCollectiveData()
        {
            return new WallCollectiveData()
            {
                LevelId = LevelId,
                WallIndex = CollectiveIndex,
                JigsawName = JigsawName,
                Position = transform.position,
                Rotation = transform.rotation,
                Scale = transform.localScale,
            };
        }

        public void SetJigsawName(string jigsawName)
        {
            JigsawName = jigsawName;
        }
    }
}