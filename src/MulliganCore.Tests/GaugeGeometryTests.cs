using HstMulligan.Core.Overlay;
using Xunit;

namespace HstMulligan.Core.Tests
{
    public class GaugeGeometryTests
    {
        [Fact]
        public void ZeroProgressProducesZeroSweep()
        {
            var arc = GaugeGeometry.BuildRing(new Point2(0, 0), diameter: 40, thicknessRatio: 0.25, progress: 0.0);
            Assert.Equal(0.0, arc.SweepAngleDeg);
            Assert.Equal(string.Empty, GaugeGeometry.ToSvgPath(arc));
        }

        [Fact]
        public void HalfProgressSweepsOneEighty()
        {
            var arc = GaugeGeometry.BuildRing(new Point2(0, 0), 40, 0.25, 0.5);
            Assert.Equal(180.0, arc.SweepAngleDeg, precision: 6);
        }

        [Fact]
        public void FullProgressYieldsCircleCappedJustBelow360()
        {
            var arc = GaugeGeometry.BuildRing(new Point2(0, 0), 40, 0.25, 1.0);
            Assert.Equal(360.0, arc.SweepAngleDeg, precision: 6);
            var path = GaugeGeometry.ToSvgPath(arc);
            Assert.False(string.IsNullOrEmpty(path));
            Assert.StartsWith("M ", path);
            Assert.EndsWith("Z", path);
        }

        [Fact]
        public void PolarPointIsOnCircle()
        {
            var c = new Point2(10, 10);
            var p = GaugeGeometry.PolarToCartesian(c, 5, 0);
            Assert.Equal(15.0, p.X, precision: 6);
            Assert.Equal(10.0, p.Y, precision: 6);
        }

        [Theory]
        [InlineData(-0.5, 0.0)]
        [InlineData(1.5, 1.0)]
        [InlineData(double.NaN, 0.0)]
        public void ClampBoundsProgress(double input, double expected)
        {
            Assert.Equal(expected, GaugeGeometry.Clamp01(input));
        }
    }
}
