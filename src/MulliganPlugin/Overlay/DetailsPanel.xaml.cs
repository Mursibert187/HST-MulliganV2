using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using HstMulligan.Core.Live;
using HstMulligan.Core.Models;
using HstMulligan.Core.Overlay;

namespace HstMulligan.Plugin.Overlay
{
    public partial class DetailsPanel : UserControl
    {
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
            foreach (var card in cards)
            {
                var advice = calc.Advise(card, dataset, ctx);
                Rows.Items.Add(BuildRow(card, advice, state.IsTossed(card)));
            }
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
            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new System.Windows.GridLength(180) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new System.Windows.GridLength(64) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new System.Windows.GridLength(90) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
            g.Margin = new System.Windows.Thickness(0, 3, 0, 3);
            g.Opacity = tossed ? 0.5 : 1.0;

            var name = new TextBlock
            {
                Text = card.Name,
                Foreground = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0)),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
            };
            var pct = new TextBlock
            {
                Text = AdviceFormatter.PercentLabel(advice.KeepRate) + "%",
                Foreground = ToBrush(ConfidenceColors.ForAdvice(advice.Grade, advice.Confidence)),
                FontFamily = new FontFamily("Segoe UI"),
                FontWeight = System.Windows.FontWeights.SemiBold,
                FontSize = 13,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                TextAlignment = System.Windows.TextAlignment.Right,
            };
            var grade = new TextBlock
            {
                Text = AdviceFormatter.GradeLabel(advice.Grade),
                Foreground = new SolidColorBrush(Color.FromRgb(0xC8, 0xC8, 0xC8)),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 11,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Margin = new System.Windows.Thickness(8, 0, 0, 0),
            };
            var note = new TextBlock
            {
                Text = AdviceFormatter.SampleFootnote(advice.Sample, advice.ScopedByDeck),
                Foreground = new SolidColorBrush(Color.FromRgb(0x90, 0x90, 0x90)),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 10,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Margin = new System.Windows.Thickness(12, 0, 0, 0),
            };
            Grid.SetColumn(name, 0);
            Grid.SetColumn(pct, 1);
            Grid.SetColumn(grade, 2);
            Grid.SetColumn(note, 3);
            g.Children.Add(name);
            g.Children.Add(pct);
            g.Children.Add(grade);
            g.Children.Add(note);
            return g;
        }

        private static SolidColorBrush ToBrush(RgbaColor c) =>
            new SolidColorBrush(Color.FromArgb(c.A, c.R, c.G, c.B));
    }
}
