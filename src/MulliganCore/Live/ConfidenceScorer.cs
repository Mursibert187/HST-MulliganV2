using System;

namespace HstMulligan.Core.Live
{
    public sealed class ConfidenceScorer
    {
        public int SaturationSampleCount { get; }

        public ConfidenceScorer(int saturationSampleCount = 500)
        {
            if (saturationSampleCount <= 1)
                throw new ArgumentOutOfRangeException(nameof(saturationSampleCount));
            SaturationSampleCount = saturationSampleCount;
        }

        public double Score(int sampleTotal)
        {
            if (sampleTotal <= 0) return 0.0;
            var num = Math.Log10(sampleTotal + 1);
            var den = Math.Log10(SaturationSampleCount + 1);
            return Clamp01(num / den);
        }

        public static double Clamp01(double x)
        {
            if (double.IsNaN(x)) return 0.0;
            if (x < 0.0) return 0.0;
            if (x > 1.0) return 1.0;
            return x;
        }
    }
}
