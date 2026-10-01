using System.Collections.Generic;
using HstMulligan.Core.Live;
using HstMulligan.Core.Models;

namespace HstMulligan.Core.Events
{
    public sealed class MulliganPhaseStartedEvent { }

    public sealed class MulliganPhaseEndedEvent { }

    public sealed class MulliganHandChangedEvent
    {
        public IReadOnlyList<MulliganCard> Cards { get; }
        public IReadOnlyDictionary<int, bool> TossFlags { get; }
        public MulliganHandChangedEvent(IReadOnlyList<MulliganCard> cards, IReadOnlyDictionary<int, bool> toss)
        {
            Cards = cards;
            TossFlags = toss;
        }
    }

    public sealed class MulliganContextChangedEvent
    {
        public MulliganContext Context { get; }
        public MulliganContextChangedEvent(MulliganContext context) { Context = context; }
    }

    public sealed class DatasetLoadedEvent
    {
        public MulliganDataset Dataset { get; }
        public DatasetLoadedEvent(MulliganDataset dataset) { Dataset = dataset; }
    }

    public sealed class AdviceComputedEvent
    {
        public IReadOnlyList<MulliganAdvice> Advices { get; }
        public IReadOnlyDictionary<int, bool> TossFlags { get; }
        public MulliganContext Context { get; }
        public MulliganDataset Dataset { get; }
        public double ExpectedWinrate { get; }

        public AdviceComputedEvent(
            IReadOnlyList<MulliganAdvice> advices,
            IReadOnlyDictionary<int, bool> tossFlags,
            MulliganContext context,
            MulliganDataset dataset,
            double expectedWinrate)
        {
            Advices = advices;
            TossFlags = tossFlags;
            Context = context;
            Dataset = dataset;
            ExpectedWinrate = expectedWinrate;
        }
    }

    public sealed class SettingsChangedEvent { }
}
