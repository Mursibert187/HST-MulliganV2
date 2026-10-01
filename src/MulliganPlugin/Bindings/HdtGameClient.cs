using System.Collections.Generic;
using System.Windows;
using Hearthstone_Deck_Tracker;
using HstMulligan.Core.Abstractions;
using HstMulligan.Core.Live;
using HstMulligan.Core.Models;

namespace HstMulligan.Plugin.Bindings
{
    /// <summary>
    /// Core's <see cref="IGameClient"/> backed by HDT. Keeps the HDT dependency
    /// out of the Core assembly entirely so the engine, bus, calculator and
    /// data layers can be unit-tested without the tracker installed.
    /// </summary>
    internal sealed class HdtGameClient : IGameClient
    {
        private readonly HearthstoneMirrorAdapter _adapter;
        private readonly ILogger _log;

        public HdtGameClient(ILogger log = null)
        {
            _adapter = new HearthstoneMirrorAdapter();
            _log = log ?? NullLogger.Instance;
        }

        public bool IsMulliganPhase
        {
            get
            {
                try { return !(Core.Game?.IsMulliganDone ?? true); }
                catch { return false; }
            }
        }

        public MulliganContext ReadContext()
        {
            try { return _adapter.ReadContext(); }
            catch (System.Exception ex)
            {
                _log.Warn("ReadContext failed", ex);
                return MulliganContext.Unknown;
            }
        }

        public IReadOnlyList<MulliganCard> ReadMulliganHand()
        {
            try { return _adapter.ReadMulliganHand(); }
            catch (System.Exception ex)
            {
                _log.Warn("ReadMulliganHand failed", ex);
                return new MulliganCard[0];
            }
        }

        public IReadOnlyDictionary<int, bool> ReadTossFlags()
        {
            try { return _adapter.ReadTossFlags(); }
            catch (System.Exception ex)
            {
                _log.Warn("ReadTossFlags failed", ex);
                return new Dictionary<int, bool>();
            }
        }

        public WindowBounds ReadGameWindowBounds()
        {
            Rect r;
            try { r = OverlayPositioner.WindowBounds(); }
            catch
            {
                return WindowBounds.Default1080p;
            }
            return new WindowBounds(r.X, r.Y, r.Width, r.Height);
        }
    }
}
