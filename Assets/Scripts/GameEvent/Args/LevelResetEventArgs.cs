namespace GameEvent.Args
{
    public sealed class LevelResetEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(LevelResetEventArgs).GetHashCode();

        public override int Id => EventId;
    
        public static LevelResetEventArgs Create()
        {
            var e = ReferencePool.Acquire<LevelResetEventArgs>();
            
            return e;
        }
    
        public override void Clear()
        {
        }
    }
}