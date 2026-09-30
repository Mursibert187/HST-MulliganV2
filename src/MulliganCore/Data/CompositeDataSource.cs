using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HstMulligan.Core.Models;

namespace HstMulligan.Core.Data
{
    /// <summary>
    /// Fetches from the first source that returns a non-empty dataset, then
    /// merges lower-priority sources underneath so missing cards can be filled
    /// in from a broader (e.g. all-ranks) feed.
    /// </summary>
    public sealed class CompositeDataSource : IMulliganDataSource
    {
        public string Name => "composite";

        private readonly IReadOnlyList<IMulliganDataSource> _sources;

        public CompositeDataSource(IEnumerable<IMulliganDataSource> sources)
        {
            if (sources == null) throw new ArgumentNullException(nameof(sources));
            _sources = new List<IMulliganDataSource>(sources).AsReadOnly();
            if (_sources.Count == 0) throw new ArgumentException("at least one source required", nameof(sources));
        }

        public async Task<MulliganDataset> FetchAsync(MulliganQuery query, CancellationToken ct)
        {
            MulliganDataset primary = null;
            var underlays = new List<MulliganDataset>();
            foreach (var s in _sources)
            {
                MulliganDataset ds;
                try { ds = await s.FetchAsync(query, ct).ConfigureAwait(false); }
                catch { continue; }
                if (ds == null || ds.Cards.Count == 0) continue;
                if (primary == null) primary = ds;
                else underlays.Add(ds);
            }
            if (primary == null) return MulliganDataset.Empty;
            foreach (var u in underlays) primary = primary.MergeUnder(u);
            return primary;
        }
    }
}
