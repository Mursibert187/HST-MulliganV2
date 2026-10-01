using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace HstMulligan.Core.Abstractions
{
    public interface IEventBus
    {
        IDisposable Subscribe<TEvent>(Action<TEvent> handler);
        void Publish<TEvent>(TEvent evt);
    }

    public sealed class EventBus : IEventBus
    {
        private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers =
            new ConcurrentDictionary<Type, List<Delegate>>();
        private readonly ILogger _log;

        public EventBus(ILogger log = null)
        {
            _log = log ?? NullLogger.Instance;
        }

        public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var list = _handlers.GetOrAdd(typeof(TEvent), _ => new List<Delegate>());
            lock (list) list.Add(handler);
            return new Subscription(() =>
            {
                if (_handlers.TryGetValue(typeof(TEvent), out var l))
                    lock (l) l.Remove(handler);
            });
        }

        public void Publish<TEvent>(TEvent evt)
        {
            if (!_handlers.TryGetValue(typeof(TEvent), out var list)) return;
            Delegate[] snapshot;
            lock (list) snapshot = list.ToArray();
            foreach (var d in snapshot)
            {
                try { ((Action<TEvent>)d)(evt); }
                catch (Exception ex) { _log.Warn($"event handler for {typeof(TEvent).Name} threw", ex); }
            }
        }

        private sealed class Subscription : IDisposable
        {
            private Action _dispose;
            public Subscription(Action dispose) { _dispose = dispose; }
            public void Dispose() { var d = _dispose; _dispose = null; d?.Invoke(); }
        }
    }
}
