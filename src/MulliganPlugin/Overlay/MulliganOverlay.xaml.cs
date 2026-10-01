using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Hearthstone_Deck_Tracker.API;
using HstMulligan.Core.Abstractions;
using HstMulligan.Core.Events;
using HstMulligan.Core.Models;
using HstMulligan.Plugin.Bindings;
using HstMulligan.Plugin.Config;

namespace HstMulligan.Plugin.Overlay
{
    public partial class MulliganOverlay : UserControl
    {
        private readonly PluginSettings _settings;
        private readonly IEventBus _bus;
        private readonly ILogger _log;
        private readonly List<CardBadge> _badges = new List<CardBadge>();
        private readonly DetailsPanel _details;
        private readonly List<IDisposable> _subs = new List<IDisposable>();
        private bool _attached;

        public MulliganOverlay(PluginSettings settings, IEventBus bus, ILogger log = null)
        {
            InitializeComponent();
            _settings = settings;
            _bus = bus;
            _log = log ?? NullLogger.Instance;
            _details = new DetailsPanel();
            _details.AttachEventBus(_bus);
            DetailsDock.Children.Add(_details);
            _details.Visibility = _settings.ShowDetailsPanel ? Visibility.Visible : Visibility.Collapsed;
            Opacity = _settings.OverlayOpacity;
            Visibility = Visibility.Collapsed;

            _subs.Add(_bus.Subscribe<MulliganPhaseStartedEvent>(_ => Dispatch(OnOpened)));
            _subs.Add(_bus.Subscribe<MulliganPhaseEndedEvent>(_ => Dispatch(OnClosed)));
            _subs.Add(_bus.Subscribe<AdviceComputedEvent>(e => Dispatch(() => Render(e))));
            _subs.Add(_bus.Subscribe<SettingsChangedEvent>(_ => Dispatch(ApplySettings)));
        }

        public void Detach()
        {
            foreach (var s in _subs) s.Dispose();
            _subs.Clear();
            if (!_attached) return;
            try { Core.OverlayCanvas.Children.Remove(this); } catch { }
            _attached = false;
        }

        private void Dispatch(Action a)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess()) a();
            else dispatcher.BeginInvoke(a);
        }

        private void OnOpened()
        {
            EnsureAttached();
            Visibility = Visibility.Visible;
        }

        private void OnClosed()
        {
            Visibility = Visibility.Collapsed;
            EnsureBadgeCount(0);
        }

        private void ApplySettings()
        {
            Opacity = _settings.OverlayOpacity;
            _details.Visibility = _settings.ShowDetailsPanel ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Render(AdviceComputedEvent evt)
        {
            var advices = evt.Advices;
            EnsureBadgeCount(advices.Count);
            var bounds = OverlayPositioner.WindowBounds();
            for (int i = 0; i < advices.Count; i++)
            {
                var advice = advices[i];
                var slot = OverlayPositioner.ComputeSlot(i, advices.Count, bounds.Width, bounds.Height);
                var badge = _badges[i];
                var size = slot.BadgeSize * _settings.OverlayScale;
                badge.Update(advice, size);
                Canvas.SetLeft(badge, slot.BadgeCenter.X - size / 2.0);
                Canvas.SetTop(badge, slot.BadgeCenter.Y - size / 2.0);
                if (evt.TossFlags != null
                    && evt.TossFlags.TryGetValue(advice.Card.SlotIndex, out var t) && t)
                    badge.Opacity = 0.35;
                else
                    badge.Opacity = advice.HasData ? 1.0 : 0.65;
            }
            _details.Update(advices, evt.TossFlags, evt.Context, evt.Dataset, evt.ExpectedWinrate);
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
                _log.Debug("overlay attached");
            }
            catch (Exception ex) { _log.Warn("overlay attach failed", ex); }
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
