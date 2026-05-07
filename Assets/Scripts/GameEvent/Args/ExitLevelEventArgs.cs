namespace GameEvent.Args
{
    public sealed class ExitLevelEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(ExitLevelEventArgs).GetHashCode();

        public override int Id => EventId;
    
        public int LevelIndex { get; private set; }
        
        public static ExitLevelEventArgs Create(int levelIndex)
        {
            var e = ReferencePool.Acquire<ExitLevelEventArgs>();
            e.LevelIndex = levelIndex;
            
            return e;
        }
    
        public override void Clear()
        {
        }
    }
}