using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using HstMulligan.Plugin.Bindings;
using Microsoft.Win32;

namespace HstMulligan.Plugin.Config
{
    public partial class SettingsWindow : Window
    {
        private readonly PluginSettings _settings;
        public event EventHandler Saved;

        public SettingsWindow(PluginSettings settings)
        {
            InitializeComponent();
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            LoadIntoControls();
        }

        private void LoadIntoControls()
        {
            EnabledBox.IsChecked = _settings.Enabled;
            SelectComboItem(SourceCombo, _settings.PrimarySource);
            FirestoneUrl.Text = _settings.FirestoneUrlTemplate ?? "";
            HsReplayUrl.Text = _settings.HsReplayUrlTemplate ?? "";
            OfflinePath.Text = _settings.OfflineDatasetPath ?? "";
            CacheTtlBox.Text = _settings.CacheTtlMinutes.ToString(CultureInfo.InvariantCulture);
            SaturationBox.Text = _settings.ConfidenceSaturationSamples.ToString(CultureInfo.InvariantCulture);
            OpacitySlider.Value = _settings.OverlayOpacity;
            ScaleSlider.Value = _settings.OverlayScale;
            DetailsBox.IsChecked = _settings.ShowDetailsPanel;
            OnlyPinnedBox.IsChecked = _settings.OnlyShowForPinnedDeck;
            PinnedShortIdBox.Text = _settings.PinnedDeckShortId ?? "";
            PinnedUrlBox.Text = _settings.PinnedDeckUrlTemplate ?? "";
            PinnedSigBox.Text = _settings.PinnedDeckSignature ?? "";
            UseScraperBox.IsChecked = _settings.UseDeckPageScraper;
            DeckPageUrlBox.Text = _settings.DeckPageUrlTemplate ?? "";
            DeckPageCookieBox.Text = _settings.DeckPageSessionCookie ?? "";
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _settings.Enabled = EnabledBox.IsChecked == true;
            _settings.PrimarySource = ((ComboBoxItem)SourceCombo.SelectedItem)?.Content?.ToString() ?? "firestone";
            _settings.FirestoneUrlTemplate = FirestoneUrl.Text?.Trim() ?? "";
            _settings.HsReplayUrlTemplate = HsReplayUrl.Text?.Trim() ?? "";
            _settings.OfflineDatasetPath = OfflinePath.Text?.Trim() ?? "";
            if (int.TryParse(CacheTtlBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ttl) && ttl > 0)
                _settings.CacheTtlMinutes = ttl;
            if (int.TryParse(SaturationBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sat) && sat > 1)
                _settings.ConfidenceSaturationSamples = sat;
            _settings.OverlayOpacity = OpacitySlider.Value;
            _settings.OverlayScale = ScaleSlider.Value;
            _settings.ShowDetailsPanel = DetailsBox.IsChecked == true;
            _settings.OnlyShowForPinnedDeck = OnlyPinnedBox.IsChecked == true;
            _settings.PinnedDeckShortId = (PinnedShortIdBox.Text ?? "").Trim();
            _settings.PinnedDeckUrlTemplate = (PinnedUrlBox.Text ?? "").Trim();
            _settings.PinnedDeckSignature = (PinnedSigBox.Text ?? "").Trim();
            _settings.UseDeckPageScraper = UseScraperBox.IsChecked == true;
            _settings.DeckPageUrlTemplate = (DeckPageUrlBox.Text ?? "").Trim();
            _settings.DeckPageSessionCookie = (DeckPageCookieBox.Text ?? "").Trim();
            _settings.Save();
            Saved?.Invoke(this, EventArgs.Empty);
            Close();
        }

        private void PinCurrentDeckButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var adapter = new HearthstoneMirrorAdapter();
                var signature = adapter.ReadActiveDeckSignature();
                if (string.IsNullOrEmpty(signature))
                {
                    MessageBox.Show(this,
                        "Could not read an active deck. Make sure a deck is selected in HDT first.",
                        "Pin current deck");
                    return;
                }
                PinnedSigBox.Text = signature;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Pin failed: " + ex.Message, "Pin current deck");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "JSON (*.json)|*.json|All files (*.*)|*.*",
                Title = "Import settings",
            };
            if (dlg.ShowDialog(this) != true) return;
            try
            {
                var json = File.ReadAllText(dlg.FileName);
                var loaded = JsonSerializer.Deserialize<PluginSettings>(json);
                if (loaded == null)
                {
                    MessageBox.Show(this, "No settings found in file.", "Import");
                    return;
                }
                _settings.Enabled = loaded.Enabled;
                _settings.PrimarySource = loaded.PrimarySource;
                _settings.FirestoneUrlTemplate = loaded.FirestoneUrlTemplate;
                _settings.HsReplayUrlTemplate = loaded.HsReplayUrlTemplate;
                _settings.OfflineDatasetPath = loaded.OfflineDatasetPath;
                _settings.CacheTtlMinutes = loaded.CacheTtlMinutes;
                _settings.ConfidenceSaturationSamples = loaded.ConfidenceSaturationSamples;
                _settings.OverlayOpacity = loaded.OverlayOpacity;
                _settings.OverlayScale = loaded.OverlayScale;
                _settings.ShowDetailsPanel = loaded.ShowDetailsPanel;
                _settings.DetailsPanelHotkey = loaded.DetailsPanelHotkey;
                LoadIntoControls();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Import failed: " + ex.Message, "Import");
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Filter = "JSON (*.json)|*.json",
                Title = "Export settings",
                FileName = "mulliganv2-settings.json",
            };
            if (dlg.ShowDialog(this) != true) return;
            try
            {
                var opts = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(dlg.FileName, JsonSerializer.Serialize(_settings, opts));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Export failed: " + ex.Message, "Export");
            }
        }

        private static void SelectComboItem(ComboBox combo, string value)
        {
            if (string.IsNullOrEmpty(value)) { combo.SelectedIndex = 0; return; }
            for (int i = 0; i < combo.Items.Count; i++)
            {
                if (combo.Items[i] is ComboBoxItem cbi &&
                    string.Equals(cbi.Content?.ToString(), value, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }
            combo.SelectedIndex = 0;
        }
    }
}
