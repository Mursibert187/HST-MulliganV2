using System.Collections.Generic;
using HstMulligan.Core.Abstractions;
using HstMulligan.Core.Events;
using HstMulligan.Core.Live;
using HstMulligan.Core.Models;
using Xunit;

namespace HstMulligan.Core.Tests
{
    public class AdviceEngineTests
    {
        private static MulliganDataset Ds(int dbf, int kept, int mull, double wr = 0.55)
        {
            var stats = new CardStats(dbf, new KeepRateSample(kept, mull, wr));
            return new MulliganDataset(FormatType.Standard, RankBracket.AllRanks,
                System.DateTimeOffset.UtcNow, 0.5,
                new Dictionary<int, CardStats> { [dbf] = stats });
        }

        [Fact]
        public void EmitsAdviceOnlyWhenAllInputsPresentAndPhaseOpen()
        {
            var bus = new EventBus();
            using (new AdviceEngine(bus, new KeepRateCalculator()))
            {
                AdviceComputedEvent latest = null;
                bus.Subscribe<AdviceComputedEvent>(e => latest = e);

                var cards = new[] { new MulliganCard(10, "T", "T", 2, 0) };
                bus.Publish(new MulliganHandChangedEvent(cards, new Dictionary<int, bool>()));
                bus.Publish(new MulliganContextChangedEvent(MulliganContext.Unknown));
                bus.Publish(new DatasetLoadedEvent(Ds(10, 80, 20)));
                Assert.Null(latest); // phase not open yet

                bus.Publish(new MulliganPhaseStartedEvent());
                bus.Publish(new DatasetLoadedEvent(Ds(10, 80, 20)));
                Assert.NotNull(latest);
                Assert.Single(latest.Advices);
                Assert.Equal(0.80, latest.Advices[0].KeepRate, precision: 2);
            }
        }

        [Fact]
        public void StopsEmittingAfterPhaseEnded()
        {
            var bus = new EventBus();
            using (new AdviceEngine(bus, new KeepRateCalculator()))
            {
                int emitted = 0;
                bus.Subscribe<AdviceComputedEvent>(_ => emitted++);
                bus.Publish(new MulliganPhaseStartedEvent());
                bus.Publish(new MulliganHandChangedEvent(
                    new[] { new MulliganCard(1, "T", "T", 2, 0) },
                    new Dictionary<int, bool>()));
                bus.Publish(new MulliganContextChangedEvent(MulliganContext.Unknown));
                bus.Publish(new DatasetLoadedEvent(Ds(1, 50, 50)));
                var before = emitted;
                bus.Publish(new MulliganPhaseEndedEvent());
                bus.Publish(new DatasetLoadedEvent(Ds(1, 50, 50)));
                Assert.Equal(before, emitted);
            }
        }

        [Fact]
        public void TossFlagsFlowIntoExpectedWinrate()
        {
            var bus = new EventBus();
            using (new AdviceEngine(bus, new KeepRateCalculator()))
            {
                AdviceComputedEvent latest = null;
                bus.Subscribe<AdviceComputedEvent>(e => latest = e);
                var card = new MulliganCard(7, "T", "T", 1, 0);
                bus.Publish(new MulliganPhaseStartedEvent());
                bus.Publish(new MulliganContextChangedEvent(MulliganContext.Unknown));
                bus.Publish(new DatasetLoadedEvent(Ds(7, 160, 40, wr: 0.60)));
                bus.Publish(new MulliganHandChangedEvent(
                    new[] { card }, new Dictionary<int, bool> { [0] = false }));
                var kept = latest.ExpectedWinrate;
                bus.Publish(new MulliganHandChangedEvent(
                    new[] { card }, new Dictionary<int, bool> { [0] = true }));
                var tossed = latest.ExpectedWinrate;
                Assert.True(kept > tossed, $"kept={kept}, tossed={tossed}");
            }
        }
    }
}
