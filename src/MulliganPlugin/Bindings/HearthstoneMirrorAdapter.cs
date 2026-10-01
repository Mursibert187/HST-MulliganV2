using System.Collections.Generic;
using System.Linq;
using HearthDb.Enums;
using Hearthstone_Deck_Tracker;
using Hearthstone_Deck_Tracker.Hearthstone;
using HstMulligan.Core.Live;
using HstMulligan.Core.Models;
using HdtCore = Hearthstone_Deck_Tracker.API.Core;

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
                var game = HdtCore.Game;
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
                        card.DbfId,
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
                var game = HdtCore.Game;
                var opp = HstMulligan.Core.Models.OpponentClass.Unknown;
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
                var game = HdtCore.Game;
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

        private static HstMulligan.Core.Models.FormatType MapFormat(HearthDb.Enums.FormatType? f)
        {
            switch (f)
            {
                case HearthDb.Enums.FormatType.FT_STANDARD: return HstMulligan.Core.Models.FormatType.Standard;
                case HearthDb.Enums.FormatType.FT_WILD:     return HstMulligan.Core.Models.FormatType.Wild;
                case HearthDb.Enums.FormatType.FT_TWIST:    return HstMulligan.Core.Models.FormatType.Twist;
                case HearthDb.Enums.FormatType.FT_CLASSIC:  return HstMulligan.Core.Models.FormatType.Classic;
                default: return HstMulligan.Core.Models.FormatType.Unknown;
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
                    var dbf = c.DbfId;
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

        public HstMulligan.Core.Models.OpponentClass ReadActiveDeckHeroClass()
        {
            try
            {
                var deck = DeckList.Instance?.ActiveDeck;
                if (deck?.Class == null) return HstMulligan.Core.Models.OpponentClass.Unknown;
                return OpponentClassExtensions.FromWireString(deck.Class.ToUpperInvariant());
            }
            catch { return HstMulligan.Core.Models.OpponentClass.Unknown; }
        }

        private static RankBracket ReadRankBracket(HearthMirror.Objects.MatchInfo matchInfo)
        {
            try
            {
                if (matchInfo?.LocalPlayer == null) return RankBracket.AllRanks;
                int? star = matchInfo.LocalPlayer.StarLevel;
                int? legend = matchInfo.LocalPlayer.LegendRank;
                return RankBracketExtensions.FromStars(star, legend);
            }
            catch { return RankBracket.AllRanks; }
        }
    }
}
