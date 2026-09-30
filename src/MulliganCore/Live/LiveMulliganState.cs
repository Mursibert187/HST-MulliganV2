using System;
using System.Collections.Generic;
using System.Linq;
using HstMulligan.Core.Models;

namespace HstMulligan.Core.Live
{
    public sealed class MulliganContext
    {
        public FormatType Format { get; }
        public OpponentClass Opponent { get; }
        public RankBracket RankBracket { get; }
        public string DeckCode { get; }
        public bool HasCoin { get; }

        public MulliganContext(FormatType format, OpponentClass opponent, RankBracket bracket, string deckCode, bool hasCoin)
        {
            Format = format;
            Opponent = opponent;
            RankBracket = bracket;
            DeckCode = deckCode;
            HasCoin = hasCoin;
        }

        public static MulliganContext Unknown =>
            new MulliganContext(FormatType.Unknown, OpponentClass.Unknown, RankBracket.AllRanks, null, false);
    }

    public sealed class LiveMulliganState
    {
        private readonly Dictionary<int, MulliganCard> _cards = new Dictionary<int, MulliganCard>();
        private readonly HashSet<int> _tossed = new HashSet<int>();

        public MulliganContext Context { get; private set; } = MulliganContext.Unknown;

        public event EventHandler Changed;

        public IReadOnlyList<MulliganCard> Cards => _cards.Values.OrderBy(c => c.SlotIndex).ToList();
        public bool IsTossed(MulliganCard card) => _tossed.Contains(card.SlotIndex);

        public void SetContext(MulliganContext ctx)
        {
            Context = ctx ?? MulliganContext.Unknown;
            Raise();
        }

        public void SetCards(IEnumerable<MulliganCard> cards)
        {
            _cards.Clear();
            _tossed.Clear();
            if (cards != null)
                foreach (var c in cards) _cards[c.SlotIndex] = c;
            Raise();
        }

        public void ToggleToss(int slotIndex)
        {
            if (!_cards.ContainsKey(slotIndex)) return;
            if (!_tossed.Add(slotIndex)) _tossed.Remove(slotIndex);
            Raise();
        }

        public void Clear()
        {
            _cards.Clear();
            _tossed.Clear();
            Context = MulliganContext.Unknown;
            Raise();
        }

        private void Raise() => Changed?.Invoke(this, EventArgs.Empty);
    }
}
