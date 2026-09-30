using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Hearthstone_Deck_Tracker.API;
using HstMulligan.Core.Data;
using HstMulligan.Core.Live;
using HstMulligan.Core.Models;
using HstMulligan.Plugin.Config;
using HstMulligan.Plugin.Overlay;

namespace HstMulligan.Plugin.Bindings
{
    internal sealed class GameEventBridge
    {
        private readonly PluginSettings _settings;
        private readonly IMulliganStateGate _gate;
        private readonly LiveMulliganState _state;
        private readonly MulliganOverlay _overlay;
        private readonly HearthstoneMirrorAdapter _adapter = new HearthstoneMirrorAdapter();

        private IMulliganDataSource _source;
        private CancellationTokenSource _fetchCts;
        private string _lastFetchKey;
        private MulliganDataset _dataset = MulliganDataset.Empty;

        public GameEventBridge(
            PluginSettings settings,
            IMulliganDataSource source,
            IMulliganStateGate gate,
            LiveMulliganState state,
            MulliganOverlay overlay)
        {
            _settings = settings;
            _source = source;
            _gate = gate;
            _state = state;
            _overlay = overlay;
            _gate.Opened += (_, __) => _overlay.OnMulliganOpened(_dataset);
            _gate.Closed += (_, __) => _overlay.OnMulliganClosed();
        }

        public void OnSettingsChanged()
        {
            _lastFetchKey = null;
            _dataset = MulliganDataset.Empty;
        }

        public void UpdateSource(IMulliganDataSource source) => _source = source;

        public void Attach()
        {
            GameEvents.OnGameStart.Add(HandleGameStart);
            GameEvents.OnGameEnd.Add(HandleGameEnd);
            GameEvents.OnPlayerMulligan.Add(_ => Tick());
            GameEvents.OnInMenu.Add(HandleGameEnd);
        }

        public void Detach()
        {
            // HDT's API.GameEvents doesn't expose Remove for individual delegates;
            // handlers become no-ops after we null internal state.
            _fetchCts?.Cancel();
        }

        public void Tick()
        {
            if (!_gate.IsOpen) return;
            try
            {
                var ctx = _adapter.ReadContext();
                var cards = _adapter.ReadMulliganHand();
                var tossFlags = _adapter.ReadTossFlags();
                _state.SetContext(ctx);
                _state.SetCards(cards);
                foreach (var kv in tossFlags)
                {
                    // toggle only if actual state differs
                    var card = cards.Count > kv.Key ? cards[kv.Key] : null;
                    if (card == null) continue;
                    if (_state.IsTossed(card) != kv.Value)
                        _state.ToggleToss(kv.Key);
                }

                var key = $"{ctx.Format.ToWireString()}|{ctx.RankBracket.ToWireString()}|{ctx.DeckCode ?? "-"}";
                if (key != _lastFetchKey)
                {
                    _lastFetchKey = key;
                    KickFetch(ctx);
                }
                _overlay.Refresh(_dataset);
            }
            catch { }
        }

        private void HandleGameStart()
        {
            _gate.NotifyGameStart();
            _lastFetchKey = null;
            Tick();
        }

        private void HandleGameEnd()
        {
            _gate.NotifyGameEnded();
            _state.Clear();
            _fetchCts?.Cancel();
        }

        private void KickFetch(MulliganContext ctx)
        {
            _fetchCts?.Cancel();
            _fetchCts = new CancellationTokenSource();
            var token = _fetchCts.Token;
            var query = new MulliganQuery(ctx.Format, ctx.RankBracket, ctx.DeckCode);
            var src = _source;
            _ = Task.Run(async () =>
            {
                try
                {
                    var ds = await src.FetchAsync(query, token).ConfigureAwait(false);
                    if (token.IsCancellationRequested) return;
                    _dataset = ds ?? MulliganDataset.Empty;
                    Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                    {
                        _overlay.Refresh(_dataset);
                    }));
                }
                catch { }
            }, token);
        }
    }
}
