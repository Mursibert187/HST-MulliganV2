using HstMulligan.Core.Models;

namespace HstMulligan.Core.Data
{
    public sealed class MulliganQuery
    {
        public FormatType Format { get; }
        public RankBracket RankBracket { get; }
        public string DeckCode { get; }
        public string ArchetypeId { get; }
        public string DeckShortId { get; }

        public MulliganQuery(
            FormatType format,
            RankBracket bracket,
            string deckCode = null,
            string archetypeId = null,
            string deckShortId = null)
        {
            Format = format;
            RankBracket = bracket;
            DeckCode = deckCode;
            ArchetypeId = archetypeId;
            DeckShortId = deckShortId;
        }

        public string CacheKey =>
            $"{Format.ToWireString()}|{RankBracket.ToWireString()}|{DeckCode ?? "-"}|{ArchetypeId ?? "-"}|{DeckShortId ?? "-"}";

        public MulliganQuery WithoutDeck() =>
            new MulliganQuery(Format, RankBracket, null, ArchetypeId, DeckShortId);

        public MulliganQuery WithDeckShortId(string shortId) =>
            new MulliganQuery(Format, RankBracket, DeckCode, ArchetypeId, shortId);
    }
}
