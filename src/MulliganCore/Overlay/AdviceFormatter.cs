using System.Globalization;
using HstMulligan.Core.Localization;
using HstMulligan.Core.Models;

namespace HstMulligan.Core.Overlay
{
    public static class AdviceFormatter
    {
        private static string T(string key, string fallback) =>
            Localization.Localization.Current.T(key, fallback);

        public static string PercentLabel(double keepRate)
        {
            if (double.IsNaN(keepRate)) return "—";
            var pct = keepRate * 100.0;
            return pct.ToString("0", CultureInfo.InvariantCulture);
        }

        public static string LiftLabel(double lift)
        {
            var pct = lift * 100.0;
            if (pct >= 0)
                return "+" + pct.ToString("0.0", CultureInfo.InvariantCulture) + " wr";
            return pct.ToString("0.0", CultureInfo.InvariantCulture) + " wr";
        }

        public static string GradeLabel(DecisionGrade grade)
        {
            switch (grade)
            {
                case DecisionGrade.StrongKeep: return T("grade.strong_keep", "keep!!");
                case DecisionGrade.Keep:       return T("grade.keep",        "keep");
                case DecisionGrade.Neutral:    return T("grade.neutral",     "flip");
                case DecisionGrade.Toss:       return T("grade.toss",        "toss");
                case DecisionGrade.StrongToss: return T("grade.strong_toss", "toss!!");
                default:                       return T("grade.unknown",     "no data");
            }
        }

        public static string SampleFootnote(KeepRateSample s, bool scopedByDeck)
        {
            if (!s.HasData) return T("sample.none", "no data");
            var basis = scopedByDeck ? T("scope.deck", "deck") : T("scope.archetype", "archetype");
            return string.Format(CultureInfo.InvariantCulture,
                "n={0} · {1}", s.Total, basis);
        }

        public static string LowSampleGlyph(double confidence)
        {
            if (confidence < 0.25) return "?";
            if (confidence < 0.45) return "!";
            return "";
        }

        public static string ExpectedWinrateLabel(double expectedWinrate)
        {
            var prefix = T("expected.prefix", "expected: ");
            if (double.IsNaN(expectedWinrate)) return prefix + "—";
            return prefix + (expectedWinrate * 100.0).ToString("0.0", CultureInfo.InvariantCulture) + "%";
        }

        public static string KeptWinrateLabel(double keptWinrate)
        {
            if (double.IsNaN(keptWinrate)) return T("kept_wr.na", "kept —");
            return T("kept_wr.prefix", "kept ")
                + (keptWinrate * 100.0).ToString("0.0", CultureInfo.InvariantCulture) + "%";
        }

        public static string DrawnWinrateLabel(double drawnWinrate)
        {
            if (double.IsNaN(drawnWinrate)) return T("drawn_wr.na", "drawn —");
            return T("drawn_wr.prefix", "drawn ")
                + (drawnWinrate * 100.0).ToString("0.0", CultureInfo.InvariantCulture) + "%";
        }

        public static string CoinStateLabel(bool hasCoin, bool scopedByCoinState)
        {
            if (!scopedByCoinState) return "";
            return hasCoin ? T("coin.on_coin", "on coin") : T("coin.on_play", "on play");
        }
    }
}
