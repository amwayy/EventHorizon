using System;

namespace GameEvent.Args
{
    public abstract class GameEventArgs : EventArgs, IReference
    {
        public abstract int Id { get; }
    
        public abstract void Clear();
    }
}