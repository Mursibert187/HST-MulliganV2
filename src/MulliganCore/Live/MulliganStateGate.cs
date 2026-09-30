using System;

namespace HstMulligan.Core.Live
{
	/// <summary>
	/// Decides whether a new live state should move the gauges, matching the original overlay:
	/// no updates while a card is hovered, and none on the tick where the player confirms the mulligan.
	/// </summary>
	public static class MulliganStateGate
	{
		/// <param name="previous">The last state received, not the last one applied.</param>
		/// <param name="next">The state just received.</param>
		public static bool ShouldApply(LiveMulliganState? previous, LiveMulliganState next)
		{
			if(previous != null && previous.WaitingForUserInput && !next.WaitingForUserInput)
				return false;
			return !next.IsAnyCardHovered;
		}
	}

	public static class MulliganTiming
	{
		/// <summary>How long the overlay stays up after the mulligan is confirmed, while the cards fly away.</summary>
		public static TimeSpan HideDelay(int swappedCards) =>
			TimeSpan.FromMilliseconds(2375 + Math.Max(1, swappedCards) * 475);
	}
}
