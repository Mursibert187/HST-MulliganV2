using System.Collections.Generic;
using HstMulligan.Core.Models;

namespace HstMulligan.Core.Live
{
    public sealed class ExpectedWinrateEstimator
    {
        /// <summary>
        /// Blends the kept-cards' per-card kept-winrate with the dataset base
        /// winrate, weighted by each card's confidence. Returns NaN when no
        /// scored sample is available.
        /// </summary>
        public double Estimate(
            IEnumerable<MulliganAdvice> advices,
            IReadOnlyDictionary<int, bool> tossedBySlot,
            double baseWinrate)
        {
            if (advices == null) return double.NaN;
            double weightedSum = 0.0;
            double weightTotal = 0.0;
            int cardsCount = 0;
            foreach (var a in advices)
            {
                cardsCount++;
                bool tossed = tossedBySlot != null
                    && tossedBySlot.TryGetValue(a.Card.SlotIndex, out var t) && t;
                double w = ConfidenceScorer.Clamp01(a.Confidence);
                double v = tossed
                    ? baseWinrate
                    : (!double.IsNaN(a.Sample.KeptWinrate) ? a.Sample.KeptWinrate : baseWinrate);
                weightedSum += v * (w + 0.001);
                weightTotal += (w + 0.001);
            }
            if (cardsCount == 0 || weightTotal <= 0) return double.NaN;
            return weightedSum / weightTotal;
        }
    }
}
