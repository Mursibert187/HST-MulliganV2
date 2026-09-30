using System;

namespace HstMulligan.Core.Overlay
{
	public readonly struct Rgb : IEquatable<Rgb>
	{
		public Rgb(byte r, byte g, byte b)
		{
			R = r;
			G = g;
			B = b;
		}

		public byte R { get; }
		public byte G { get; }
		public byte B { get; }

		public string ToHex() => $"#{R:X2}{G:X2}{B:X2}";

		public bool Equals(Rgb other) => R == other.R && G == other.G && B == other.B;
		public override bool Equals(object? obj) => obj is Rgb other && Equals(other);
		public override int GetHashCode() => (R << 16) | (G << 8) | B;
		public override string ToString() => ToHex();
	}

	/// <summary>
	/// The gauge colours. Formula ported from HSTracker's Helper.getColorString (MIT), which adapted it
	/// from HSReplay.net; see THIRD_PARTY.md.
	/// </summary>
	public static class ConfidenceColors
	{
		private static readonly double[] Positive = { 120.0, 55.0, 30.0 };
		private static readonly double[] Neutral = { 50.0, 80.0, 45.0 };
		private static readonly double[] Negative = { 0.0, 80.0, 40.0 };

		private const int GaugeIntensity = 75;

		/// <summary>Left end of the gauge ("Replace").</summary>
		public static Rgb GaugeNegative => Get(-10, GaugeIntensity);

		/// <summary>Middle of the gauge.</summary>
		public static Rgb GaugeNeutral => Get(0, GaugeIntensity);

		/// <summary>Right end of the gauge ("Keep").</summary>
		public static Rgb GaugePositive => Get(10, GaugeIntensity);

		public static Rgb Get(double delta, int intensity)
		{
			var colorWinrate = 50 + Math.Max(-50, Math.Min(50, 5 * delta));
			var severity = Math.Abs(0.5 - colorWinrate / 100) * 2;

			var hsl = delta > 0 ? ScaleTriple(severity, Neutral, Positive, intensity)
				: delta < 0 ? ScaleTriple(severity, Neutral, Negative, intensity)
				: Neutral;

			return HslToRgb(hsl[0], hsl[1], hsl[2]);
		}

		private static double[] ScaleTriple(double x, double[] from, double[] to, int intensity) => new[]
		{
			Scale(x, from[0], to[0], intensity),
			Scale(x, from[1], to[1], intensity),
			Scale(x, from[2], to[2], intensity),
		};

		private static double Scale(double x, double from, double to, int intensity) =>
			from + (to - from) * Math.Pow(x, 1.0 - intensity / 100.0);

		/// <summary>https://drafts.csswg.org/css-color/#hsl-to-rgb, channels truncated like the original.</summary>
		public static Rgb HslToRgb(double hue, double saturation, double lightness)
		{
			var h = hue % 360;
			if(h < 0)
				h += 360;
			var s = saturation / 100;
			var l = lightness / 100;

			double F(double n)
			{
				var k = (n + h / 30) % 12;
				var a = s * Math.Min(l, 1 - l);
				return l - a * Math.Max(-1, Math.Min(Math.Min(k - 3, 9 - k), 1));
			}

			return new Rgb((byte)(F(0) * 255), (byte)(F(8) * 255), (byte)(F(4) * 255));
		}
	}
}
