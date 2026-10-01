using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using HstMulligan.Core.Models;
using HstMulligan.Core.Overlay;

namespace HstMulligan.Plugin.Overlay.ViewModels
{
    /// <summary>
    /// Observable view-model behind a single mulligan card badge. The
    /// XAML binds its text / brush / geometry properties; the overlay
    /// only has to call <see cref="UpdateFrom"/> when fresh advice lands.
    /// </summary>
    public sealed class CardBadgeViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _percentText = "—";
        private string _gradeText = "";
        private string _liftText = "";
        private string _lowSampleText = "";
        private Brush _ringFill = Brushes.Transparent;
        private Brush _liftBrush = Brushes.Transparent;
        private Geometry _ringGeometry;
        private Visibility _liftVisibility = Visibility.Collapsed;
        private Visibility _lowSampleVisibility = Visibility.Collapsed;
        private double _percentFontSize = 16;
        private double _gradeFontSize = 10;
        private double _liftFontSize = 10;
        private double _glyphFontSize = 14;
        private string _toolTip = "";

        public string PercentText { get => _percentText; set => Set(ref _percentText, value); }
        public string GradeText   { get => _gradeText;   set => Set(ref _gradeText, value); }
        public string LiftText    { get => _liftText;    set => Set(ref _liftText, value); }
        public string LowSampleText { get => _lowSampleText; set => Set(ref _lowSampleText, value); }
        public Brush  RingFill    { get => _ringFill;    set => Set(ref _ringFill, value); }
        public Brush  LiftBrush   { get => _liftBrush;   set => Set(ref _liftBrush, value); }
        public Geometry RingGeometry { get => _ringGeometry; set => Set(ref _ringGeometry, value); }
        public Visibility LiftVisibility { get => _liftVisibility; set => Set(ref _liftVisibility, value); }
        public Visibility LowSampleVisibility { get => _lowSampleVisibility; set => Set(ref _lowSampleVisibility, value); }
        public double PercentFontSize { get => _percentFontSize; set => Set(ref _percentFontSize, value); }
        public double GradeFontSize   { get => _gradeFontSize;   set => Set(ref _gradeFontSize, value); }
        public double LiftFontSize    { get => _liftFontSize;    set => Set(ref _liftFontSize, value); }
        public double GlyphFontSize   { get => _glyphFontSize;   set => Set(ref _glyphFontSize, value); }
        public string ToolTipText     { get => _toolTip;         set => Set(ref _toolTip, value); }

        public void UpdateFrom(MulliganAdvice advice, double diameter)
        {
            var ringColor = ConfidenceColors.ForAdvice(advice.Grade, advice.Confidence);
            RingFill = ToBrush(ringColor);

            var innerRatio = 0.62;
            var progress = double.IsNaN(advice.KeepRate) ? 0.0 : advice.KeepRate;
            var arc = GaugeGeometry.BuildRing(
                new Point2(diameter / 2.0, diameter / 2.0),
                diameter,
                thicknessRatio: 1.0 - innerRatio,
                progress: progress);
            var svg = GaugeGeometry.ToSvgPath(arc);
            RingGeometry = string.IsNullOrEmpty(svg) ? null : Geometry.Parse(svg);

            PercentText = AdviceFormatter.PercentLabel(advice.KeepRate);
            PercentFontSize = diameter * 0.30;
            GradeText = AdviceFormatter.GradeLabel(advice.Grade);
            GradeFontSize = diameter * 0.12;

            if (advice.HasData && !double.IsNaN(advice.Sample.KeptWinrate))
            {
                LiftVisibility = Visibility.Visible;
                LiftFontSize = diameter * 0.11;
                LiftText = AdviceFormatter.LiftLabel(advice.Lift);
                LiftBrush = advice.Lift >= 0
                    ? new SolidColorBrush(Color.FromRgb(0x8E, 0xC8, 0xA0))
                    : new SolidColorBrush(Color.FromRgb(0xDF, 0x9C, 0x72));
            }
            else
            {
                LiftVisibility = Visibility.Collapsed;
            }

            var glyph = AdviceFormatter.LowSampleGlyph(advice.Confidence);
            LowSampleText = glyph;
            GlyphFontSize = diameter * 0.18;
            LowSampleVisibility = string.IsNullOrEmpty(glyph) ? Visibility.Collapsed : Visibility.Visible;

            ToolTipText = AdviceFormatter.SampleFootnote(advice.Sample, advice.ScopedByDeck);
        }

        private static SolidColorBrush ToBrush(RgbaColor c) =>
            new SolidColorBrush(Color.FromArgb(c.A, c.R, c.G, c.B));

        private void Set<T>(ref T field, T value, [CallerMemberName] string name = null)
        {
            if (Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
