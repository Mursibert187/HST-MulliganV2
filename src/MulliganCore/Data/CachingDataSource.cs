using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using HstMulligan.Core.Models;

namespace HstMulligan.Core.Data
{
    /// <summary>
    /// Wraps another IMulliganDataSource with an in-memory TTL cache.
    /// Failed fetches surface the last-good value if one is still on hand.
    /// </summary>
    public sealed class CachingDataSource : IMulliganDataSource
    {
        public string Name => $"cache({_inner.Name})";

        private sealed class Entry
        {
            public MulliganDataset Value;
            public DateTimeOffset LoadedAt;
        }

        private readonly IMulliganDataSource _inner;
        private readonly TimeSpan _ttl;
        private readonly ConcurrentDictionary<string, Entry> _entries = new ConcurrentDictionary<string, Entry>();
        private readonly Func<DateTimeOffset> _clock;

        public CachingDataSource(IMulliganDataSource inner, TimeSpan ttl, Func<DateTimeOffset> clock = null)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _ttl = ttl;
            _clock = clock ?? (() => DateTimeOffset.UtcNow);
        }

        public async Task<MulliganDataset> FetchAsync(MulliganQuery query, CancellationToken ct)
        {
            var key = query.CacheKey;
            if (_entries.TryGetValue(key, out var entry) && _clock() - entry.LoadedAt < _ttl && entry.Value != null)
                return entry.Value;

            try
            {
                var fresh = await _inner.FetchAsync(query, ct).ConfigureAwait(false);
                if (fresh != null && fresh.Cards.Count > 0)
                {
                    _entries[key] = new Entry { Value = fresh, LoadedAt = _clock() };
                    return fresh;
                }
            }
            catch when (entry?.Value != null)
            {
                return entry.Value;
            }

            return entry?.Value ?? MulliganDataset.Empty;
        }

        public void Invalidate() => _entries.Clear();
    }
}
