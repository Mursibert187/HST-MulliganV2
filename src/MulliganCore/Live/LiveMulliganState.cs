using System;
using System.Collections.Generic;
using System.Linq;
using HstMulligan.Core.Models;

namespace HstMulligan.Core.Live
{
    public sealed class MulliganContext : IEquatable<MulliganContext>
    {
        public FormatType Format { get; }
        public OpponentClass Opponent { get; }
        public RankBracket RankBracket { get; }
        public string DeckCode { get; }
        public string ArchetypeId { get; }
        public string ActiveDeckSignature { get; }
        public bool HasCoin { get; }
        public OpponentClass? OverrideOpponent { get; }

        public MulliganContext(
            FormatType format,
            OpponentClass opponent,
            RankBracket bracket,
            string deckCode,
            bool hasCoin,
            string archetypeId = null,
            OpponentClass? overrideOpponent = null,
            string activeDeckSignature = null)
        {
            Format = format;
            Opponent = opponent;
            RankBracket = bracket;
            DeckCode = deckCode;
            ArchetypeId = archetypeId;
            HasCoin = hasCoin;
            OverrideOpponent = overrideOpponent;
            ActiveDeckSignature = activeDeckSignature;
        }

        public OpponentClass EffectiveOpponent =>
            OverrideOpponent.HasValue ? OverrideOpponent.Value : Opponent;

        public MulliganContext WithOverrideOpponent(OpponentClass? oc) =>
            new MulliganContext(Format, Opponent, RankBracket, DeckCode, HasCoin, ArchetypeId, oc, ActiveDeckSignature);

        public static MulliganContext Unknown =>
            new MulliganContext(FormatType.Unknown, OpponentClass.Unknown, RankBracket.AllRanks, null, false);

        public bool Equals(MulliganContext other)
        {
            if (other is null) return false;
            return Format == other.Format
                && Opponent == other.Opponent
                && RankBracket == other.RankBracket
                && HasCoin == other.HasCoin
                && string.Equals(DeckCode, other.DeckCode, StringComparison.Ordinal)
                && string.Equals(ArchetypeId, other.ArchetypeId, StringComparison.Ordinal)
                && string.Equals(ActiveDeckSignature, other.ActiveDeckSignature, StringComparison.Ordinal)
                && OverrideOpponent == other.OverrideOpponent;
        }

        public override bool Equals(object obj) => Equals(obj as MulliganContext);

        public override int GetHashCode()
        {
            unchecked
            {
                int h = 17;
                h = h * 31 + (int)Format;
                h = h * 31 + (int)Opponent;
                h = h * 31 + (int)RankBracket;
                h = h * 31 + HasCoin.GetHashCode();
                h = h * 31 + (DeckCode?.GetHashCode() ?? 0);
                h = h * 31 + (ArchetypeId?.GetHashCode() ?? 0);
                h = h * 31 + (ActiveDeckSignature?.GetHashCode() ?? 0);
                h = h * 31 + (OverrideOpponent?.GetHashCode() ?? 0);
                return h;
            }
        }
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
