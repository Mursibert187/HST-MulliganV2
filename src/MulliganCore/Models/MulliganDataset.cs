using System;
using System.Collections.Generic;
using System.Linq;

namespace HstMulligan.Core.Models
{
    public sealed class CardStats
    {
        public int DbfId { get; }
        public KeepRateSample Overall { get; }
        public IReadOnlyDictionary<OpponentClass, KeepRateSample> ByOpponent { get; }
        public IReadOnlyDictionary<string, KeepRateSample> ByDeck { get; }
        public IReadOnlyDictionary<string, KeepRateSample> ByArchetype { get; }

        public CardStats(
            int dbfId,
            KeepRateSample overall,
            IReadOnlyDictionary<OpponentClass, KeepRateSample> byOpponent = null,
            IReadOnlyDictionary<string, KeepRateSample> byDeck = null,
            IReadOnlyDictionary<string, KeepRateSample> byArchetype = null)
        {
            DbfId = dbfId;
            Overall = overall;
            ByOpponent = byOpponent ?? new Dictionary<OpponentClass, KeepRateSample>();
            ByDeck = byDeck ?? new Dictionary<string, KeepRateSample>();
            ByArchetype = byArchetype ?? new Dictionary<string, KeepRateSample>(StringComparer.Ordinal);
        }

        /// <summary>
        /// Resolution order: deck code (narrowest) → archetype slug →
        /// opponent class → overall. The first bucket that holds data wins.
        /// </summary>
        public KeepRateSample Resolve(OpponentClass opponent, string deckCode, string archetypeId = null)
        {
            if (!string.IsNullOrEmpty(deckCode) && ByDeck.TryGetValue(deckCode, out var d) && d.HasData)
                return d;
            if (!string.IsNullOrEmpty(archetypeId) && ByArchetype.TryGetValue(archetypeId, out var a) && a.HasData)
                return a;
            if (opponent != OpponentClass.Unknown && ByOpponent.TryGetValue(opponent, out var o) && o.HasData)
                return o;
            return Overall;
        }
    }

    public sealed class MulliganDataset
    {
        public FormatType Format { get; }
        public RankBracket RankBracket { get; }
        public DateTimeOffset GeneratedAt { get; }
        public double BaseWinrate { get; }
        public IReadOnlyDictionary<int, CardStats> Cards { get; }

        public MulliganDataset(
            FormatType format,
            RankBracket bracket,
            DateTimeOffset generatedAt,
            double baseWinrate,
            IReadOnlyDictionary<int, CardStats> cards)
        {
            Format = format;
            RankBracket = bracket;
            GeneratedAt = generatedAt;
            BaseWinrate = baseWinrate;
            Cards = cards ?? new Dictionary<int, CardStats>();
        }

        public bool TryGet(int dbfId, out CardStats stats) => Cards.TryGetValue(dbfId, out stats);

        public static readonly MulliganDataset Empty = new MulliganDataset(
            FormatType.Unknown, RankBracket.AllRanks, DateTimeOffset.MinValue, 0.5,
            new Dictionary<int, CardStats>());

        public MulliganDataset MergeUnder(MulliganDataset fallback)
        {
            if (fallback == null || fallback.Cards.Count == 0) return this;
            var merged = Cards.ToDictionary(kv => kv.Key, kv => kv.Value);
            foreach (var kv in fallback.Cards)
                if (!merged.ContainsKey(kv.Key)) merged[kv.Key] = kv.Value;
            return new MulliganDataset(Format, RankBracket, GeneratedAt, BaseWinrate, merged);
        }
    }
}
