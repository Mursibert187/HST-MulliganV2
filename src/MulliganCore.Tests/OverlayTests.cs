using System;
using HstMulligan.Core.Live;
using HstMulligan.Core.Models;
using HstMulligan.Core.Overlay;
using Xunit;

namespace HstMulligan.Core.Tests
{
	public class ConfidenceColorsTests
	{
		// Expected values computed independently from HSTracker's formula.
		[Theory]
		[InlineData(-10, "#B71414")]
		[InlineData(-5, "#BB2A14")]
		[InlineData(0, "#CEAF16")]
		[InlineData(2.5, "#468E21")]
		[InlineData(5, "#338321")]
		[InlineData(10, "#227622")]
		public void MatchesReferenceColors(double delta, string expected)
		{
			Assert.Equal(expected, ConfidenceColors.Get(delta, 75).ToHex());
		}

		[Fact]
		public void GaugeStops_AreTheMulliganConfidenceColors()
		{
			Assert.Equal("#B71414", ConfidenceColors.GaugeNegative.ToHex());
			Assert.Equal("#CEAF16", ConfidenceColors.GaugeNeutral.ToHex());
			Assert.Equal("#227622", ConfidenceColors.GaugePositive.ToHex());
		}
	}

	public class GaugeGeometryTests
	{
		[Theory]
		[InlineData(0.0, 0.0)]
		[InlineData(0.5, 101.0)]
		[InlineData(1.0, 202.0)]
		[InlineData(-0.2, 0.0)]
		[InlineData(1.3, 202.0)]
		public void MarkerX_IsClampedToBar(double confidence, double expected)
		{
			Assert.Equal(expected, GaugeGeometry.MarkerX(confidence), 6);
		}

		[Fact]
		public void KeepRange_SpansLowestToHighest()
		{
			var range = GaugeGeometry.KeepRange(new[] { 0.62, 0.40, 0.55 });

			Assert.NotNull(range);
			Assert.Equal(0.40 * 202, range!.Value.Left, 6);
			Assert.Equal((0.62 - 0.40) * 202, range.Value.Width, 6);
		}

		[Fact]
		public void KeepRange_EmptyIsNull()
		{
			Assert.Null(GaugeGeometry.KeepRange(new double[0]));
		}

		[Fact]
		public void ChangeFlash_CoversBothPositions()
		{
			var flash = GaugeGeometry.ChangeFlash(150, 90);

			Assert.Equal((90.0, 60.0), flash);
			Assert.Null(GaugeGeometry.ChangeFlash(null, 90));
		}
	}

	public class MulliganStateGateTests
	{
		private static LiveMulliganState S(bool waiting, bool hovered = false) =>
			new LiveMulliganState(waiting, new[] { new LiveMulliganCard(1, 1, true, hovered) });

		[Fact]
		public void SkipsTheConfirmTick() => Assert.False(MulliganStateGate.ShouldApply(S(true), S(false)));

		[Fact]
		public void SkipsWhileHovered() => Assert.False(MulliganStateGate.ShouldApply(S(true), S(true, hovered: true)));

		[Fact]
		public void AppliesNormalChanges() => Assert.True(MulliganStateGate.ShouldApply(S(true), S(true)));

		[Fact]
		public void AppliesFirstState() => Assert.True(MulliganStateGate.ShouldApply(null, S(true)));

		[Theory]
		[InlineData(0, 2850)]
		[InlineData(1, 2850)]
		[InlineData(3, 3800)]
		public void HideDelay(int swapped, int expectedMs)
		{
			Assert.Equal(TimeSpan.FromMilliseconds(expectedMs), MulliganTiming.HideDelay(swapped));
		}
	}

	public class ModelTests
	{
		[Theory]
		[InlineData("[]", new int[0])]
		[InlineData("[118222]", new[] { 118222 })]
		[InlineData("[118222, 121064]", new[] { 118222, 121064 })]
		[InlineData(" [ 1 ,2 ] ", new[] { 1, 2 })]
		public void ParsesJustificationKeys(string key, int[] expected)
		{
			Assert.Equal(expected, Justification.ParseKey(key));
		}

		[Theory]
		[InlineData(0.50, 0.62, 12)]
		[InlineData(0.62, 0.50, -12)]
		[InlineData(0.504, 0.506, 1)]
		public void TipDelta_IsDifferenceOfRoundedPercents(double baseRate, double adjusted, int expected)
		{
			Assert.Equal(expected, new MulliganTip(TipType.KeptMore, 1, baseRate, adjusted).DeltaPercent);
		}

		[Fact]
		public void CardStatus_ErrorAndWarning()
		{
			MulliganCard Card(OfferedCardStatus s) => new MulliganCard(1, s, new Justification[0], new MulliganTip[0]);

			Assert.True(Card(OfferedCardStatus.NoData).HasError);
			Assert.True(Card(OfferedCardStatus.UnknownCard).HasError);
			Assert.True(Card(OfferedCardStatus.LowData).HasWarning);
			Assert.True(Card(OfferedCardStatus.ValidWithInvalidNeighbors).HasWarning);
			Assert.False(Card(OfferedCardStatus.Valid).HasError || Card(OfferedCardStatus.Valid).HasWarning);
		}
	}
}
