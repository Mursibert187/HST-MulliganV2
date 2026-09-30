using HstMulligan.Core.Models;

namespace HstMulligan.Core.Data
{
    public sealed class MulliganQuery
    {
        public FormatType Format { get; }
        public RankBracket RankBracket { get; }
        public string DeckCode { get; }

        public MulliganQuery(FormatType format, RankBracket bracket, string deckCode = null)
        {
            Format = format;
            RankBracket = bracket;
            DeckCode = deckCode;
        }

        public string CacheKey =>
            $"{Format.ToWireString()}|{RankBracket.ToWireString()}|{DeckCode ?? "no-deck"}";

        public MulliganQuery WithoutDeck() => new MulliganQuery(Format, RankBracket, null);
    }
}
