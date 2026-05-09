namespace GameEvent.Args
{
    public sealed class ToggleOpenMenuEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(ToggleOpenMenuEventArgs).GetHashCode();

        public override int Id => EventId;
        
        public bool IsOn { get; private set; }
    
        public static ToggleOpenMenuEventArgs Create(bool isOn)
        {
            var e = ReferencePool.Acquire<ToggleOpenMenuEventArgs>();
            e.IsOn = isOn;
            
            return e;
        }
    
        public override void Clear()
        {
        }
    }
}