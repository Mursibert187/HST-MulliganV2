using System;
using HstMulligan.Core.Models;

namespace HstMulligan.Core.Overlay
{
    public readonly struct RgbaColor : IEquatable<RgbaColor>
    {
        public byte R { get; }
        public byte G { get; }
        public byte B { get; }
        public byte A { get; }
        public RgbaColor(byte r, byte g, byte b, byte a = 255) { R = r; G = g; B = b; A = a; }
        public string ToHex() => $"#{A:X2}{R:X2}{G:X2}{B:X2}";
        public RgbaColor WithAlpha(byte a) => new RgbaColor(R, G, B, a);
        public bool Equals(RgbaColor o) => R == o.R && G == o.G && B == o.B && A == o.A;
        public override bool Equals(object obj) => obj is RgbaColor o && Equals(o);
        public override int GetHashCode() => (R << 24) | (G << 16) | (B << 8) | A;
    }

    public static class ConfidenceColors
    {
        public static readonly RgbaColor StrongKeepRing = new RgbaColor(0x1F, 0xB2, 0x6E);
        public static readonly RgbaColor KeepRing       = new RgbaColor(0x7F, 0xC5, 0x4F);
        public static readonly RgbaColor NeutralRing    = new RgbaColor(0xC9, 0xC0, 0x9E);
        public static readonly RgbaColor TossRing       = new RgbaColor(0xE0, 0x8F, 0x3B);
        public static readonly RgbaColor StrongTossRing = new RgbaColor(0xC6, 0x3A, 0x3A);
        public static readonly RgbaColor UnknownRing    = new RgbaColor(0x6D, 0x6D, 0x6D);
        public static readonly RgbaColor RingBackdrop   = new RgbaColor(0x0C, 0x10, 0x18, 0xCC);
        public static readonly RgbaColor LabelText      = new RgbaColor(0xF6, 0xF6, 0xF6);
        public static readonly RgbaColor LabelShadow    = new RgbaColor(0x00, 0x00, 0x00, 0xB0);

        public static RgbaColor ForGrade(DecisionGrade grade)
        {
            switch (grade)
            {
                case DecisionGrade.StrongKeep: return StrongKeepRing;
                case DecisionGrade.Keep:       return KeepRing;
                case DecisionGrade.Neutral:    return NeutralRing;
                case DecisionGrade.Toss:       return TossRing;
                case DecisionGrade.StrongToss: return StrongTossRing;
                default:                       return UnknownRing;
            }
        }

        /// <summary>Blends the grade color with the backdrop when confidence is low.</summary>
        public static RgbaColor ForAdvice(DecisionGrade grade, double confidence)
        {
            var full = ForGrade(grade);
            var c = ConfidenceScorerLimits.Clamp01(confidence);
            var wash = 0.35 + 0.65 * c;
            byte r = (byte)Math.Round(full.R * wash + RingBackdrop.R * (1 - wash));
            byte g = (byte)Math.Round(full.G * wash + RingBackdrop.G * (1 - wash));
            byte b = (byte)Math.Round(full.B * wash + RingBackdrop.B * (1 - wash));
            return new RgbaColor(r, g, b, full.A);
        }
    }

    internal static class ConfidenceScorerLimits
    {
        public static double Clamp01(double x)
        {
            if (double.IsNaN(x)) return 0.0;
            if (x < 0.0) return 0.0;
            if (x > 1.0) return 1.0;
            return x;
        }
    }
}
