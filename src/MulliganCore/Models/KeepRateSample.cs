using System;

namespace HstMulligan.Core.Models
{
    public readonly struct KeepRateSample : IEquatable<KeepRateSample>
    {
        public int Kept { get; }
        public int Mulliganed { get; }
        public double KeptWinrate { get; }

        public int Total => Kept + Mulliganed;

        public double KeepRate =>
            Total == 0 ? double.NaN : (double)Kept / Total;

        public bool HasData => Total > 0;

        public KeepRateSample(int kept, int mulliganed, double keptWinrate)
        {
            if (kept < 0) throw new ArgumentOutOfRangeException(nameof(kept));
            if (mulliganed < 0) throw new ArgumentOutOfRangeException(nameof(mulliganed));
            Kept = kept;
            Mulliganed = mulliganed;
            KeptWinrate = keptWinrate;
        }

        public static readonly KeepRateSample Empty = new KeepRateSample(0, 0, double.NaN);

        public static KeepRateSample Combine(KeepRateSample a, KeepRateSample b)
        {
            var kept = a.Kept + b.Kept;
            var mull = a.Mulliganed + b.Mulliganed;
            var total = kept + mull;
            if (total == 0) return Empty;
            double wr = double.NaN;
            if (!double.IsNaN(a.KeptWinrate) || !double.IsNaN(b.KeptWinrate))
            {
                var wa = double.IsNaN(a.KeptWinrate) ? 0 : a.KeptWinrate * a.Kept;
                var wb = double.IsNaN(b.KeptWinrate) ? 0 : b.KeptWinrate * b.Kept;
                wr = kept == 0 ? double.NaN : (wa + wb) / kept;
            }
            return new KeepRateSample(kept, mull, wr);
        }

        public bool Equals(KeepRateSample other) =>
            Kept == other.Kept && Mulliganed == other.Mulliganed &&
            (double.IsNaN(KeptWinrate) ? double.IsNaN(other.KeptWinrate) : KeptWinrate.Equals(other.KeptWinrate));

        public override bool Equals(object obj) => obj is KeepRateSample o && Equals(o);
        public override int GetHashCode() => unchecked((Kept * 397) ^ Mulliganed);
        public override string ToString() => $"kept={Kept} mull={Mulliganed} kwr={KeptWinrate:F3}";
    }
}
