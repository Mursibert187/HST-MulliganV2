using HstMulligan.Core.Live;
using Xunit;

namespace HstMulligan.Core.Tests
{
    public class ConfidenceScorerTests
    {
        [Fact]
        public void ZeroSamplesGivesZeroConfidence()
        {
            var s = new ConfidenceScorer();
            Assert.Equal(0.0, s.Score(0));
        }

        [Fact]
        public void SaturationSampleGivesOne()
        {
            var s = new ConfidenceScorer(saturationSampleCount: 500);
            Assert.Equal(1.0, s.Score(500), precision: 6);
        }

        [Fact]
        public void ConfidenceIsMonotonicallyNonDecreasing()
        {
            var s = new ConfidenceScorer(saturationSampleCount: 500);
            double last = -1;
            for (int n = 0; n <= 1000; n += 50)
            {
                var cur = s.Score(n);
                Assert.True(cur + 1e-9 >= last, $"non-monotonic at n={n}");
                last = cur;
            }
        }

        [Fact]
        public void OverSaturationClampsToOne()
        {
            var s = new ConfidenceScorer(saturationSampleCount: 100);
            Assert.Equal(1.0, s.Score(999_999), precision: 6);
        }
    }
}
