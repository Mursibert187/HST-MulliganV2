using System.Globalization;
using HstMulligan.Core.Models;

namespace HstMulligan.Core.Overlay
{
    public static class AdviceFormatter
    {
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
                case DecisionGrade.StrongKeep: return "keep!!";
                case DecisionGrade.Keep:       return "keep";
                case DecisionGrade.Neutral:    return "flip";
                case DecisionGrade.Toss:       return "toss";
                case DecisionGrade.StrongToss: return "toss!!";
                default:                       return "no data";
            }
        }

        public static string SampleFootnote(KeepRateSample s, bool scopedByDeck)
        {
            if (!s.HasData) return "no data";
            var basis = scopedByDeck ? "deck" : "archetype";
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
            if (double.IsNaN(expectedWinrate)) return "expected: —";
            return "expected: " + (expectedWinrate * 100.0).ToString("0.0", CultureInfo.InvariantCulture) + "%";
        }
    }
}
