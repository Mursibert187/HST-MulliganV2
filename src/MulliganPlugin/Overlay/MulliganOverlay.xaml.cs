using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Hearthstone_Deck_Tracker.API;
using HstMulligan.Core.Live;
using HstMulligan.Core.Models;
using HstMulligan.Plugin.Bindings;
using HstMulligan.Plugin.Config;

namespace HstMulligan.Plugin.Overlay
{
    public partial class MulliganOverlay : UserControl
    {
        private readonly PluginSettings _settings;
        private readonly LiveMulliganState _state;
        private readonly KeepRateCalculator _calc;
        private readonly List<CardBadge> _badges = new List<CardBadge>();
        private readonly DetailsPanel _details;
        private MulliganDataset _dataset = MulliganDataset.Empty;
        private bool _attached;

        public MulliganOverlay(PluginSettings settings, LiveMulliganState state, KeepRateCalculator calc)
        {
            InitializeComponent();
            _settings = settings;
            _state = state;
            _calc = calc;
            _details = new DetailsPanel();
            DetailsDock.Children.Add(_details);
            _details.Visibility = _settings.ShowDetailsPanel ? Visibility.Visible : Visibility.Collapsed;
            _state.Changed += (_, __) => Refresh(_dataset);
            Opacity = _settings.OverlayOpacity;
            Visibility = Visibility.Collapsed;
        }

        public void OnSettingsChanged()
        {
            Opacity = _settings.OverlayOpacity;
            _details.Visibility = _settings.ShowDetailsPanel ? Visibility.Visible : Visibility.Collapsed;
        }

        public void OnMulliganOpened(MulliganDataset dataset)
        {
            _dataset = dataset ?? MulliganDataset.Empty;
            EnsureAttached();
            Visibility = Visibility.Visible;
            Refresh(_dataset);
        }

        public void OnMulliganClosed()
        {
            Visibility = Visibility.Collapsed;
            EnsureBadgeCount(0);
        }

        public void Detach()
        {
            if (!_attached) return;
            try { Core.OverlayCanvas.Children.Remove(this); } catch { }
            _attached = false;
        }

        public void Refresh(MulliganDataset dataset)
        {
            _dataset = dataset ?? _dataset ?? MulliganDataset.Empty;
            var cards = _state.Cards;
            var ctx = _state.Context;
            EnsureBadgeCount(cards.Count);
            var bounds = OverlayPositioner.WindowBounds();
            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                var slot = OverlayPositioner.ComputeSlot(i, cards.Count, bounds.Width, bounds.Height);
                var advice = _calc.Advise(card, _dataset, ctx);
                var badge = _badges[i];
                var scale = _settings.OverlayScale;
                var size = slot.BadgeSize * scale;
                badge.Update(advice, size);
                Canvas.SetLeft(badge, slot.BadgeCenter.X - size / 2.0);
                Canvas.SetTop(badge, slot.BadgeCenter.Y - size / 2.0);
                if (_state.IsTossed(card))
                    badge.Opacity = 0.35;
            }
            _details.Update(cards, _state, _calc, _dataset);
        }

        private void EnsureAttached()
        {
            if (_attached) return;
            try
            {
                Core.OverlayCanvas.Children.Add(this);
                Canvas.SetLeft(this, 0);
                Canvas.SetTop(this, 0);
                var bounds = OverlayPositioner.WindowBounds();
                Width = bounds.Width;
                Height = bounds.Height;
                _attached = true;
            }
            catch { }
        }

        private void EnsureBadgeCount(int desired)
        {
            while (_badges.Count < desired)
            {
                var b = new CardBadge();
                _badges.Add(b);
                BadgeLayer.Children.Add(b);
            }
            while (_badges.Count > desired)
            {
                var last = _badges.Count - 1;
                BadgeLayer.Children.Remove(_badges[last]);
                _badges.RemoveAt(last);
            }
        }
    }
}
