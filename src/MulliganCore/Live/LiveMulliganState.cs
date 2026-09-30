using System;
using System.Collections.Generic;
using System.Linq;

namespace HstMulligan.Core.Live
{
	/// <summary>
	/// What Hearthstone currently shows in the mulligan: which offered card sits where and whether it
	/// is still marked to keep. The plugin fills this from HearthMirror.
	/// </summary>
	public sealed class LiveMulliganState
	{
		public LiveMulliganState(bool waitingForUserInput, IReadOnlyList<LiveMulliganCard> cards)
		{
			WaitingForUserInput = waitingForUserInput;
			Cards = cards ?? Array.Empty<LiveMulliganCard>();
		}

		/// <summary>True until the player confirms the mulligan.</summary>
		public bool WaitingForUserInput { get; }

		public IReadOnlyList<LiveMulliganCard> Cards { get; }

		public bool IsKeepingAll => Cards.All(c => c.Kept);

		public bool IsAnyCardHovered => Cards.Any(c => c.MouseOver);
	}

	public sealed class LiveMulliganCard
	{
		public LiveMulliganCard(int zonePosition, int dbfId, bool kept, bool mouseOver = false)
		{
			ZonePosition = zonePosition;
			DbfId = dbfId;
			Kept = kept;
			MouseOver = mouseOver;
		}

		/// <summary>1-based position in the offered hand.</summary>
		public int ZonePosition { get; }

		public int DbfId { get; }

		/// <summary>False once the player has marked the card to be replaced.</summary>
		public bool Kept { get; }

		public bool MouseOver { get; }
	}
}
