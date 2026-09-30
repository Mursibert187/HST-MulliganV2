using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

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
            _settings.Save();
            Saved?.Invoke(this, EventArgs.Empty);
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();

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
