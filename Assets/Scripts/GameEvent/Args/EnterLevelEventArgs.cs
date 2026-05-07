namespace GameEvent.Args
{
    public sealed class EnterLevelEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(EnterLevelEventArgs).GetHashCode();

        public override int Id => EventId;
        public int LevelIndex { get; private set; }
    
        public static EnterLevelEventArgs Create(int index)
        {
            var e = ReferencePool.Acquire<EnterLevelEventArgs>();
            e.LevelIndex = index;
            
            return e;
        }
    
        public override void Clear()
        {
        }
    }
}