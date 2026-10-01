using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HstMulligan.Core.Models;

namespace HstMulligan.Core.Data
{
    /// <summary>
    /// Reads the deck-specific mulligan guide by scraping the HSReplay deck
    /// page's inlined React hydration data. HSReplay does not expose a
    /// public JSON endpoint for per-deck mulligan stats; the deck page
    /// embeds the same numbers that it later renders, so we lift them from
    /// the page HTML without a browser.
    ///
    /// Robust to shape drift: we try the Next.js `__NEXT_DATA__` script
    /// first, then the older `data-props` React container pattern, and we
    /// walk the resulting JSON tree looking for the row shape rather than
    /// relying on a fixed path. If the page is Premium-gated for the
    /// current session, the scraper returns MulliganDataset.Empty so the
    /// composite pipeline can fall back.
    /// </summary>
    public sealed class DeckPageScraperSource : IMulliganDataSource
    {
        public string Name => "hsreplay-deck-page";

        private readonly HttpClient _http;
        private readonly string _urlTemplate;
        private readonly string _sessionCookie;

        public DeckPageScraperSource(HttpClient http, string urlTemplate = null, string sessionCookie = null)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _urlTemplate = string.IsNullOrWhiteSpace(urlTemplate)
                ? "https://hsreplay.net/decks/{shortId}/"
                : urlTemplate;
            _sessionCookie = sessionCookie;
        }

        public async Task<MulliganDataset> FetchAsync(MulliganQuery query, CancellationToken ct)
        {
            if (query == null || string.IsNullOrEmpty(query.DeckShortId))
                return MulliganDataset.Empty;

            var url = _urlTemplate
                .Replace("{shortId}", query.DeckShortId)
                .Replace("{format}", query.Format.ToWireString())
                .Replace("{rank}", query.RankBracket.ToWireString());

            using (var req = new HttpRequestMessage(HttpMethod.Get, url))
            {
                req.Headers.UserAgent.ParseAdd("Mozilla/5.0 HST-MulliganV2/2.0");
                req.Headers.Accept.ParseAdd("text/html,application/xhtml+xml");
                if (!string.IsNullOrEmpty(_sessionCookie))
                    req.Headers.TryAddWithoutValidation("Cookie", _sessionCookie);
                using (var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseContentRead, ct).ConfigureAwait(false))
                {
                    if (!resp.IsSuccessStatusCode) return MulliganDataset.Empty;
                    var html = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return ParseHtml(html, query);
                }
            }
        }

        internal static MulliganDataset ParseHtml(string html, MulliganQuery query)
        {
            if (string.IsNullOrEmpty(html)) return MulliganDataset.Empty;

            foreach (var blob in CandidateJsonBlobs(html))
            {
                MulliganDataset ds;
                try { ds = TryParseBlob(blob, query); }
                catch { continue; }
                if (ds != null && ds.Cards.Count > 0) return ds;
            }
            return MulliganDataset.Empty;
        }

        private static IEnumerable<string> CandidateJsonBlobs(string html)
        {
            var nextData = Regex.Match(html,
                "<script[^>]*id=\"__NEXT_DATA__\"[^>]*>(?<json>.*?)</script>",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (nextData.Success) yield return nextData.Groups["json"].Value;

            var initial = Regex.Match(html,
                @"window\.__INITIAL_STATE__\s*=\s*(?<json>\{.*?\});\s*</script>",
                RegexOptions.Singleline);
            if (initial.Success) yield return initial.Groups["json"].Value;

            var reactProps = Regex.Matches(html,
                "data-react-component=\"[^\"]*\"\\s+data-props=\"(?<json>[^\"]*)\"",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);
            foreach (Match m in reactProps)
                yield return HtmlDecode(m.Groups["json"].Value);
        }

        private static string HtmlDecode(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Replace("&quot;", "\"")
                    .Replace("&#34;", "\"")
                    .Replace("&amp;", "&")
                    .Replace("&#38;", "&")
                    .Replace("&lt;", "<")
                    .Replace("&gt;", ">")
                    .Replace("&#39;", "'")
                    .Replace("&apos;", "'");
        }

        private static MulliganDataset TryParseBlob(string json, MulliganQuery query)
        {
            using (var doc = JsonDocument.Parse(json))
            {
                var byDbf = new Dictionary<int, (KeepRateSample overall, Dictionary<OpponentClass, KeepRateSample> byOpp)>();
                WalkForRows(doc.RootElement, byDbf, OpponentClass.Unknown);
                if (byDbf.Count == 0) return MulliganDataset.Empty;
                var cards = new Dictionary<int, CardStats>(byDbf.Count);
                foreach (var kv in byDbf)
                {
                    var overall = kv.Value.overall.HasData
                        ? kv.Value.overall
                        : AggregateOverOpponents(kv.Value.byOpp);
                    cards[kv.Key] = new CardStats(kv.Key, overall, kv.Value.byOpp);
                }
                return new MulliganDataset(query.Format, query.RankBracket,
                    DateTimeOffset.UtcNow, 0.5, cards);
            }
        }

        private static void WalkForRows(
            JsonElement el,
            Dictionary<int, (KeepRateSample overall, Dictionary<OpponentClass, KeepRateSample> byOpp)> byDbf,
            OpponentClass currentOpponent)
        {
            switch (el.ValueKind)
            {
                case JsonValueKind.Object:
                    if (TryExtractRow(el, out var dbf, out var sample))
                    {
                        if (!byDbf.TryGetValue(dbf, out var entry))
                            byDbf[dbf] = entry = (KeepRateSample.Empty, new Dictionary<OpponentClass, KeepRateSample>());
                        if (currentOpponent == OpponentClass.Unknown)
                            byDbf[dbf] = (sample, entry.byOpp);
                        else
                            entry.byOpp[currentOpponent] = sample;
                        return;
                    }
                    foreach (var p in el.EnumerateObject())
                    {
                        var next = currentOpponent;
                        var mapped = OpponentClassExtensions.FromWireString(p.Name);
                        if (mapped != OpponentClass.Unknown) next = mapped;
                        WalkForRows(p.Value, byDbf, next);
                    }
                    break;
                case JsonValueKind.Array:
                    foreach (var item in el.EnumerateArray())
                        WalkForRows(item, byDbf, currentOpponent);
                    break;
            }
        }

        private static bool TryExtractRow(JsonElement obj, out int dbfId, out KeepRateSample sample)
        {
            dbfId = 0;
            sample = KeepRateSample.Empty;
            if (!TryReadInt(obj, "dbf_id", out dbfId) &&
                !TryReadInt(obj, "dbfId", out dbfId))
                return false;

            int total = 0;
            if (!TryReadInt(obj, "sample_size", out total) &&
                !TryReadInt(obj, "sampleSize", out total) &&
                !TryReadInt(obj, "num_games_kept", out total))
                total = 0;

            if (!TryReadDouble(obj, "kept_percent", out var keptPct) &&
                !TryReadDouble(obj, "keptPercent", out keptPct) &&
                !TryReadDouble(obj, "keep_rate", out keptPct))
                keptPct = double.NaN;

            if (!TryReadDouble(obj, "winrate_when_kept", out var keptWr) &&
                !TryReadDouble(obj, "winrateWhenKept", out keptWr) &&
                !TryReadDouble(obj, "kept_winrate", out keptWr))
                keptWr = double.NaN;

            if (total <= 0 || double.IsNaN(keptPct)) return false;
            var keptFrac = keptPct > 1.0 ? keptPct / 100.0 : keptPct;
            var kept = (int)Math.Round(total * keptFrac);
            var mull = total - kept;
            var wr = double.IsNaN(keptWr) ? double.NaN : (keptWr > 1.0 ? keptWr / 100.0 : keptWr);
            sample = new KeepRateSample(kept, mull, wr);
            return true;
        }

        private static KeepRateSample AggregateOverOpponents(Dictionary<OpponentClass, KeepRateSample> byOpp)
        {
            var agg = KeepRateSample.Empty;
            foreach (var s in byOpp.Values) agg = KeepRateSample.Combine(agg, s);
            return agg;
        }

        private static bool TryReadInt(JsonElement obj, string name, out int value)
        {
            value = 0;
            if (!obj.TryGetProperty(name, out var v)) return false;
            if (v.ValueKind == JsonValueKind.Number) { value = v.GetInt32(); return true; }
            if (v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), out var i)) { value = i; return true; }
            return false;
        }

        private static bool TryReadDouble(JsonElement obj, string name, out double value)
        {
            value = 0;
            if (!obj.TryGetProperty(name, out var v)) return false;
            if (v.ValueKind == JsonValueKind.Number) { value = v.GetDouble(); return true; }
            if (v.ValueKind == JsonValueKind.String && double.TryParse(v.GetString(),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var d))
            { value = d; return true; }
            return false;
        }
    }
}
