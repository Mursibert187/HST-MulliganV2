using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HstMulligan.Plugin.Config
{
    public sealed class PluginSettings
    {
        public bool Enabled { get; set; } = true;
        public string PrimarySource { get; set; } = "firestone";
        public string FirestoneUrlTemplate { get; set; } =
            "https://static.zerotoheroes.com/hearthstone/data/mulligan/{format}/{rank}.gz.json";
        public string HsReplayUrlTemplate { get; set; } =
            "https://hsreplay.net/analytics/query/card_mulligan_winrate_by_opponent_class/?GameType={format}&RankRange={rank}";
        public string OfflineDatasetPath { get; set; } = "";
        public int CacheTtlMinutes { get; set; } = 60;
        public int ConfidenceSaturationSamples { get; set; } = 500;
        public double OverlayOpacity { get; set; } = 0.95;
        public double OverlayScale { get; set; } = 1.0;
        public bool ShowDetailsPanel { get; set; } = true;
        public string DetailsPanelHotkey { get; set; } = "F9";
        public string ArchetypeIndexPath { get; set; } = "";
        public string ArchetypeIndexUrl { get; set; } = "";
        public int ArchetypeIndexTtlMinutes { get; set; } = 180;
        public string Locale { get; set; } = "en";
        public string LocalizationDir { get; set; } = "";
        public string PinnedDeckShortId { get; set; } = "";
        public string PinnedDeckSignature { get; set; } = "";
        public string PinnedDeckUrlTemplate { get; set; } =
            "https://hsreplay.net/analytics/query/single_deck_mulligan_guide_v2/?deck_id={shortId}&GameType={format}&RankRange={rank}";
        public bool OnlyShowForPinnedDeck { get; set; } = true;
        public bool UseDeckPageScraper { get; set; } = true;
        public string DeckPageUrlTemplate { get; set; } = "https://hsreplay.net/decks/{shortId}/";
        public string DeckPageSessionCookie { get; set; } = "";

        [JsonIgnore]
        public static string SettingsPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HearthstoneDeckTracker", "Plugins", "MulliganV2", "settings.json");

        public static PluginSettings LoadOrCreate()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    var loaded = JsonSerializer.Deserialize<PluginSettings>(json);
                    if (loaded != null) return loaded;
                }
            }
            catch { }
            return new PluginSettings();
        }

        public void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(SettingsPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                var opts = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, opts));
            }
            catch { }
        }
    }
}
