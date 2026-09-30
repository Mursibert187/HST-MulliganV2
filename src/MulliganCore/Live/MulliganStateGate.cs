using System;

namespace HstMulligan.Core.Live
{
    public sealed class MulliganStateGate : IMulliganStateGate
    {
        public bool IsOpen { get; private set; }
        public event EventHandler Opened;
        public event EventHandler Closed;

        public void NotifyGameStart()
        {
            if (IsOpen) return;
            IsOpen = true;
            Opened?.Invoke(this, EventArgs.Empty);
        }

        public void NotifyMulliganCommitted() => Close();
        public void NotifyGameEnded() => Close();

        private void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            Closed?.Invoke(this, EventArgs.Empty);
        }
    }
}
