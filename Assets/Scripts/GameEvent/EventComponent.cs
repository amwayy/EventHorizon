using System;
using System.Collections.Generic;
using GameEvent.Args;
using UnityEngine;

namespace GameEvent
{
    public sealed class EventComponent : MonoBehaviour
    {
        public static EventComponent Instance { get; private set; }
    
        private readonly Dictionary<int, LinkedList<EventHandler<GameEventArgs>>> _eventHandlers
            = new();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }
    
        public void Subscribe(int eventId, EventHandler<GameEventArgs> handler)
        {
            if (handler == null)
            {
                Debug.LogError("Event handler is null.");
                return;
            }

            if (!_eventHandlers.TryGetValue(eventId, out var handlers))
            {
                handlers = new LinkedList<EventHandler<GameEventArgs>>();
                _eventHandlers.Add(eventId, handlers);
            }

            if (!handlers.Contains(handler))
            {
                handlers.AddLast(handler);
            }
        }

        public void Unsubscribe(int eventId, EventHandler<GameEventArgs> handler)
        {
            if (handler == null)
                return;

            if (_eventHandlers.TryGetValue(eventId, out var handlers))
            {
                handlers.Remove(handler);
            }
        }

        public void Fire(object sender, GameEventArgs e)
        {
            if (e == null)
            {
                return;
            }

            if (!_eventHandlers.TryGetValue(e.Id, out var handlers))
                return;

            var node = handlers.First;
            while (node != null)
            {
                var handler = node.Value;
                node = node.Next;
                handler?.Invoke(sender, e);
            }
        
            ReferencePool.Release(e);
        }

        private void Clear()
        {
            _eventHandlers.Clear();
        }

        private void OnDestroy()
        {
            Clear();
        }
    }
}
