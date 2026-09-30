using System.Collections.Generic;
using System.Linq;
using HstMulligan.Core.Models;

namespace HstMulligan.Core.Live
{
	public readonly struct KeepRateResult
	{
		public KeepRateResult(double? confidence, double? contextualConfidence, bool isKeepingAll)
		{
			Confidence = confidence;
			ContextualConfidence = contextualConfidence;
			IsKeepingAll = isKeepingAll;
		}

		/// <summary>The keep rate to show, or null when the card is marked to be replaced or has no data.</summary>
		public double? Confidence { get; }

		/// <summary>The keep rate if every offered card were kept.</summary>
		public double? ContextualConfidence { get; }

		/// <summary>Whether the tooltip talks about the contextual (true) or the dynamic (false) keep rate.</summary>
		public bool IsKeepingAll { get; }
	}

	public static class KeepRateCalculator
	{
		/// <summary>
		/// Works out the keep rate of the card at <paramref name="position"/> for the cards the player is
		/// keeping right now. Once the mulligan is confirmed, or without a live state, the contextual rate
		/// is shown and <paramref name="previousIsKeepingAll"/> is kept as it was.
		/// </summary>
		public static KeepRateResult Calculate(
			int position,
			IReadOnlyList<Justification> justifications,
			LiveMulliganState? state,
			bool previousIsKeepingAll = true)
		{
			if(justifications.Count == 0)
				return new KeepRateResult(null, null, previousIsKeepingAll);

			var contextual = justifications
				.OrderByDescending(j => j.OtherKeptDbfIds.Count)
				.First()
				.Confidence;

			if(state == null || !state.WaitingForUserInput)
				return new KeepRateResult(contextual, contextual, previousIsKeepingAll);

			var isKeepingAll = state.IsKeepingAll;
			var kept = state.Cards.Where(c => c.Kept).ToList();
			var thisCard = kept.FirstOrDefault(c => c.ZonePosition == position);
			if(thisCard == null)
				return new KeepRateResult(null, contextual, isKeepingAll);

			kept.Remove(thisCard);
			var keptCounts = CountByDbfId(kept.Select(c => c.DbfId));

			var match = justifications.FirstOrDefault(j => SameCounts(keptCounts, CountByDbfId(j.OtherKeptDbfIds)));
			return new KeepRateResult(match?.Confidence, contextual, isKeepingAll);
		}

		private static Dictionary<int, int> CountByDbfId(IEnumerable<int> dbfIds) =>
			dbfIds.GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());

		private static bool SameCounts(Dictionary<int, int> a, Dictionary<int, int> b) =>
			a.Count == b.Count && a.All(kv => b.TryGetValue(kv.Key, out var count) && count == kv.Value);
	}
}
