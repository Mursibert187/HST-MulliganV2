using HstMulligan.Core.Live;
using Xunit;

namespace HstMulligan.Core.Tests
{
    public class MulliganStateGateTests
    {
        [Fact]
        public void OpensOnGameStart()
        {
            var g = new MulliganStateGate();
            int opened = 0;
            g.Opened += (_, __) => opened++;
            g.NotifyGameStart();
            Assert.True(g.IsOpen);
            Assert.Equal(1, opened);
        }

        [Fact]
        public void SecondGameStartIsIdempotent()
        {
            var g = new MulliganStateGate();
            int opened = 0;
            g.Opened += (_, __) => opened++;
            g.NotifyGameStart();
            g.NotifyGameStart();
            Assert.Equal(1, opened);
        }

        [Fact]
        public void ClosesOnCommitAndOnGameEnd()
        {
            var g = new MulliganStateGate();
            int closed = 0;
            g.Closed += (_, __) => closed++;
            g.NotifyGameStart();
            g.NotifyMulliganCommitted();
            g.NotifyGameStart();
            g.NotifyGameEnded();
            Assert.False(g.IsOpen);
            Assert.Equal(2, closed);
        }

        [Fact]
        public void CommitBeforeStartIsNoop()
        {
            var g = new MulliganStateGate();
            int closed = 0;
            g.Closed += (_, __) => closed++;
            g.NotifyMulliganCommitted();
            Assert.False(g.IsOpen);
            Assert.Equal(0, closed);
        }
    }
}
