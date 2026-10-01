using System;

namespace HstMulligan.Core.Models
{
    public readonly struct KeepRateSample : IEquatable<KeepRateSample>
    {
        public int Kept { get; }
        public int Mulliganed { get; }
        public double KeptWinrate { get; }
        public double DrawnWinrate { get; }

        public int Total => Kept + Mulliganed;

        public double KeepRate =>
            Total == 0 ? double.NaN : (double)Kept / Total;

        public bool HasData => Total > 0;

        public KeepRateSample(int kept, int mulliganed, double keptWinrate)
            : this(kept, mulliganed, keptWinrate, double.NaN) { }

        public KeepRateSample(int kept, int mulliganed, double keptWinrate, double drawnWinrate)
        {
            if (kept < 0) throw new ArgumentOutOfRangeException(nameof(kept));
            if (mulliganed < 0) throw new ArgumentOutOfRangeException(nameof(mulliganed));
            Kept = kept;
            Mulliganed = mulliganed;
            KeptWinrate = keptWinrate;
            DrawnWinrate = drawnWinrate;
        }

        public static readonly KeepRateSample Empty =
            new KeepRateSample(0, 0, double.NaN, double.NaN);

        public static KeepRateSample Combine(KeepRateSample a, KeepRateSample b)
        {
            var kept = a.Kept + b.Kept;
            var mull = a.Mulliganed + b.Mulliganed;
            var total = kept + mull;
            if (total == 0) return Empty;
            double kwr = WeightedAvg(a.KeptWinrate, a.Kept, b.KeptWinrate, b.Kept);
            double dwr = WeightedAvg(a.DrawnWinrate, a.Mulliganed, b.DrawnWinrate, b.Mulliganed);
            return new KeepRateSample(kept, mull, kwr, dwr);
        }

        private static double WeightedAvg(double va, int wa, double vb, int wb)
        {
            var sumW = (double.IsNaN(va) ? 0 : wa) + (double.IsNaN(vb) ? 0 : wb);
            if (sumW <= 0) return double.NaN;
            var sum = (double.IsNaN(va) ? 0 : va * wa) + (double.IsNaN(vb) ? 0 : vb * wb);
            return sum / sumW;
        }

        public bool Equals(KeepRateSample other) =>
            Kept == other.Kept && Mulliganed == other.Mulliganed &&
            NaNSafeEq(KeptWinrate, other.KeptWinrate) &&
            NaNSafeEq(DrawnWinrate, other.DrawnWinrate);

        private static bool NaNSafeEq(double a, double b) =>
            double.IsNaN(a) ? double.IsNaN(b) : a.Equals(b);

        public override bool Equals(object obj) => obj is KeepRateSample o && Equals(o);
        public override int GetHashCode() => unchecked((Kept * 397) ^ Mulliganed);
        public override string ToString() => $"kept={Kept} mull={Mulliganed} kwr={KeptWinrate:F3} dwr={DrawnWinrate:F3}";
    }
}
