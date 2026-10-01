using System;
using System.IO;
using System.Net.Http;
using System.Windows.Controls;
using HstMulligan.Core.Abstractions;
using HstMulligan.Core.Data;
using HstMulligan.Core.Events;
using HstMulligan.Core.Live;
using HstMulligan.Plugin.Bindings;
using HstMulligan.Plugin.Config;
using HstMulligan.Plugin.Diagnostics;
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
        private FileLogger _logger;
        private IEventBus _bus;
        private IMulliganDataSource _source;
        private IGameClient _gameClient;
        private KeepRateCalculator _calc;
        private AdviceEngine _engine;
        private MulliganOverlay _overlay;
        private GameEventBridge _bridge;
        private MenuItem _menuItem;

        public MenuItem MenuItem => _menuItem;

        public void OnLoad()
        {
            _settings = PluginSettings.LoadOrCreate();
            _logger = new FileLogger(
                Path.Combine(Path.GetDirectoryName(PluginSettings.SettingsPath) ?? ".", "log.txt"),
                LogLevel.Info);
            _logger.Info($"Mulligan V2 {Version} loading");

            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("HST-MulliganV2/" + Version);

            _bus = new EventBus(_logger);
            _source = SourcePipelineFactory.Build(_settings, _http);
            _gameClient = new HdtGameClient(_logger);
            _calc = new KeepRateCalculator(
                new ConfidenceScorer(_settings.ConfidenceSaturationSamples));
            _engine = new AdviceEngine(_bus, _calc, new ExpectedWinrateEstimator(), _logger);

            _overlay = new MulliganOverlay(_settings, _bus, _logger);
            _bridge = new GameEventBridge(_settings, _source, _gameClient, _bus, _logger);
            _bridge.Attach();

            _menuItem = new MenuItem { Header = "Mulligan V2 – Settings" };
            _menuItem.Click += (_, __) => OnButtonPress();

            _logger.Info("Mulligan V2 loaded");
        }

        public void OnUnload()
        {
            _logger?.Info("Mulligan V2 unloading");
            try { _bridge?.Detach(); } catch { }
            try { _overlay?.Detach(); } catch { }
            try { _engine?.Dispose(); } catch { }
            _http?.Dispose();
            _logger?.Dispose();
            _http = null; _overlay = null; _bridge = null; _engine = null;
            _source = null; _gameClient = null; _calc = null; _bus = null; _logger = null;
        }

        public void OnButtonPress()
        {
            if (_settings == null) return;
            var w = new SettingsWindow(_settings);
            w.Saved += (_, __) =>
            {
                _source = SourcePipelineFactory.Build(_settings, _http);
                _bridge?.UpdateSource(_source);
                _calc = new KeepRateCalculator(new ConfidenceScorer(_settings.ConfidenceSaturationSamples));
                _bus?.Publish(new SettingsChangedEvent());
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
