using System.Windows;
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
            var brush = ToBrush(ringColor);
            RingArc.Fill = brush;
            RingArc.Stroke = Brushes.Transparent;

            var progress = double.IsNaN(advice.KeepRate) ? 0.0 : advice.KeepRate;
            var arc = GaugeGeometry.BuildRing(
                new Point2(diameter / 2.0, diameter / 2.0),
                diameter,
                thicknessRatio: 1.0 - innerRatio,
                progress: progress);
            var svg = GaugeGeometry.ToSvgPath(arc);
            if (!string.IsNullOrEmpty(svg))
                RingArc.Data = Geometry.Parse(svg);
            else
                RingArc.Data = null;

            PercentLabel.Text = AdviceFormatter.PercentLabel(advice.KeepRate);
            PercentLabel.FontSize = diameter * 0.34;
            GradeLabel.Text = AdviceFormatter.GradeLabel(advice.Grade);
            GradeLabel.FontSize = diameter * 0.14;

            Opacity = advice.HasData ? 1.0 : 0.65;
            ToolTip = AdviceFormatter.SampleFootnote(advice.Sample, advice.ScopedByDeck);
        }

        private static SolidColorBrush ToBrush(RgbaColor c) =>
            new SolidColorBrush(Color.FromArgb(c.A, c.R, c.G, c.B));
    }
}
