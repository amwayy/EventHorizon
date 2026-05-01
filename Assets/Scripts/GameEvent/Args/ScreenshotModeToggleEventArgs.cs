namespace GameEvent.Args
{
    public sealed class ScreenshotModeToggleEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(ScreenshotModeToggleEventArgs).GetHashCode();

        public override int Id => EventId;
        
        public bool IsOn { get; private set; }
    
        public static ScreenshotModeToggleEventArgs Create(bool isOn)
        {
            var e = ReferencePool.Acquire<ScreenshotModeToggleEventArgs>();
            e.IsOn = isOn;
            
            return e;
        }
    
        public override void Clear()
        {
        }
    }
}