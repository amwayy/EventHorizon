namespace GameEvent.Args
{
    public sealed class SwitchCameraEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(SwitchCameraEventArgs).GetHashCode();

        public override int Id => EventId;
        public int CameraIndex { get; private set; }
    
        public static SwitchCameraEventArgs Create(int index)
        {
            var e = ReferencePool.Acquire<SwitchCameraEventArgs>();
            e.CameraIndex = index;
            return e;
        }
    
        public override void Clear()
        {
        }
    }
}