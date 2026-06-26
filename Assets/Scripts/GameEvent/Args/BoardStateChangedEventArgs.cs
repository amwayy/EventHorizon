namespace GameEvent.Args
{
    public sealed class BoardStateChangedEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(BoardStateChangedEventArgs).GetHashCode();

        public override int Id => EventId;
        
        public bool IsFilled { get; private set; }
        public bool IsEmpty { get; private set; }
    
        public static BoardStateChangedEventArgs Create(bool isFilled, bool isEmpty)
        {
            var e = ReferencePool.Acquire<BoardStateChangedEventArgs>();
            e.IsFilled = isFilled;
            e.IsEmpty = isEmpty;
            
            return e;
        }
    
        public override void Clear()
        {
        }
    }
}