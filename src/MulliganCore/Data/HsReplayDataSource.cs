using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HstMulligan.Core.Models;

namespace HstMulligan.Core.Data
{
    /// <summary>
    /// Reads the community HSReplay-shaped mulligan JSON. Expected wire form:
    /// {
    ///   "series": {
    ///     "data": {
    ///       "ALL":  [ { "dbf_id": 12345, "kept_percent": 74.2, "sample_size": 1000, "winrate_when_kept": 53.0 }, ... ],
    ///       "MAGE": [ ... ]
    ///     },
    ///     "metadata": { "generated_at": "...", "base_winrate": 50.5 }
    ///   }
    /// }
    /// </summary>
    public sealed class HsReplayDataSource : IMulliganDataSource
    {
        public string Name => "hsreplay";

        private readonly HttpClient _http;
        private readonly string _urlTemplate;

        public HsReplayDataSource(HttpClient http, string urlTemplate)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _urlTemplate = urlTemplate ?? throw new ArgumentNullException(nameof(urlTemplate));
        }

        public async Task<MulliganDataset> FetchAsync(MulliganQuery query, CancellationToken ct)
        {
            var url = _urlTemplate
                .Replace("{format}", query.Format.ToWireString())
                .Replace("{rank}", query.RankBracket.ToWireString())
                .Replace("{shortId}", query.DeckShortId ?? "")
                .Replace("{deck}", query.DeckCode ?? "");
            using (var resp = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false))
            {
                resp.EnsureSuccessStatusCode();
                using (var stream = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false))
                {
                    return Parse(stream, query);
                }
            }
        }

        internal static MulliganDataset Parse(Stream jsonStream, MulliganQuery query)
        {
            using var doc = JsonDocument.Parse(jsonStream);
            return Parse(doc.RootElement, query);
        }

        internal static MulliganDataset Parse(JsonElement root, MulliganQuery query)
        {
            var generated = DateTimeOffset.UtcNow;
            double baseWr = 0.5;
            var series = root.TryGetProperty("series", out var s) ? s : root;
            if (series.TryGetProperty("metadata", out var meta))
            {
                if (meta.TryGetProperty("generated_at", out var g) && g.ValueKind == JsonValueKind.String &&
                    DateTimeOffset.TryParse(g.GetString(), out var parsed))
                    generated = parsed;
                if (meta.TryGetProperty("base_winrate", out var bw) && bw.ValueKind == JsonValueKind.Number)
                    baseWr = ToFraction(bw.GetDouble());
            }

            var data = series.TryGetProperty("data", out var d) ? d : series;
            var perDbf = new Dictionary<int, Bucket>();

            foreach (var opp in data.EnumerateObject())
            {
                var arr = opp.Value;
                if (arr.ValueKind != JsonValueKind.Array) continue;
                var key = opp.Name;
                var oppClass = OpponentClassExtensions.FromWireString(key);
                var coinKind = CoinKindOf(key);
                foreach (var row in arr.EnumerateArray())
                {
                    if (!row.TryGetProperty("dbf_id", out var idEl)) continue;
                    var dbf = idEl.GetInt32();
                    var sample = ToSample(row);
                    if (!perDbf.TryGetValue(dbf, out var entry))
                        perDbf[dbf] = entry = new Bucket();
                    if (coinKind == CoinKind.OnPlay)      entry.OnPlay = sample;
                    else if (coinKind == CoinKind.OnCoin) entry.OnCoin = sample;
                    else if (key.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                        entry.Overall = sample;
                    else if (oppClass != OpponentClass.Unknown)
                        entry.ByOpp[oppClass] = sample;
                }
            }

            var cards = new Dictionary<int, CardStats>();
            foreach (var kv in perDbf)
            {
                var overall = kv.Value.Overall.HasData
                    ? kv.Value.Overall
                    : AggregateOverOpponents(kv.Value.ByOpp);
                cards[kv.Key] = new CardStats(kv.Key, overall, kv.Value.ByOpp,
                    byDeck: null, byArchetype: null,
                    onPlay: kv.Value.OnPlay.HasData ? kv.Value.OnPlay : (KeepRateSample?)null,
                    onCoin: kv.Value.OnCoin.HasData ? kv.Value.OnCoin : (KeepRateSample?)null);
            }
            return new MulliganDataset(query.Format, query.RankBracket, generated, baseWr, cards);
        }

        private enum CoinKind { None, OnPlay, OnCoin }

        private static CoinKind CoinKindOf(string key)
        {
            if (string.IsNullOrEmpty(key)) return CoinKind.None;
            var k = key.ToUpperInvariant();
            if (k == "ON_PLAY" || k == "ONPLAY" || k == "PLAY" || k == "NO_COIN" || k == "FIRST") return CoinKind.OnPlay;
            if (k == "ON_COIN" || k == "ONCOIN" || k == "COIN" || k == "HAS_COIN" || k == "SECOND") return CoinKind.OnCoin;
            return CoinKind.None;
        }

        private sealed class Bucket
        {
            public KeepRateSample Overall = KeepRateSample.Empty;
            public KeepRateSample OnPlay = KeepRateSample.Empty;
            public KeepRateSample OnCoin = KeepRateSample.Empty;
            public Dictionary<OpponentClass, KeepRateSample> ByOpp = new Dictionary<OpponentClass, KeepRateSample>();
        }

        private static KeepRateSample ToSample(JsonElement row)
        {
            int sample = ReadIntFlexible(row, "sample_size");
            double keptPct = ReadDoubleFlexible(row, "kept_percent", double.NaN);
            double keptWr = ReadDoubleFlexible(row, "winrate_when_kept", double.NaN);
            double drawnWr = ReadDoubleFlexible(row, "winrate_when_drawn", double.NaN);
            if (sample <= 0 || double.IsNaN(keptPct)) return KeepRateSample.Empty;
            var keptFrac = ToFraction(keptPct);
            int kept = (int)Math.Round(sample * keptFrac);
            int mull = sample - kept;
            var kwr = double.IsNaN(keptWr) ? double.NaN : ToFraction(keptWr);
            var dwr = double.IsNaN(drawnWr) ? double.NaN : ToFraction(drawnWr);
            return new KeepRateSample(kept, mull, kwr, dwr);
        }

        private static KeepRateSample AggregateOverOpponents(Dictionary<OpponentClass, KeepRateSample> byOpp)
        {
            var agg = KeepRateSample.Empty;
            foreach (var s in byOpp.Values) agg = KeepRateSample.Combine(agg, s);
            return agg;
        }

        private static double ToFraction(double v) => v > 1.0 ? v / 100.0 : v;

        private static int ReadIntFlexible(JsonElement el, string name)
        {
            if (!el.TryGetProperty(name, out var v)) return 0;
            if (v.ValueKind == JsonValueKind.Number) return v.GetInt32();
            if (v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), out var i)) return i;
            return 0;
        }

        private static double ReadDoubleFlexible(JsonElement el, string name, double dflt)
        {
            if (!el.TryGetProperty(name, out var v)) return dflt;
            if (v.ValueKind == JsonValueKind.Number) return v.GetDouble();
            if (v.ValueKind == JsonValueKind.String && double.TryParse(v.GetString(),
                System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d)) return d;
            return dflt;
        }
    }
}
