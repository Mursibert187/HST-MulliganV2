using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using HstMulligan.Core.Live;
using HstMulligan.Core.Models;
using HstMulligan.Core.Overlay;

namespace HstMulligan.Plugin.Overlay
{
    public partial class DetailsPanel : UserControl
    {
        private readonly ExpectedWinrateEstimator _estimator = new ExpectedWinrateEstimator();

        public DetailsPanel()
        {
            InitializeComponent();
        }

        public void Update(
            IReadOnlyList<MulliganCard> cards,
            LiveMulliganState state,
            KeepRateCalculator calc,
            MulliganDataset dataset)
        {
            var ctx = state.Context;
            SubHeader.Text = HeaderLine(ctx, dataset);
            Rows.Items.Clear();

            var advices = new List<MulliganAdvice>(cards.Count);
            var tossFlags = new Dictionary<int, bool>(cards.Count);
            long totalSamples = 0;
            foreach (var card in cards)
            {
                var advice = calc.Advise(card, dataset, ctx);
                advices.Add(advice);
                tossFlags[card.SlotIndex] = state.IsTossed(card);
                totalSamples += advice.Sample.Total;
                Rows.Items.Add(BuildRow(card, advice, state.IsTossed(card)));
            }

            var expected = _estimator.Estimate(advices, tossFlags, dataset.BaseWinrate);
            ExpectedLabel.Text = AdviceFormatter.ExpectedWinrateLabel(expected);
            OverallSample.Text = totalSamples > 0
                ? $"queried {totalSamples:N0} games"
                : "awaiting data";
        }

        private static string HeaderLine(MulliganContext ctx, MulliganDataset ds)
        {
            var sb = new StringBuilder();
            sb.Append("vs ").Append(ctx.Opponent == OpponentClass.Unknown ? "any" : ctx.Opponent.ToString().ToLowerInvariant());
            sb.Append(" · ").Append(ctx.Format == FormatType.Unknown ? "any format" : ctx.Format.ToString().ToLowerInvariant());
            sb.Append(" · ").Append(ctx.RankBracket.ToWireString());
            if (!string.IsNullOrEmpty(ctx.DeckCode)) sb.Append(" · deck-scoped");
            if (ds.GeneratedAt.Year > 2000) sb.Append("  (").Append(ds.GeneratedAt.ToLocalTime().ToString("yyyy-MM-dd")).Append(")");
            return sb.ToString();
        }

        private static UIElement BuildRow(MulliganCard card, MulliganAdvice advice, bool tossed)
        {
            var g = new Grid
            {
                Margin = new Thickness(0, 3, 0, 3),
                Opacity = tossed ? 0.5 : 1.0,
            };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var ringColor = ConfidenceColors.ForAdvice(advice.Grade, advice.Confidence);

            var name = new TextBlock
            {
                Text = card.Name,
                Foreground = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0)),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
            };
            var pct = new TextBlock
            {
                Text = AdviceFormatter.PercentLabel(advice.KeepRate) + "%",
                Foreground = ToBrush(ringColor),
                FontFamily = new FontFamily("Segoe UI"),
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Right,
            };
            var bar = BuildMiniBar(advice);
            var grade = new TextBlock
            {
                Text = AdviceFormatter.GradeLabel(advice.Grade),
                Foreground = new SolidColorBrush(Color.FromRgb(0xC8, 0xC8, 0xC8)),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0),
            };
            var lift = new TextBlock
            {
                Text = advice.HasData && !double.IsNaN(advice.Sample.KeptWinrate)
                    ? AdviceFormatter.LiftLabel(advice.Lift)
                    : "",
                Foreground = advice.Lift >= 0
                    ? new SolidColorBrush(Color.FromRgb(0x8E, 0xC8, 0xA0))
                    : new SolidColorBrush(Color.FromRgb(0xDF, 0x9C, 0x72)),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0),
            };
            var note = new TextBlock
            {
                Text = AdviceFormatter.SampleFootnote(advice.Sample, advice.ScopedByDeck),
                Foreground = new SolidColorBrush(Color.FromRgb(0x90, 0x90, 0x90)),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 0, 0),
            };
            Grid.SetColumn(name, 0);
            Grid.SetColumn(pct, 1);
            Grid.SetColumn(bar, 2);
            Grid.SetColumn(grade, 3);
            Grid.SetColumn(lift, 4);
            Grid.SetColumn(note, 5);
            g.Children.Add(name);
            g.Children.Add(pct);
            g.Children.Add(bar);
            g.Children.Add(grade);
            g.Children.Add(lift);
            g.Children.Add(note);
            return g;
        }

        private static UIElement BuildMiniBar(MulliganAdvice advice)
        {
            var container = new Grid
            {
                Width = 36,
                Height = 6,
                Margin = new Thickness(4, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            container.Children.Add(new Rectangle
            {
                Fill = new SolidColorBrush(Color.FromRgb(0x27, 0x30, 0x3A)),
                RadiusX = 2, RadiusY = 2,
            });
            if (advice.HasData)
            {
                var ringColor = ConfidenceColors.ForAdvice(advice.Grade, advice.Confidence);
                container.Children.Add(new Rectangle
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Width = 36 * (double.IsNaN(advice.KeepRate) ? 0 : advice.KeepRate),
                    Fill = ToBrush(ringColor),
                    RadiusX = 2, RadiusY = 2,
                });
            }
            return container;
        }

        private static SolidColorBrush ToBrush(RgbaColor c) =>
            new SolidColorBrush(Color.FromArgb(c.A, c.R, c.G, c.B));
    }
}
