using System.Windows;
using HstMulligan.Core.Overlay;

namespace HstMulligan.Plugin.Bindings
{
    /// <summary>
    /// Computes the on-screen position of each mulligan card slot given the
    /// current Hearthstone client window bounds. The mulligan card row sits
    /// horizontally centered at ~52% of window height, with 3 or 4 cards
    /// evenly spaced across ~62% of window width.
    /// </summary>
    internal static class OverlayPositioner
    {
        private const double RowCenterYFraction = 0.52;
        private const double RowSpanXFraction   = 0.62;
        private const double CardWidthFraction  = 0.16;
        private const double CardHeightFraction = 0.44;

        public struct SlotRect
        {
            public double CenterX;
            public double CenterY;
            public double CardWidth;
            public double CardHeight;
            public double BadgeSize;
            public Point2 BadgeCenter;
        }

        public static SlotRect ComputeSlot(int slotIndex, int slotCount, double windowWidth, double windowHeight)
        {
            var cx = windowWidth * 0.5;
            var cy = windowHeight * RowCenterYFraction;
            var span = windowWidth * RowSpanXFraction;
            var count = slotCount < 2 ? 2 : slotCount;
            var step = span / count;
            var start = cx - span / 2.0 + step / 2.0;
            var x = start + step * slotIndex;
            var cardW = windowWidth * CardWidthFraction;
            var cardH = windowHeight * CardHeightFraction;

            var badge = cardW * 0.22;
            // Anchor badge to top-right of the card box.
            var badgeCenter = new Point2(
                x + cardW * 0.5 - badge * 0.5,
                cy - cardH * 0.5 + badge * 0.5);
            return new SlotRect
            {
                CenterX = x,
                CenterY = cy,
                CardWidth = cardW,
                CardHeight = cardH,
                BadgeSize = badge,
                BadgeCenter = badgeCenter,
            };
        }

        public static Rect WindowBounds()
        {
            try
            {
                var handle = Hearthstone_Deck_Tracker.User32.GetHearthstoneWindow();
                if (handle != System.IntPtr.Zero)
                {
                    var r = Hearthstone_Deck_Tracker.User32.GetHearthstoneRect(true);
                    return new Rect(r.X, r.Y, r.Width, r.Height);
                }
            }
            catch { }
            return new Rect(0, 0, 1920, 1080);
        }
    }
}
