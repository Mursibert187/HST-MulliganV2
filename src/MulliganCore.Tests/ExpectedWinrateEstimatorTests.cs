using System.Collections.Generic;
using HstMulligan.Core.Live;
using HstMulligan.Core.Models;
using Xunit;

namespace HstMulligan.Core.Tests
{
    public class ExpectedWinrateEstimatorTests
    {
        private static MulliganAdvice Advice(int slot, double keepRate, double keptWinrate, double confidence)
        {
            var card = new MulliganCard(100 + slot, "T", "T", 2, slot);
            var kept = (int)(400 * keepRate);
            var mull = 400 - kept;
            return new MulliganAdvice(
                card,
                new KeepRateSample(kept, mull, keptWinrate),
                keepRate,
                keptWinrate - 0.5,
                confidence,
                DecisionGrade.Keep,
                OpponentClass.Mage,
                scopedByDeck: false);
        }

        [Fact]
        public void NoAdvicesYieldsNaN()
        {
            var est = new ExpectedWinrateEstimator();
            Assert.True(double.IsNaN(est.Estimate(new MulliganAdvice[0], new Dictionary<int, bool>(), 0.5)));
        }

        [Fact]
        public void AllKeptPullsTowardKeptWinrate()
        {
            var est = new ExpectedWinrateEstimator();
            var advices = new[]
            {
                Advice(0, 0.80, 0.60, confidence: 1.0),
                Advice(1, 0.70, 0.58, confidence: 1.0),
            };
            var toss = new Dictionary<int, bool> { [0] = false, [1] = false };
            var ewr = est.Estimate(advices, toss, baseWinrate: 0.50);
            Assert.InRange(ewr, 0.58, 0.60);
        }

        [Fact]
        public void TossedSlotsPullTowardBaseWinrate()
        {
            var est = new ExpectedWinrateEstimator();
            var advices = new[]
            {
                Advice(0, 0.80, 0.60, confidence: 1.0),
                Advice(1, 0.70, 0.58, confidence: 1.0),
            };
            var toss = new Dictionary<int, bool> { [0] = true, [1] = true };
            var ewr = est.Estimate(advices, toss, baseWinrate: 0.50);
            Assert.Equal(0.50, ewr, precision: 3);
        }

        [Fact]
        public void LowConfidenceSoftensInfluence()
        {
            var est = new ExpectedWinrateEstimator();
            var strong = est.Estimate(new[] { Advice(0, 0.80, 0.70, confidence: 1.0) },
                new Dictionary<int, bool> { [0] = false }, baseWinrate: 0.50);
            var weak = est.Estimate(new[] { Advice(0, 0.80, 0.70, confidence: 0.0) },
                new Dictionary<int, bool> { [0] = false }, baseWinrate: 0.50);
            Assert.True(strong > weak + 0.1, $"strong={strong} weak={weak}");
        }
    }
}
