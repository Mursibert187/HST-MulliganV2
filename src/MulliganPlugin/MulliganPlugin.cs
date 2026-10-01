using System;
using System.IO;
using System.Net.Http;
using System.Windows.Controls;
using System.Reflection;
using HstMulligan.Core.Abstractions;
using HstMulligan.Core.Data;
using HstMulligan.Core.Events;
using HstMulligan.Core.Live;
using HstMulligan.Core.Localization;
using HstMulligan.Plugin.Bindings;
using HstMulligan.Plugin.Config;
using HstMulligan.Plugin.Diagnostics;
using HstMulligan.Plugin.Input;
using HstMulligan.Plugin.Overlay;
using Hearthstone_Deck_Tracker.Plugins;
using PluginSettings = HstMulligan.Plugin.Config.PluginSettings;

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
        private GlobalHotkey _hotkey;
        private IArchetypeIndex _archetypeIndex;
        private MenuItem _menuItem;

        public MenuItem MenuItem => _menuItem;

        public void OnLoad()
        {
            _settings = PluginSettings.LoadOrCreate();
            _logger = new FileLogger(
                Path.Combine(Path.GetDirectoryName(PluginSettings.SettingsPath) ?? ".", "log.txt"),
                LogLevel.Info);
            _logger.Info($"Mulligan V2 {Version} loading");
            BootstrapLocalization(_settings, _logger);

            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("HST-MulliganV2/" + Version);

            _bus = new EventBus(_logger);
            _source = SourcePipelineFactory.Build(_settings, _http);
            _archetypeIndex = BuildArchetypeIndex(_settings, _http, _logger);
            _gameClient = new HdtGameClient(_archetypeIndex, _logger);
            _calc = new KeepRateCalculator(
                new ConfidenceScorer(_settings.ConfidenceSaturationSamples));
            _engine = new AdviceEngine(_bus, _calc, new ExpectedWinrateEstimator(), _logger);

            _overlay = new MulliganOverlay(_settings, _bus, _logger);
            _bridge = new GameEventBridge(_settings, _source, _gameClient, _bus, _logger);
            _bridge.Attach();

            try
            {
                _hotkey = new GlobalHotkey(_logger);
                _hotkey.Register(_settings.DetailsPanelHotkey,
                    () => _bus?.Publish(new DetailsVisibilityToggleEvent()));
            }
            catch (Exception ex) { _logger.Warn("GlobalHotkey init failed", ex); }

            _menuItem = new MenuItem { Header = "Mulligan V2 – Settings" };
            _menuItem.Click += (_, __) => OnButtonPress();

            _logger.Info("Mulligan V2 loaded");
        }

        private static void BootstrapLocalization(PluginSettings settings, ILogger log)
        {
            var locale = string.IsNullOrWhiteSpace(settings.Locale) ? "en" : settings.Locale.Trim();
            var dir = settings.LocalizationDir;
            if (string.IsNullOrWhiteSpace(dir))
            {
                try
                {
                    var asmPath = Assembly.GetExecutingAssembly().Location;
                    var asmDir = Path.GetDirectoryName(asmPath);
                    if (!string.IsNullOrEmpty(asmDir))
                        dir = Path.Combine(asmDir, "Assets");
                }
                catch (Exception ex) { log.Warn("locale dir probe failed", ex); }
            }
            var path = string.IsNullOrEmpty(dir) ? null : Path.Combine(dir, $"strings-{locale}.json");
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                Localization.Current = JsonLocalization.LoadFile(path, locale);
                log.Info($"localization loaded: {path}");
            }
            else
            {
                Localization.Current = NullLocalization.Instance;
                log.Info($"localization falling back to defaults (looked for {path ?? "<no dir>"})");
            }
        }

        private static IArchetypeIndex BuildArchetypeIndex(PluginSettings settings, HttpClient http, ILogger log)
        {
            var layers = new List<IArchetypeIndex>();
            if (!string.IsNullOrWhiteSpace(settings.ArchetypeIndexUrl))
            {
                var ttl = TimeSpan.FromMinutes(Math.Max(5, settings.ArchetypeIndexTtlMinutes));
                layers.Add(new RemoteArchetypeIndex(http, settings.ArchetypeIndexUrl, ttl, log));
            }
            if (!string.IsNullOrWhiteSpace(settings.ArchetypeIndexPath))
                layers.Add(new JsonArchetypeIndex(settings.ArchetypeIndexPath));
            if (layers.Count == 0) return NullArchetypeIndex.Instance;
            if (layers.Count == 1) return layers[0];
            return new CompositeArchetypeIndex(layers.ToArray());
        }

        public void OnUnload()
        {
            _logger?.Info("Mulligan V2 unloading");
            try { _hotkey?.Dispose(); } catch { }
            try { _bridge?.Detach(); } catch { }
            try { _overlay?.Detach(); } catch { }
            try { _engine?.Dispose(); } catch { }
            try { (_archetypeIndex as IDisposable)?.Dispose(); } catch { }
            _http?.Dispose();
            _logger?.Dispose();
            _http = null; _overlay = null; _bridge = null; _engine = null; _hotkey = null;
            _source = null; _gameClient = null; _calc = null; _bus = null; _logger = null;
            _archetypeIndex = null;
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
