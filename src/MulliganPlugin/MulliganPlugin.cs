using System;
using System.Net.Http;
using System.Windows.Controls;
using HstMulligan.Core.Data;
using HstMulligan.Core.Live;
using HstMulligan.Plugin.Bindings;
using HstMulligan.Plugin.Config;
using HstMulligan.Plugin.Overlay;
using Hearthstone_Deck_Tracker.Plugins;

namespace HstMulligan.Plugin
{
    public sealed class MulliganPlugin : IPlugin, IDisposable
    {
        public string Name => "Mulligan V2";
        public string Description => "Keep-rate guidance for every mulligan card, sourced from public aggregate feeds.";
        public string ButtonText => "Settings";
        public string Author => "HST-MulliganV2 contributors";
        public Version Version => new Version(2, 0, 0, 0);

        private PluginSettings _settings;
        private HttpClient _http;
        private IMulliganDataSource _source;
        private MulliganStateGate _gate;
        private LiveMulliganState _state;
        private KeepRateCalculator _calc;
        private MulliganOverlay _overlay;
        private GameEventBridge _bridge;
        private MenuItem _menuItem;

        public MenuItem MenuItem => _menuItem;

        public void OnLoad()
        {
            _settings = PluginSettings.LoadOrCreate();
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("HST-MulliganV2/2.0");

            _source = SourcePipelineFactory.Build(_settings, _http);
            _gate = new MulliganStateGate();
            _state = new LiveMulliganState();
            _calc = new KeepRateCalculator(
                new ConfidenceScorer(_settings.ConfidenceSaturationSamples));

            _overlay = new MulliganOverlay(_settings, _state, _calc);
            _bridge = new GameEventBridge(_settings, _source, _gate, _state, _overlay);

            _menuItem = new MenuItem { Header = "Mulligan V2 – Settings" };
            _menuItem.Click += (_, __) => OnButtonPress();

            _bridge.Attach();
        }

        public void OnUnload()
        {
            try { _bridge?.Detach(); } catch { }
            try { _overlay?.Detach(); } catch { }
            _http?.Dispose();
            _http = null;
            _overlay = null;
            _bridge = null;
            _source = null;
            _gate = null;
            _state = null;
            _calc = null;
        }

        public void OnButtonPress()
        {
            var w = new Config.SettingsWindow(_settings);
            w.Saved += (_, __) =>
            {
                _source = SourcePipelineFactory.Build(_settings, _http);
                _bridge?.OnSettingsChanged();
                _overlay?.OnSettingsChanged();
            };
            w.Show();
        }

        public void OnUpdate()
        {
            _bridge?.Tick();
        }

        public void Dispose() => OnUnload();
    }
}
