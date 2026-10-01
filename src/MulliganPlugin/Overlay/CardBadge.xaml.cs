using System.Windows.Controls;
using System.Windows.Media;
using HstMulligan.Core.Models;
using HstMulligan.Core.Overlay;

namespace HstMulligan.Plugin.Overlay
{
    public partial class CardBadge : UserControl
    {
        public CardBadge()
        {
            InitializeComponent();
        }

        public void Update(MulliganAdvice advice, double diameter)
        {
            Width = diameter;
            Height = diameter;
            Backdrop.Width = diameter;
            Backdrop.Height = diameter;
            var innerRatio = 0.62;
            InnerHole.Width = diameter * innerRatio;
            InnerHole.Height = diameter * innerRatio;

            var ringColor = ConfidenceColors.ForAdvice(advice.Grade, advice.Confidence);
            RingArc.Fill = ToBrush(ringColor);
            RingArc.Stroke = Brushes.Transparent;

            var progress = double.IsNaN(advice.KeepRate) ? 0.0 : advice.KeepRate;
            var arc = GaugeGeometry.BuildRing(
                new Point2(diameter / 2.0, diameter / 2.0),
                diameter,
                thicknessRatio: 1.0 - innerRatio,
                progress: progress);
            var svg = GaugeGeometry.ToSvgPath(arc);
            RingArc.Data = string.IsNullOrEmpty(svg) ? null : Geometry.Parse(svg);

            PercentLabel.Text = AdviceFormatter.PercentLabel(advice.KeepRate);
            PercentLabel.FontSize = diameter * 0.30;
            GradeLabel.Text = AdviceFormatter.GradeLabel(advice.Grade);
            GradeLabel.FontSize = diameter * 0.12;

            if (advice.HasData && !double.IsNaN(advice.Sample.KeptWinrate))
            {
                LiftLabel.Visibility = System.Windows.Visibility.Visible;
                LiftLabel.FontSize = diameter * 0.11;
                LiftLabel.Text = AdviceFormatter.LiftLabel(advice.Lift);
                LiftLabel.Foreground = advice.Lift >= 0
                    ? new SolidColorBrush(Color.FromRgb(0x8E, 0xC8, 0xA0))
                    : new SolidColorBrush(Color.FromRgb(0xDF, 0x9C, 0x72));
            }
            else
            {
                LiftLabel.Visibility = System.Windows.Visibility.Collapsed;
            }

            var glyph = AdviceFormatter.LowSampleGlyph(advice.Confidence);
            LowSampleGlyph.Text = glyph;
            LowSampleGlyph.FontSize = diameter * 0.18;
            LowSampleGlyph.Visibility = string.IsNullOrEmpty(glyph)
                ? System.Windows.Visibility.Collapsed
                : System.Windows.Visibility.Visible;

            Opacity = advice.HasData ? 1.0 : 0.65;
            ToolTip = AdviceFormatter.SampleFootnote(advice.Sample, advice.ScopedByDeck);
        }

        private static SolidColorBrush ToBrush(RgbaColor c) =>
            new SolidColorBrush(Color.FromArgb(c.A, c.R, c.G, c.B));
    }
}
