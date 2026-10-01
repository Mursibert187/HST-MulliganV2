using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HstMulligan.Core.Models;

namespace HstMulligan.Core.Abstractions
{
    /// <summary>
    /// Fetches an archetype index JSON from a URL, caches the parsed result
    /// in-memory, and re-fetches on a TTL. Resolve() is synchronous and
    /// returns null until the first prime completes; the prime kicks off
    /// in the background on construction so by the time the user hits the
    /// mulligan screen the index is usually warm.
    /// </summary>
    public sealed class RemoteArchetypeIndex : IArchetypeIndex, IDisposable
    {
        private readonly HttpClient _http;
        private readonly string _url;
        private readonly TimeSpan _ttl;
        private readonly ILogger _log;
        private readonly Func<DateTimeOffset> _clock;

        private IReadOnlyDictionary<OpponentClass, IReadOnlyDictionary<string, string>> _snapshot
            = ArchetypeIndexParser.Empty;
        private DateTimeOffset _loadedAt = DateTimeOffset.MinValue;
        private CancellationTokenSource _refreshCts;
        private readonly object _primeGate = new object();
        private Task _inflight;

        public RemoteArchetypeIndex(HttpClient http, string url, TimeSpan ttl,
            ILogger log = null, Func<DateTimeOffset> clock = null, bool primeImmediately = true)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _url  = url ?? throw new ArgumentNullException(nameof(url));
            _ttl  = ttl;
            _log  = log ?? NullLogger.Instance;
            _clock = clock ?? (() => DateTimeOffset.UtcNow);
            if (primeImmediately) KickRefresh();
        }

        public string Resolve(OpponentClass heroClass, IReadOnlyCollection<int> dbfIds)
        {
            if (dbfIds == null || dbfIds.Count == 0) return null;
            if (_clock() - _loadedAt > _ttl) KickRefresh();
            if (!_snapshot.TryGetValue(heroClass, out var sigs)) return null;
            return sigs.TryGetValue(Signature.Compute(dbfIds), out var id) ? id : null;
        }

        public Task PrimeAsync(CancellationToken ct = default) => RefreshNow(ct);

        private void KickRefresh()
        {
            lock (_primeGate)
            {
                if (_inflight != null && !_inflight.IsCompleted) return;
                _refreshCts?.Cancel();
                _refreshCts = new CancellationTokenSource();
                _inflight = Task.Run(() => RefreshNow(_refreshCts.Token));
            }
        }

        private async Task RefreshNow(CancellationToken ct)
        {
            try
            {
                using var resp = await _http.GetAsync(_url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
                resp.EnsureSuccessStatusCode();
                using var stream = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false);
                var parsed = ArchetypeIndexParser.Parse(stream);
                Interlocked.Exchange(ref _snapshot, parsed);
                _loadedAt = _clock();
                _log.Info($"RemoteArchetypeIndex loaded ({parsed.Count} classes)");
            }
            catch (Exception ex)
            {
                _log.Warn("RemoteArchetypeIndex fetch failed", ex);
            }
        }

        public void Dispose()
        {
            _refreshCts?.Cancel();
            _refreshCts?.Dispose();
        }
    }
}
