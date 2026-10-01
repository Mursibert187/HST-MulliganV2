using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Hearthstone_Deck_Tracker.API;
using HstMulligan.Core.Abstractions;
using HstMulligan.Core.Data;
using HstMulligan.Core.Events;
using HstMulligan.Core.Live;
using HstMulligan.Core.Models;
using HstMulligan.Plugin.Config;

namespace HstMulligan.Plugin.Bindings
{
    /// <summary>
    /// Translates HDT game events + per-tick mirror reads into typed bus
    /// events. Owns nothing beyond that: the engine, overlay and details
    /// panel all live further downstream and never see HDT internals.
    /// </summary>
    internal sealed class GameEventBridge
    {
        private readonly PluginSettings _settings;
        private readonly IGameClient _client;
        private readonly IEventBus _bus;
        private readonly ILogger _log;

        private IMulliganDataSource _source;
        private CancellationTokenSource _fetchCts;

        private bool _phaseOpen;
        private MulliganContext _lastContext;
        private string _lastHandSig;
        private string _lastTossSig;
        private string _lastFetchKey;

        public GameEventBridge(
            PluginSettings settings,
            IMulliganDataSource source,
            IGameClient client,
            IEventBus bus,
            ILogger log)
        {
            _settings = settings;
            _source = source;
            _client = client;
            _bus = bus;
            _log = log ?? NullLogger.Instance;
            _bus.Subscribe<SettingsChangedEvent>(_ => OnSettingsChanged());
        }

        public void UpdateSource(IMulliganDataSource source) => _source = source;

        public void Attach()
        {
            GameEvents.OnGameStart.Add(HandleGameStart);
            GameEvents.OnGameEnd.Add(HandleGameEnd);
            GameEvents.OnInMenu.Add(HandleGameEnd);
            GameEvents.OnPlayerMulligan.Add(_ => Tick());
            _log.Info("GameEventBridge attached");
        }

        public void Detach()
        {
            _fetchCts?.Cancel();
            _log.Info("GameEventBridge detached");
        }

        public void OnSettingsChanged()
        {
            _lastFetchKey = null;
        }

        public void Tick()
        {
            if (!_phaseOpen && !_client.IsMulliganPhase) return;
            if (!_phaseOpen && _client.IsMulliganPhase)
            {
                if (!IsPinnedDeckMatch(_client.ReadContext())) return;
                OpenPhase();
            }

            try
            {
                var ctx = _client.ReadContext();
                if (!EqualContext(ctx, _lastContext))
                {
                    _lastContext = ctx;
                    _bus.Publish(new MulliganContextChangedEvent(ctx));
                }

                var cards = _client.ReadMulliganHand();
                var toss  = _client.ReadTossFlags();
                var handSig = Sig(cards);
                var tossSig = Sig(toss);
                if (handSig != _lastHandSig || tossSig != _lastTossSig)
                {
                    _lastHandSig = handSig;
                    _lastTossSig = tossSig;
                    _bus.Publish(new MulliganHandChangedEvent(cards, toss));
                }

                var key = $"{ctx.Format.ToWireString()}|{ctx.RankBracket.ToWireString()}|{ctx.DeckCode ?? "-"}";
                if (key != _lastFetchKey)
                {
                    _lastFetchKey = key;
                    KickFetch(ctx);
                }
            }
            catch (Exception ex) { _log.Warn("Tick failed", ex); }
        }

        private void HandleGameStart()
        {
            _lastFetchKey = null;
            if (!IsPinnedDeckMatch(_client.ReadContext()))
            {
                _log.Info("game start ignored: active deck does not match pinned signature");
                return;
            }
            OpenPhase();
            Tick();
        }

        private void OpenPhase()
        {
            _phaseOpen = true;
            _lastContext = null;
            _lastHandSig = null;
            _lastTossSig = null;
            _bus.Publish(new MulliganPhaseStartedEvent());
        }

        private bool IsPinnedDeckMatch(MulliganContext ctx)
        {
            if (!_settings.OnlyShowForPinnedDeck) return true;
            var pinned = _settings.PinnedDeckSignature;
            if (string.IsNullOrEmpty(pinned)) return true;
            if (ctx == null || string.IsNullOrEmpty(ctx.ActiveDeckSignature)) return false;
            return string.Equals(ctx.ActiveDeckSignature, pinned, StringComparison.Ordinal);
        }

        private void HandleGameEnd()
        {
            if (!_phaseOpen) return;
            _phaseOpen = false;
            _fetchCts?.Cancel();
            _bus.Publish(new MulliganPhaseEndedEvent());
        }

        private void KickFetch(MulliganContext ctx)
        {
            _fetchCts?.Cancel();
            _fetchCts = new CancellationTokenSource();
            var token = _fetchCts.Token;
            var shortId = string.IsNullOrEmpty(_settings.PinnedDeckShortId) ? null : _settings.PinnedDeckShortId;
            var query = new MulliganQuery(ctx.Format, ctx.RankBracket, ctx.DeckCode, ctx.ArchetypeId, shortId);
            var src = _source;
            _ = Task.Run(async () =>
            {
                try
                {
                    var ds = await src.FetchAsync(query, token).ConfigureAwait(false) ?? MulliganDataset.Empty;
                    if (token.IsCancellationRequested) return;
                    Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                    {
                        _bus.Publish(new DatasetLoadedEvent(ds));
                    }));
                }
                catch (Exception ex) { _log.Warn("fetch failed", ex); }
            }, token);
        }

        private static bool EqualContext(MulliganContext a, MulliganContext b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null) return false;
            return a.Format == b.Format
                && a.Opponent == b.Opponent
                && a.RankBracket == b.RankBracket
                && string.Equals(a.DeckCode, b.DeckCode, StringComparison.Ordinal)
                && a.HasCoin == b.HasCoin;
        }

        private static string Sig(IReadOnlyList<MulliganCard> cards)
        {
            if (cards == null || cards.Count == 0) return "";
            var sb = new System.Text.StringBuilder();
            foreach (var c in cards) sb.Append(c.DbfId).Append(':').Append(c.SlotIndex).Append(';');
            return sb.ToString();
        }

        private static string Sig(IReadOnlyDictionary<int, bool> flags)
        {
            if (flags == null || flags.Count == 0) return "";
            var sb = new System.Text.StringBuilder();
            foreach (var kv in flags) sb.Append(kv.Key).Append('=').Append(kv.Value ? '1' : '0').Append(';');
            return sb.ToString();
        }
    }
}
