using HstMulligan.Core.Models;

namespace HstMulligan.Core.Data
{
    public sealed class MulliganQuery
    {
        public FormatType Format { get; }
        public RankBracket RankBracket { get; }
        public string DeckCode { get; }
        public string ArchetypeId { get; }

        public MulliganQuery(FormatType format, RankBracket bracket, string deckCode = null, string archetypeId = null)
        {
            Format = format;
            RankBracket = bracket;
            DeckCode = deckCode;
            ArchetypeId = archetypeId;
        }

        public string CacheKey =>
            $"{Format.ToWireString()}|{RankBracket.ToWireString()}|{DeckCode ?? "-"}|{ArchetypeId ?? "-"}";

        public MulliganQuery WithoutDeck() =>
            new MulliganQuery(Format, RankBracket, null, ArchetypeId);
    }
}
