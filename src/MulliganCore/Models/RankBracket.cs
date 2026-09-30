using System;

namespace HstMulligan.Core.Models
{
    public enum RankBracket
    {
        Unknown = 0,
        BronzeSilverGold,
        PlatinumDiamond,
        Legend,
        AllRanks,
        TopLegend,
    }

    public static class RankBracketExtensions
    {
        public static string ToWireString(this RankBracket r)
        {
            switch (r)
            {
                case RankBracket.BronzeSilverGold: return "bronze-silver-gold";
                case RankBracket.PlatinumDiamond:  return "platinum-diamond";
                case RankBracket.Legend:           return "legend";
                case RankBracket.TopLegend:        return "top-legend";
                case RankBracket.AllRanks:         return "all-ranks";
                default:                           return "unknown";
            }
        }

        public static RankBracket FromWireString(string s)
        {
            if (string.IsNullOrEmpty(s)) return RankBracket.Unknown;
            switch (s.Trim().ToLowerInvariant())
            {
                case "bronze-silver-gold":
                case "bronze_silver_gold": return RankBracket.BronzeSilverGold;
                case "platinum-diamond":
                case "platinum_diamond":   return RankBracket.PlatinumDiamond;
                case "legend":             return RankBracket.Legend;
                case "top-legend":
                case "top_legend":         return RankBracket.TopLegend;
                case "all-ranks":
                case "all":                return RankBracket.AllRanks;
                default:                   return RankBracket.Unknown;
            }
        }

        public static RankBracket FromStars(int? starLevel, int? legendRank)
        {
            if (legendRank.HasValue && legendRank.Value > 0)
                return legendRank.Value <= 1000 ? RankBracket.TopLegend : RankBracket.Legend;
            if (!starLevel.HasValue) return RankBracket.AllRanks;
            var s = starLevel.Value;
            if (s <= 30) return RankBracket.BronzeSilverGold;
            if (s <= 50) return RankBracket.PlatinumDiamond;
            return RankBracket.Legend;
        }
    }
}
