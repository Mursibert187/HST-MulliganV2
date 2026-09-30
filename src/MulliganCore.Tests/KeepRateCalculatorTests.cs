using System.Collections.Generic;
using HstMulligan.Core.Live;
using HstMulligan.Core.Models;
using Xunit;

namespace HstMulligan.Core.Tests
{
	public class KeepRateCalculatorTests
	{
		private const int A = 100, B = 200, C = 300;

		// Justifications for the card at position 1 (A), offered together with B and C.
		private static readonly IReadOnlyList<Justification> ForA = new[]
		{
			new Justification(new int[0], 0.40),
			new Justification(new[] { B }, 0.55),
			new Justification(new[] { C }, 0.35),
			new Justification(new[] { B, C }, 0.62),
		};

		private static LiveMulliganState State(bool waiting, bool keepA, bool keepB, bool keepC) =>
			new LiveMulliganState(waiting, new[]
			{
				new LiveMulliganCard(1, A, keepA),
				new LiveMulliganCard(2, B, keepB),
				new LiveMulliganCard(3, C, keepC),
			});

		[Fact]
		public void KeepingEverything_ShowsContextualRate()
		{
			var result = KeepRateCalculator.Calculate(1, ForA, State(true, true, true, true));

			Assert.Equal(0.62, result.Confidence);
			Assert.Equal(0.62, result.ContextualConfidence);
			Assert.True(result.IsKeepingAll);
		}

		[Fact]
		public void ReplacingAnotherCard_ShowsDynamicRateForRemainingCards()
		{
			var result = KeepRateCalculator.Calculate(1, ForA, State(true, true, true, false));

			Assert.Equal(0.55, result.Confidence);
			Assert.Equal(0.62, result.ContextualConfidence);
			Assert.False(result.IsKeepingAll);
		}

		[Fact]
		public void ReplacingAllOthers_UsesEmptyJustification()
		{
			var result = KeepRateCalculator.Calculate(1, ForA, State(true, true, false, false));

			Assert.Equal(0.40, result.Confidence);
		}

		[Fact]
		public void ReplacingThisCard_HasNoConfidence()
		{
			var result = KeepRateCalculator.Calculate(1, ForA, State(true, false, true, true));

			Assert.Null(result.Confidence);
			Assert.Equal(0.62, result.ContextualConfidence);
		}

		[Fact]
		public void AfterConfirming_FallsBackToContextualAndKeepsTooltipMode()
		{
			var result = KeepRateCalculator.Calculate(1, ForA, State(false, true, false, false), previousIsKeepingAll: false);

			Assert.Equal(0.62, result.Confidence);
			Assert.False(result.IsKeepingAll);
		}

		[Fact]
		public void WithoutLiveState_ShowsContextualRate()
		{
			Assert.Equal(0.62, KeepRateCalculator.Calculate(1, ForA, null).Confidence);
		}

		[Fact]
		public void ContextualRate_DoesNotDependOnListOrder()
		{
			var shuffled = new[] { ForA[3], ForA[0], ForA[2], ForA[1] };

			Assert.Equal(0.62, KeepRateCalculator.Calculate(1, shuffled, null).ContextualConfidence);
		}

		[Fact]
		public void NoJustifications_HasNoConfidence()
		{
			var result = KeepRateCalculator.Calculate(1, new Justification[0], State(true, true, true, true));

			Assert.Null(result.Confidence);
			Assert.Null(result.ContextualConfidence);
		}

		[Fact]
		public void DuplicateCopies_AreMatchedByCount()
		{
			// Hand: A, B, B. Card at position 1 is A.
			var justifications = new[]
			{
				new Justification(new int[0], 0.30),
				new Justification(new[] { B }, 0.45),
				new Justification(new[] { B, B }, 0.70),
			};
			LiveMulliganState Hand(bool keepSecondB) => new LiveMulliganState(true, new[]
			{
				new LiveMulliganCard(1, A, true),
				new LiveMulliganCard(2, B, true),
				new LiveMulliganCard(3, B, keepSecondB),
			});

			Assert.Equal(0.70, KeepRateCalculator.Calculate(1, justifications, Hand(true)).Confidence);
			Assert.Equal(0.45, KeepRateCalculator.Calculate(1, justifications, Hand(false)).Confidence);
		}

		[Fact]
		public void MissingCombination_HasNoConfidence()
		{
			var partial = new[] { new Justification(new[] { B, C }, 0.62) };

			Assert.Null(KeepRateCalculator.Calculate(1, partial, State(true, true, true, false)).Confidence);
		}
	}
}
