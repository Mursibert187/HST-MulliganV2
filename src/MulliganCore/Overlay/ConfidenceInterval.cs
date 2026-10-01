using System;

namespace HstMulligan.Core.Overlay
{
    public readonly struct ConfidenceInterval
    {
        public double Low { get; }
        public double High { get; }
        public double Center { get; }
        public bool HasData { get; }

        public ConfidenceInterval(double low, double high, double center, bool hasData)
        {
            Low = low; High = high; Center = center; HasData = hasData;
        }

        public static readonly ConfidenceInterval Empty = new ConfidenceInterval(0, 0, double.NaN, false);
    }

    /// <summary>
    /// Wilson score interval at 95% confidence. Preferred over the plain
    /// Normal approximation at small sample sizes because the interval
    /// shrinks to [0,1] rather than overshooting it.
    /// </summary>
    public static class WilsonInterval
    {
        public static ConfidenceInterval Compute(int kept, int total, double z = 1.96)
        {
            if (total <= 0) return ConfidenceInterval.Empty;
            var n = (double)total;
            var p = kept / n;
            var z2n = z * z / n;
            var denom = 1.0 + z2n;
            var center = (p + z2n / 2.0) / denom;
            var margin = (z * Math.Sqrt(p * (1.0 - p) / n + z2n / (4.0 * n))) / denom;
            var low = Clamp01(center - margin);
            var high = Clamp01(center + margin);
            return new ConfidenceInterval(low, high, p, true);
        }

        private static double Clamp01(double x)
        {
            if (double.IsNaN(x)) return 0.0;
            if (x < 0.0) return 0.0;
            if (x > 1.0) return 1.0;
            return x;
        }
    }
}
