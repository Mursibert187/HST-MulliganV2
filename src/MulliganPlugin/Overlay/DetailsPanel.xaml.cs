using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using HstMulligan.Core.Abstractions;
using HstMulligan.Core.Events;
using HstMulligan.Core.Live;
using HstMulligan.Core.Models;
using HstMulligan.Core.Overlay;

namespace HstMulligan.Plugin.Overlay
{
    public partial class DetailsPanel : UserControl
    {
        private IEventBus _bus;

        public DetailsPanel()
        {
            InitializeComponent();
        }

        public void AttachEventBus(IEventBus bus) { _bus = bus; }

        public void Update(
            IReadOnlyList<MulliganAdvice> advices,
            IReadOnlyDictionary<int, bool> tossFlags,
            MulliganContext ctx,
            MulliganDataset dataset,
            double expectedWinrate)
        {
            SubHeader.Text = HeaderLine(ctx, dataset);
            HighlightActiveOpponentButton(ctx);
            Rows.Items.Clear();
            long totalSamples = 0;
            foreach (var advice in advices)
            {
                var tossed = tossFlags != null
                    && tossFlags.TryGetValue(advice.Card.SlotIndex, out var t) && t;
                totalSamples += advice.Sample.Total;
                Rows.Items.Add(BuildRow(advice, tossed));
            }
            ExpectedLabel.Text = AdviceFormatter.ExpectedWinrateLabel(expectedWinrate);
            OverallSample.Text = totalSamples > 0
                ? $"queried {totalSamples:N0} games"
                : "awaiting data";
        }

        private static readonly SolidColorBrush _selectedBg =
            new SolidColorBrush(Color.FromRgb(0x2A, 0x35, 0x40));
        private static readonly SolidColorBrush _selectedFg =
            new SolidColorBrush(Color.FromRgb(0xF6, 0xF6, 0xF6));
        private static readonly SolidColorBrush _selectedBorder =
            new SolidColorBrush(Color.FromRgb(0x7F, 0xC5, 0x4F));
        private static readonly SolidColorBrush _defaultFg =
            new SolidColorBrush(Color.FromRgb(0xD0, 0xD0, 0xD0));

        private void HighlightActiveOpponentButton(MulliganContext ctx)
        {
            Button active;
            if (ctx == null || !ctx.OverrideOpponent.HasValue)
                active = OppAutoButton;
            else
            {
                switch (ctx.OverrideOpponent.Value)
                {
                    case OpponentClass.Unknown:     active = OppOverallBtn;  break;
                    case OpponentClass.Mage:        active = OppMageBtn;     break;
                    case OpponentClass.Warrior:     active = OppWarriorBtn;  break;
                    case OpponentClass.Hunter:      active = OppHunterBtn;   break;
                    case OpponentClass.Druid:       active = OppDruidBtn;    break;
                    case OpponentClass.Paladin:     active = OppPaladinBtn;  break;
                    case OpponentClass.Priest:      active = OppPriestBtn;   break;
                    case OpponentClass.Rogue:       active = OppRogueBtn;    break;
                    case OpponentClass.Shaman:      active = OppShamanBtn;   break;
                    case OpponentClass.Warlock:     active = OppWarlockBtn;  break;
                    case OpponentClass.DemonHunter: active = OppDemonBtn;    break;
                    case OpponentClass.DeathKnight: active = OppDkBtn;       break;
                    default:                        active = OppAutoButton;  break;
                }
            }
            foreach (var b in AllOpponentButtons())
            {
                var selected = ReferenceEquals(b, active);
                b.Background    = selected ? _selectedBg : System.Windows.Media.Brushes.Transparent;
                b.Foreground    = selected ? _selectedFg : _defaultFg;
                b.BorderBrush   = selected ? _selectedBorder : System.Windows.Media.Brushes.Transparent;
                b.BorderThickness = new Thickness(1);
                b.FontWeight    = selected ? FontWeights.SemiBold : FontWeights.Normal;
            }
        }

        private IEnumerable<Button> AllOpponentButtons()
        {
            yield return OppAutoButton;
            yield return OppOverallBtn;
            yield return OppMageBtn;
            yield return OppWarriorBtn;
            yield return OppHunterBtn;
            yield return OppDruidBtn;
            yield return OppPaladinBtn;
            yield return OppPriestBtn;
            yield return OppRogueBtn;
            yield return OppShamanBtn;
            yield return OppWarlockBtn;
            yield return OppDemonBtn;
            yield return OppDkBtn;
        }

        private void OppAutoButton_Click(object sender, RoutedEventArgs e)    => PublishOverride(null);
        private void OppOverallButton_Click(object sender, RoutedEventArgs e) => PublishOverride(OpponentClass.Unknown);
        private void OppMageButton_Click(object sender, RoutedEventArgs e)        => PublishOverride(OpponentClass.Mage);
        private void OppWarriorButton_Click(object sender, RoutedEventArgs e)     => PublishOverride(OpponentClass.Warrior);
        private void OppHunterButton_Click(object sender, RoutedEventArgs e)      => PublishOverride(OpponentClass.Hunter);
        private void OppDruidButton_Click(object sender, RoutedEventArgs e)       => PublishOverride(OpponentClass.Druid);
        private void OppPaladinButton_Click(object sender, RoutedEventArgs e)     => PublishOverride(OpponentClass.Paladin);
        private void OppPriestButton_Click(object sender, RoutedEventArgs e)      => PublishOverride(OpponentClass.Priest);
        private void OppRogueButton_Click(object sender, RoutedEventArgs e)       => PublishOverride(OpponentClass.Rogue);
        private void OppShamanButton_Click(object sender, RoutedEventArgs e)      => PublishOverride(OpponentClass.Shaman);
        private void OppWarlockButton_Click(object sender, RoutedEventArgs e)     => PublishOverride(OpponentClass.Warlock);
        private void OppDemonHunterButton_Click(object sender, RoutedEventArgs e) => PublishOverride(OpponentClass.DemonHunter);
        private void OppDeathKnightButton_Click(object sender, RoutedEventArgs e) => PublishOverride(OpponentClass.DeathKnight);

        private void PublishOverride(OpponentClass? oc) =>
            _bus?.Publish(new OpponentOverrideChangedEvent(oc));

        private static string HeaderLine(MulliganContext ctx, MulliganDataset ds)
        {
            if (ctx == null) ctx = MulliganContext.Unknown;
            if (ds == null) ds = MulliganDataset.Empty;
            var sb = new StringBuilder();
            var effective = ctx.EffectiveOpponent;
            sb.Append("vs ").Append(effective == OpponentClass.Unknown ? "any" : effective.ToString().ToLowerInvariant());
            if (ctx.OverrideOpponent.HasValue) sb.Append(" (override)");
            sb.Append(" · ").Append(ctx.Format == FormatType.Unknown ? "any format" : ctx.Format.ToString().ToLowerInvariant());
            sb.Append(" · ").Append(ctx.RankBracket.ToWireString());
            if (!string.IsNullOrEmpty(ctx.ArchetypeId)) sb.Append(" · ").Append(ctx.ArchetypeId);
            else if (!string.IsNullOrEmpty(ctx.DeckCode)) sb.Append(" · deck-scoped");
            if (ds.GeneratedAt.Year > 2000) sb.Append("  (").Append(ds.GeneratedAt.ToLocalTime().ToString("yyyy-MM-dd")).Append(")");
            return sb.ToString();
        }

        private static UIElement BuildRow(MulliganAdvice advice, bool tossed)
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
                Text = advice.Card.Name,
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

        private const double MiniBarWidth = 36.0;

        private static UIElement BuildMiniBar(MulliganAdvice advice)
        {
            var container = new Grid
            {
                Width = MiniBarWidth,
                Height = 8,
                Margin = new Thickness(4, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            container.Children.Add(new Rectangle
            {
                Fill = new SolidColorBrush(Color.FromRgb(0x27, 0x30, 0x3A)),
                RadiusX = 2, RadiusY = 2,
            });
            if (!advice.HasData) return container;

            var ringColor = ConfidenceColors.ForAdvice(advice.Grade, advice.Confidence);
            var band = WilsonInterval.Compute(advice.Sample.Kept, advice.Sample.Total);
            if (band.HasData)
            {
                var bandBrush = ToBrush(ringColor);
                bandBrush.Opacity = 0.35;
                var left = MiniBarWidth * band.Low;
                var width = Math.Max(1.0, MiniBarWidth * (band.High - band.Low));
                container.Children.Add(new Rectangle
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(left, 0, 0, 0),
                    Width = width,
                    Fill = bandBrush,
                    RadiusX = 2, RadiusY = 2,
                });
            }
            var centerX = MiniBarWidth * (double.IsNaN(advice.KeepRate) ? 0 : advice.KeepRate);
            container.Children.Add(new Rectangle
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(Math.Max(0, centerX - 1), 0, 0, 0),
                Width = 2,
                Fill = ToBrush(ringColor),
            });
            return container;
        }

        private static SolidColorBrush ToBrush(RgbaColor c) =>
            new SolidColorBrush(Color.FromArgb(c.A, c.R, c.G, c.B));
    }
}
