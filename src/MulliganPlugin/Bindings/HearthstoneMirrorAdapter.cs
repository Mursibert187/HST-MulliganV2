using System.Collections.Generic;
using System.Linq;
using HearthDb.Enums;
using Hearthstone_Deck_Tracker;
using Hearthstone_Deck_Tracker.Hearthstone;
using HstMulligan.Core.Live;
using HstMulligan.Core.Models;

namespace HstMulligan.Plugin.Bindings
{
    /// <summary>
    /// Read-only view over HDT's live game state. Every call is defensive:
    /// HDT internals shift release-to-release, and we never want the plugin
    /// to crash HDT because a field disappeared.
    /// </summary>
    internal sealed class HearthstoneMirrorAdapter
    {
        public IReadOnlyList<MulliganCard> ReadMulliganHand()
        {
            var result = new List<MulliganCard>(4);
            try
            {
                var game = Core.Game;
                if (game?.Player == null) return result;
                var entities = game.Entities?.Values;
                if (entities == null) return result;
                var playerId = game.Player.Id;
                int slot = 0;
                foreach (var entity in entities
                    .Where(e => e != null && !string.IsNullOrEmpty(e.CardId))
                    .Where(e => e.IsControlledBy(playerId))
                    .Where(e => e.IsInHand)
                    .OrderBy(e => e.GetTag(GameTag.ZONE_POSITION)))
                {
                    var card = Database.GetCardFromId(entity.CardId);
                    if (card == null) continue;
                    result.Add(new MulliganCard(
                        card.DbfIf(),
                        entity.CardId,
                        card.LocalizedName,
                        card.Cost,
                        slot++));
                    if (result.Count >= 4) break;
                }
            }
            catch { }
            return result;
        }

        public MulliganContext ReadContext()
        {
            try
            {
                var game = Core.Game;
                var opp = OpponentClass.Unknown;
                if (game?.Opponent?.Class != null)
                    opp = OpponentClassExtensions.FromWireString(game.Opponent.Class.ToUpperInvariant());
                var format = MapFormat(game?.CurrentFormat);
                var hasCoin = game?.Player?.HasCoin ?? false;
                var deckCode = ReadActiveDeckCode();
                var bracket = ReadRankBracket(game?.MatchInfo);
                return new MulliganContext(format, opp, bracket, deckCode, hasCoin);
            }
            catch
            {
                return MulliganContext.Unknown;
            }
        }

        public IReadOnlyDictionary<int, bool> ReadTossFlags()
        {
            var result = new Dictionary<int, bool>();
            try
            {
                var game = Core.Game;
                if (game?.Player == null) return result;
                var playerId = game.Player.Id;
                int slot = 0;
                var entities = game.Entities?.Values?
                    .Where(e => e != null && !string.IsNullOrEmpty(e.CardId))
                    .Where(e => e.IsControlledBy(playerId))
                    .Where(e => e.IsInHand)
                    .OrderBy(e => e.GetTag(GameTag.ZONE_POSITION));
                if (entities == null) return result;
                foreach (var entity in entities)
                {
                    var flagged = entity.GetTag(GameTag.MULLIGAN_STATE) == (int)Mulligan.INPUT
                        && entity.GetTag(GameTag.TO_BE_DESTROYED) == 1;
                    result[slot++] = flagged;
                    if (slot >= 4) break;
                }
            }
            catch { }
            return result;
        }

        private static FormatType MapFormat(HearthDb.Enums.Format? f)
        {
            switch (f)
            {
                case HearthDb.Enums.Format.FT_STANDARD: return FormatType.Standard;
                case HearthDb.Enums.Format.FT_WILD:     return FormatType.Wild;
                case HearthDb.Enums.Format.FT_TWIST:    return FormatType.Twist;
                case HearthDb.Enums.Format.FT_CLASSIC:  return FormatType.Classic;
                default: return FormatType.Unknown;
            }
        }

        private static string ReadActiveDeckCode()
        {
            try
            {
                var deck = DeckList.Instance?.ActiveDeck;
                return deck?.DeckId.ToString();
            }
            catch { return null; }
        }

        public IReadOnlyList<int> ReadActiveDeckDbfIds()
        {
            var result = new List<int>();
            try
            {
                var deck = DeckList.Instance?.ActiveDeck;
                if (deck?.Cards == null) return result;
                foreach (var c in deck.Cards)
                {
                    var card = c.Id != null ? Database.GetCardFromId(c.Id) : null;
                    if (card == null) continue;
                    var dbf = card.DbfIf();
                    if (dbf <= 0) continue;
                    for (int i = 0; i < c.Count; i++) result.Add(dbf);
                }
            }
            catch { }
            return result;
        }

        public string ReadActiveDeckSignature()
        {
            try
            {
                var dbfIds = ReadActiveDeckDbfIds();
                return dbfIds.Count == 0 ? null : HstMulligan.Core.Abstractions.Signature.Compute(dbfIds);
            }
            catch { return null; }
        }

        public OpponentClass ReadActiveDeckHeroClass()
        {
            try
            {
                var deck = DeckList.Instance?.ActiveDeck;
                if (deck?.Class == null) return OpponentClass.Unknown;
                return OpponentClassExtensions.FromWireString(deck.Class.ToUpperInvariant());
            }
            catch { return OpponentClass.Unknown; }
        }

        private static RankBracket ReadRankBracket(dynamic matchInfo)
        {
            try
            {
                if (matchInfo == null) return RankBracket.AllRanks;
                int? star = matchInfo.LocalPlayer?.StarLevel;
                int? legend = matchInfo.LocalPlayer?.LegendRank;
                return RankBracketExtensions.FromStars(star, legend);
            }
            catch { return RankBracket.AllRanks; }
        }
    }

    internal static class CardCompatExt
    {
        // HearthDb's Card DbfId has changed name across versions; keep a
        // single access point that survives either spelling.
        public static int DbfIf(this HearthDb.CardDefs.Card card)
        {
            try { return card.DbfId; } catch { }
            try { return (int)card.GetType().GetProperty("DbfId").GetValue(card); } catch { }
            return 0;
        }
    }
}
