using System;

namespace HstMulligan.Core.Overlay
{
    public readonly struct Point2
    {
        public double X { get; }
        public double Y { get; }
        public Point2(double x, double y) { X = x; Y = y; }
        public override string ToString() => $"({X:F2},{Y:F2})";
    }

    public readonly struct Arc
    {
        public Point2 Center { get; }
        public double RadiusOuter { get; }
        public double RadiusInner { get; }
        public double StartAngleDeg { get; }
        public double SweepAngleDeg { get; }
        public bool LargeArc => SweepAngleDeg > 180.0;

        public Arc(Point2 center, double rOuter, double rInner, double startDeg, double sweepDeg)
        {
            Center = center;
            RadiusOuter = rOuter;
            RadiusInner = rInner;
            StartAngleDeg = startDeg;
            SweepAngleDeg = sweepDeg;
        }
    }

    public static class GaugeGeometry
    {
        /// <summary>
        /// Builds a ring-arc that sweeps clockwise starting at 12 o'clock ("north"),
        /// covering the given progress [0..1]. Progress is clamped.
        /// </summary>
        public static Arc BuildRing(Point2 center, double diameter, double thicknessRatio, double progress)
        {
            if (diameter <= 0) throw new ArgumentOutOfRangeException(nameof(diameter));
            if (thicknessRatio <= 0 || thicknessRatio >= 1) throw new ArgumentOutOfRangeException(nameof(thicknessRatio));
            var rOuter = diameter / 2.0;
            var rInner = rOuter * (1.0 - thicknessRatio);
            var p = Clamp01(progress);
            var sweep = 360.0 * p;
            return new Arc(center, rOuter, rInner, startDeg: -90.0, sweepDeg: sweep);
        }

        public static Point2 PolarToCartesian(Point2 center, double radius, double angleDeg)
        {
            var rad = angleDeg * Math.PI / 180.0;
            return new Point2(center.X + radius * Math.Cos(rad), center.Y + radius * Math.Sin(rad));
        }

        /// <summary>
        /// Emits an SVG-style path "d" attribute for a filled ring arc between
        /// StartAngle and StartAngle + SweepAngle. Suitable for WPF PathGeometry
        /// via StreamGeometry.CreateFromString.
        /// </summary>
        public static string ToSvgPath(Arc arc)
        {
            if (arc.SweepAngleDeg <= 0) return string.Empty;
            var start = arc.StartAngleDeg;
            var end = arc.StartAngleDeg + arc.SweepAngleDeg;
            // Cap tiny arcs so we don't produce degenerate geometry.
            if (arc.SweepAngleDeg >= 359.999)
                end = start + 359.999;

            var p1 = PolarToCartesian(arc.Center, arc.RadiusOuter, start);
            var p2 = PolarToCartesian(arc.Center, arc.RadiusOuter, end);
            var p3 = PolarToCartesian(arc.Center, arc.RadiusInner, end);
            var p4 = PolarToCartesian(arc.Center, arc.RadiusInner, start);

            var large = (end - start) > 180.0 ? 1 : 0;
            var sweepOuter = 1; // clockwise
            var sweepInner = 0;

            return string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "M {0:F3},{1:F3} A {2:F3},{2:F3} 0 {3} {4} {5:F3},{6:F3} L {7:F3},{8:F3} A {9:F3},{9:F3} 0 {3} {10} {11:F3},{12:F3} Z",
                p1.X, p1.Y,
                arc.RadiusOuter, large, sweepOuter,
                p2.X, p2.Y,
                p3.X, p3.Y,
                arc.RadiusInner, sweepInner,
                p4.X, p4.Y);
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
