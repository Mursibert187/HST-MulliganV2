using System.Collections.Generic;
using System.Windows;
using Hearthstone_Deck_Tracker;
using HstMulligan.Core.Abstractions;
using HstMulligan.Core.Live;
using HstMulligan.Core.Models;

namespace HstMulligan.Plugin.Bindings
{
    internal sealed class HdtGameClient : IGameClient
    {
        private readonly HearthstoneMirrorAdapter _adapter;
        private readonly IArchetypeIndex _archetypeIndex;
        private readonly ILogger _log;

        public HdtGameClient(IArchetypeIndex archetypeIndex = null, ILogger log = null)
        {
            _adapter = new HearthstoneMirrorAdapter();
            _archetypeIndex = archetypeIndex ?? NullArchetypeIndex.Instance;
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
            MulliganContext baseCtx;
            try { baseCtx = _adapter.ReadContext(); }
            catch (System.Exception ex)
            {
                _log.Warn("ReadContext failed", ex);
                return MulliganContext.Unknown;
            }
            string archetype = null;
            string signature = null;
            try
            {
                var heroClass = _adapter.ReadActiveDeckHeroClass();
                var dbfIds = _adapter.ReadActiveDeckDbfIds();
                if (dbfIds.Count > 0)
                {
                    signature = HstMulligan.Core.Abstractions.Signature.Compute(dbfIds);
                    if (heroClass != OpponentClass.Unknown)
                        archetype = _archetypeIndex.Resolve(heroClass, dbfIds);
                }
            }
            catch (System.Exception ex) { _log.Warn("archetype resolve failed", ex); }
            if (archetype == null && signature == null) return baseCtx;
            return new MulliganContext(
                baseCtx.Format, baseCtx.Opponent, baseCtx.RankBracket,
                baseCtx.DeckCode, baseCtx.HasCoin,
                archetypeId: archetype,
                overrideOpponent: baseCtx.OverrideOpponent,
                activeDeckSignature: signature);
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
            catch { return WindowBounds.Default1080p; }
            return new WindowBounds(r.X, r.Y, r.Width, r.Height);
        }
    }
}
