namespace GameEvent.Args
{
    public sealed class GotCollectiveEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(GotCollectiveEventArgs).GetHashCode();

        public override int Id => EventId;
    
        public static GotCollectiveEventArgs Create()
        {
            var e = ReferencePool.Acquire<GotCollectiveEventArgs>();
            return e;
        }
    
        public override void Clear()
        {
        }
    }
}