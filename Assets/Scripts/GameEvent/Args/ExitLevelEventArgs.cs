namespace GameEvent.Args
{
    public sealed class ExitLevelEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(ExitLevelEventArgs).GetHashCode();

        public override int Id => EventId;
    
        public static ExitLevelEventArgs Create()
        {
            var e = ReferencePool.Acquire<ExitLevelEventArgs>();
            
            return e;
        }
    
        public override void Clear()
        {
        }
    }
}