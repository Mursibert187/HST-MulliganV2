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
    /// Reads the Firestone-shaped mulligan-guide JSON. The URL template is
    /// configurable; the envelope shape is documented in docs/SPEC.md.
    /// </summary>
    public sealed class FirestoneDataSource : IMulliganDataSource
    {
        public string Name => "firestone";

        private readonly HttpClient _http;
        private readonly string _urlTemplate;

        public FirestoneDataSource(HttpClient http, string urlTemplate)
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
            var format = query.Format;
            if (root.TryGetProperty("format", out var f) && f.ValueKind == JsonValueKind.String)
                format = FormatTypeExtensions.FromWireString(f.GetString());

            var bracket = query.RankBracket;
            if (root.TryGetProperty("rankBracket", out var b) && b.ValueKind == JsonValueKind.String)
                bracket = RankBracketExtensions.FromWireString(b.GetString());

            var generated = DateTimeOffset.UtcNow;
            if (root.TryGetProperty("generatedAt", out var g) && g.ValueKind == JsonValueKind.String &&
                DateTimeOffset.TryParse(g.GetString(), out var parsed))
                generated = parsed;

            double baseWr = 0.5;
            if (root.TryGetProperty("baseWinrate", out var bw) && bw.ValueKind == JsonValueKind.Number)
                baseWr = bw.GetDouble();

            var cards = new Dictionary<int, CardStats>();
            if (root.TryGetProperty("cards", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in arr.EnumerateArray())
                {
                    if (!el.TryGetProperty("dbfId", out var dEl)) continue;
                    var dbf = dEl.GetInt32();
                    var overall = ReadSample(el, "overall");
                    var byOpp = new Dictionary<OpponentClass, KeepRateSample>();
                    if (el.TryGetProperty("byOpponent", out var oppMap) && oppMap.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var p in oppMap.EnumerateObject())
                        {
                            var oc = OpponentClassExtensions.FromWireString(p.Name);
                            if (oc == OpponentClass.Unknown) continue;
                            byOpp[oc] = ReadSampleValue(p.Value);
                        }
                    }
                    var byDeck = new Dictionary<string, KeepRateSample>(StringComparer.Ordinal);
                    if (el.TryGetProperty("byDeck", out var deckMap) && deckMap.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var p in deckMap.EnumerateObject())
                            byDeck[p.Name] = ReadSampleValue(p.Value);
                    }
                    var byArch = new Dictionary<string, KeepRateSample>(StringComparer.Ordinal);
                    if (el.TryGetProperty("byArchetype", out var archMap) && archMap.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var p in archMap.EnumerateObject())
                            byArch[p.Name] = ReadSampleValue(p.Value);
                    }
                    KeepRateSample? onPlay = null, onCoin = null;
                    if (el.TryGetProperty("onPlay", out var opEl)) onPlay = ReadSampleValue(opEl);
                    if (el.TryGetProperty("onCoin", out var ocEl)) onCoin = ReadSampleValue(ocEl);
                    if (el.TryGetProperty("byCoinState", out var coinMap) && coinMap.ValueKind == JsonValueKind.Object)
                    {
                        if (coinMap.TryGetProperty("play", out var pEl)) onPlay = ReadSampleValue(pEl);
                        if (coinMap.TryGetProperty("coin", out var cEl)) onCoin = ReadSampleValue(cEl);
                    }
                    cards[dbf] = new CardStats(dbf, overall, byOpp, byDeck, byArch, onPlay, onCoin);
                }
            }

            return new MulliganDataset(format, bracket, generated, baseWr, cards);
        }

        private static KeepRateSample ReadSample(JsonElement parent, string prop)
        {
            if (parent.TryGetProperty(prop, out var child)) return ReadSampleValue(child);
            return KeepRateSample.Empty;
        }

        private static KeepRateSample ReadSampleValue(JsonElement el)
        {
            if (el.ValueKind != JsonValueKind.Object) return KeepRateSample.Empty;
            int kept = ReadInt(el, "kept");
            int mull = ReadInt(el, "mulliganed");
            double kwr = ReadDouble(el, "keptWinrate", double.NaN);
            double dwr = ReadDouble(el, "drawnWinrate", double.NaN);
            return new KeepRateSample(kept, mull, kwr, dwr);
        }

        private static int ReadInt(JsonElement el, string name)
        {
            if (el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number) return v.GetInt32();
            return 0;
        }

        private static double ReadDouble(JsonElement el, string name, double dflt)
        {
            if (el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number) return v.GetDouble();
            return dflt;
        }
    }
}
