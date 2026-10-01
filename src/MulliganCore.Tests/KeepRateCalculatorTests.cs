using System.Collections.Generic;
using HstMulligan.Core.Live;
using HstMulligan.Core.Models;
using Xunit;

namespace HstMulligan.Core.Tests
{
    public class KeepRateCalculatorTests
    {
        private static MulliganDataset Build(int dbf, KeepRateSample overall,
            IReadOnlyDictionary<OpponentClass, KeepRateSample> byOpp = null,
            IReadOnlyDictionary<string, KeepRateSample> byDeck = null,
            double baseWinrate = 0.5)
        {
            var card = new CardStats(dbf, overall, byOpp, byDeck);
            var cards = new Dictionary<int, CardStats> { [dbf] = card };
            return new MulliganDataset(FormatType.Standard, RankBracket.AllRanks,
                System.DateTimeOffset.UtcNow, baseWinrate, cards);
        }

        private static MulliganCard Card(int dbf = 12345) => new MulliganCard(dbf, "TST_001", "Testcard", 3, 0);

        [Theory]
        [InlineData(0.90, 0.02, DecisionGrade.StrongKeep)]
        [InlineData(0.66, 0.05, DecisionGrade.StrongKeep)] // promoted by lift
        [InlineData(0.60, 0.00, DecisionGrade.Keep)]
        [InlineData(0.48, 0.02, DecisionGrade.Keep)] // weak keep by lift
        [InlineData(0.50, 0.00, DecisionGrade.Neutral)]
        [InlineData(0.40, -0.01, DecisionGrade.Toss)]
        [InlineData(0.30, -0.04, DecisionGrade.StrongToss)]
        [InlineData(0.10, 0.00, DecisionGrade.StrongToss)]
        public void GradeBucketsMatchSpec(double keepRate, double lift, DecisionGrade expected)
        {
            var calc = new KeepRateCalculator();
            Assert.Equal(expected, calc.BucketGrade(keepRate, lift));
        }

        [Fact]
        public void MissingCardYieldsUnknownGrade()
        {
            var calc = new KeepRateCalculator();
            var ds = MulliganDataset.Empty;
            var advice = calc.Advise(Card(), ds, MulliganContext.Unknown);
            Assert.Equal(DecisionGrade.Unknown, advice.Grade);
            Assert.False(advice.HasData);
        }

        [Fact]
        public void DeckScopeBeatsOpponentScope()
        {
            var byOpp = new Dictionary<OpponentClass, KeepRateSample>
            {
                [OpponentClass.Mage] = new KeepRateSample(30, 70, 0.42),
            };
            var byDeck = new Dictionary<string, KeepRateSample>
            {
                ["DECK1"] = new KeepRateSample(180, 20, 0.60),
            };
            var ds = Build(42,
                overall: new KeepRateSample(500, 500, 0.50),
                byOpp: byOpp,
                byDeck: byDeck);
            var calc = new KeepRateCalculator();
            var ctx = new MulliganContext(FormatType.Standard, OpponentClass.Mage,
                RankBracket.AllRanks, "DECK1", hasCoin: false);
            var advice = calc.Advise(new MulliganCard(42, "T", "T", 2, 0), ds, ctx);
            Assert.True(advice.ScopedByDeck);
            Assert.Equal(200, advice.Sample.Total);
            Assert.Equal(DecisionGrade.StrongKeep, advice.Grade);
        }

        [Fact]
        public void CoinStateBucketWinsOverOpponentBucket()
        {
            var byOpp = new Dictionary<OpponentClass, KeepRateSample>
            {
                [OpponentClass.Mage] = new KeepRateSample(30, 70, 0.42),
            };
            var onCoin = new KeepRateSample(180, 20, 0.60, 0.50);
            var stats = new CardStats(99, new KeepRateSample(500, 500, 0.50), byOpp,
                byDeck: null, byArchetype: null,
                onPlay: new KeepRateSample(80, 120, 0.47),
                onCoin: onCoin);
            var ctx = new MulliganContext(FormatType.Standard, OpponentClass.Mage,
                RankBracket.AllRanks, deckCode: null, hasCoin: true);
            var ds = new MulliganDataset(FormatType.Standard, RankBracket.AllRanks,
                System.DateTimeOffset.UtcNow, 0.5,
                new Dictionary<int, CardStats> { [99] = stats });
            var advice = new KeepRateCalculator().Advise(new MulliganCard(99, "T", "T", 2, 0), ds, ctx);
            Assert.True(advice.ScopedByCoinState);
            Assert.Equal(200, advice.Sample.Total);
            Assert.Equal(DecisionGrade.StrongKeep, advice.Grade);
        }

        [Fact]
        public void DrawnWinrateDrivesImpactWhenPresent()
        {
            var sample = new KeepRateSample(100, 100, keptWinrate: 0.55, drawnWinrate: 0.40);
            var stats = new CardStats(5, sample);
            var ds = new MulliganDataset(FormatType.Standard, RankBracket.AllRanks,
                System.DateTimeOffset.UtcNow, 0.52,
                new Dictionary<int, CardStats> { [5] = stats });
            var advice = new KeepRateCalculator().Advise(
                new MulliganCard(5, "T", "T", 2, 0), ds, MulliganContext.Unknown);
            Assert.Equal(0.15, advice.Lift, precision: 4);
            Assert.Equal(0.40, advice.DrawnWinrate, precision: 4);
            Assert.Equal(0.15, advice.KeepVsDrawDelta, precision: 4);
        }

        [Fact]
        public void FallsBackFromDeckToOpponentToOverall()
        {
            var byOpp = new Dictionary<OpponentClass, KeepRateSample>
            {
                [OpponentClass.Mage] = new KeepRateSample(150, 50, 0.55),
            };
            var ds = Build(7,
                overall: new KeepRateSample(1000, 1000, 0.50),
                byOpp: byOpp);
            var calc = new KeepRateCalculator();
            var ctx = new MulliganContext(FormatType.Standard, OpponentClass.Mage,
                RankBracket.AllRanks, deckCode: "NOPE", hasCoin: false);
            var advice = calc.Advise(new MulliganCard(7, "T", "T", 2, 0), ds, ctx);
            Assert.False(advice.ScopedByDeck);
            Assert.Equal(200, advice.Sample.Total); // opponent-scoped
        }
    }
}
