using UnityEngine;

namespace GameEvent.Args
{
    public sealed class CapturedJigsawEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(CapturedJigsawEventArgs).GetHashCode();

        public override int Id => EventId;
        
        public int Angle { get; private set; }
        public JigsawSO JigsawData { get; private set; }
        public RenderTexture CapturedJigsawRT { get; private set; }
        public Vector2Int BBoxCenter { get; private set; }
        public Color Color { get; private set; }
        public GameObject HitGameObject { get; private set; }
    
        public static CapturedJigsawEventArgs Create(
            int angle, JigsawSO jigsawData, RenderTexture capturedJigsawRT, 
            Vector2Int bBoxCenter, Color color, GameObject hitGameObject)
        {
            var e = ReferencePool.Acquire<CapturedJigsawEventArgs>();
            e.Angle = angle;
            e.JigsawData = jigsawData;
            e.CapturedJigsawRT = capturedJigsawRT;
            e.BBoxCenter = bBoxCenter;
            e.Color = color;
            e.HitGameObject = hitGameObject;
            return e;
        }
    
        public override void Clear()
        {
        }
    }
}