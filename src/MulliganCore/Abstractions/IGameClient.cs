using System.Collections.Generic;
using HstMulligan.Core.Live;
using HstMulligan.Core.Models;

namespace HstMulligan.Core.Abstractions
{
    /// <summary>
    /// Core's view onto the live Hearthstone game. All HDT, HearthMirror and
    /// window-chrome details are hidden behind this contract so Core stays
    /// test-friendly and free of a UI-framework dependency.
    /// </summary>
    public interface IGameClient
    {
        bool IsMulliganPhase { get; }
        MulliganContext ReadContext();
        IReadOnlyList<MulliganCard> ReadMulliganHand();
        IReadOnlyDictionary<int, bool> ReadTossFlags();
        WindowBounds ReadGameWindowBounds();
    }

    public readonly struct WindowBounds
    {
        public double X { get; }
        public double Y { get; }
        public double Width { get; }
        public double Height { get; }

        public WindowBounds(double x, double y, double width, double height)
        {
            X = x; Y = y; Width = width; Height = height;
        }

        public static readonly WindowBounds Default1080p = new WindowBounds(0, 0, 1920, 1080);
    }
}
