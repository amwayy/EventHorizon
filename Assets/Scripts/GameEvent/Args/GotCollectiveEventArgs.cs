using UnityEngine;

namespace GameEvent.Args
{
    public sealed class GotCollectiveEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(GotCollectiveEventArgs).GetHashCode();

        public override int Id => EventId;
        
        public int Angle { get; private set; }
        public Vector2Int BBoxSize { get; private set; }
        public Texture2D TargetTexture { get; private set; }
    
        public static GotCollectiveEventArgs Create(int angle, Vector2Int bboxSize, Texture2D texture)
        {
            var e = ReferencePool.Acquire<GotCollectiveEventArgs>();
            e.Angle = angle;
            e.BBoxSize = bboxSize;
            e.TargetTexture = texture;
            return e;
        }
    
        public override void Clear()
        {
        }
    }
}