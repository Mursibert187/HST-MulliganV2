using System;

namespace HstMulligan.Core.Live
{
    public interface IMulliganStateGate
    {
        bool IsOpen { get; }
        event EventHandler Opened;
        event EventHandler Closed;

        void NotifyGameStart();
        void NotifyMulliganCommitted();
        void NotifyGameEnded();
    }
}
