using System;
using System.Collections.Generic;
using HstMulligan.Core.Abstractions;
using HstMulligan.Core.Events;
using HstMulligan.Core.Models;

namespace HstMulligan.Core.Live
{
    /// <summary>
    /// Reactive glue between hand/context/dataset changes and the overlay.
    /// Subscribes to the event bus, recomputes per-card advice whenever any
    /// input moves, and republishes an <see cref="AdviceComputedEvent"/>.
    /// </summary>
    public sealed class AdviceEngine : IDisposable
    {
        private readonly IEventBus _bus;
        private readonly KeepRateCalculator _calc;
        private readonly ExpectedWinrateEstimator _estimator;
        private readonly ILogger _log;
        private readonly List<IDisposable> _subs = new List<IDisposable>();

        private IReadOnlyList<MulliganCard> _cards;
        private IReadOnlyDictionary<int, bool> _tossFlags;
        private MulliganContext _context;
        private MulliganDataset _dataset;
        private bool _phaseOpen;

        public AdviceEngine(
            IEventBus bus,
            KeepRateCalculator calc,
            ExpectedWinrateEstimator estimator = null,
            ILogger log = null)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _calc = calc ?? throw new ArgumentNullException(nameof(calc));
            _estimator = estimator ?? new ExpectedWinrateEstimator();
            _log = log ?? NullLogger.Instance;
            _subs.Add(_bus.Subscribe<MulliganPhaseStartedEvent>(OnPhaseStarted));
            _subs.Add(_bus.Subscribe<MulliganPhaseEndedEvent>(OnPhaseEnded));
            _subs.Add(_bus.Subscribe<MulliganHandChangedEvent>(OnHand));
            _subs.Add(_bus.Subscribe<MulliganContextChangedEvent>(OnContext));
            _subs.Add(_bus.Subscribe<DatasetLoadedEvent>(OnDataset));
        }

        public void Dispose()
        {
            foreach (var s in _subs) s.Dispose();
            _subs.Clear();
        }

        private void OnPhaseStarted(MulliganPhaseStartedEvent _)
        {
            _phaseOpen = true;
            _log.Debug("mulligan phase opened");
        }

        private void OnPhaseEnded(MulliganPhaseEndedEvent _)
        {
            _phaseOpen = false;
            _cards = null;
            _tossFlags = null;
            _log.Debug("mulligan phase closed");
        }

        private void OnHand(MulliganHandChangedEvent evt)
        {
            _cards = evt.Cards;
            _tossFlags = evt.TossFlags;
            Recompute();
        }

        private void OnContext(MulliganContextChangedEvent evt)
        {
            _context = evt.Context;
            Recompute();
        }

        private void OnDataset(DatasetLoadedEvent evt)
        {
            _dataset = evt.Dataset ?? MulliganDataset.Empty;
            Recompute();
        }

        private void Recompute()
        {
            if (!_phaseOpen || _cards == null || _context == null) return;
            var ds = _dataset ?? MulliganDataset.Empty;
            var advices = new List<MulliganAdvice>(_cards.Count);
            foreach (var card in _cards)
                advices.Add(_calc.Advise(card, ds, _context));
            var ewr = _estimator.Estimate(advices, _tossFlags, ds.BaseWinrate);
            _bus.Publish(new AdviceComputedEvent(advices, _tossFlags, _context, ds, ewr));
        }
    }
}
