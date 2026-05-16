using UnityEngine;

namespace DefaultNamespace
{
    public class WallJigsawCollective : JigsawCollective
    {
        [SerializeField] private CuttableWall cuttableWall;
        
        public override void ResetState(bool sendNotification = true)
        {
            base.ResetState(sendNotification);
            
            cuttableWall.ResetStateInternally();
        }
    }
}