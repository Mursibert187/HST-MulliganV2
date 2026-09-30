using System;
using System.Collections.Generic;
using System.Linq;

namespace HstMulligan.Core.Models
{
	/// <summary>
	/// Everything the overlay needs for one mulligan, in the same shape as a Mulligan G-V2 response,
	/// so the UI does not care which engine produced it.
	/// </summary>
	public sealed class MulliganV2Data
	{
		public MulliganV2Data(DeckStatus deckStatus, IReadOnlyDictionary<int, MulliganCard> cardsByPosition)
		{
			DeckStatus = deckStatus;
			CardsByPosition = cardsByPosition ?? throw new ArgumentNullException(nameof(cardsByPosition));
		}

		public DeckStatus DeckStatus { get; }

		/// <summary>Keyed by 1-based hand position.</summary>
		public IReadOnlyDictionary<int, MulliganCard> CardsByPosition { get; }

		public bool IsAvailable => DeckStatus is DeckStatus.Supported or DeckStatus.Partial;
	}

	public sealed class MulliganCard
	{
		public MulliganCard(
			int dbfId,
			OfferedCardStatus cardStatus,
			IReadOnlyList<Justification> justifications,
			IReadOnlyList<MulliganTip> tips)
		{
			DbfId = dbfId;
			CardStatus = cardStatus;
			Justifications = justifications ?? Array.Empty<Justification>();
			Tips = tips ?? Array.Empty<MulliganTip>();
		}

		public int DbfId { get; }
		public OfferedCardStatus CardStatus { get; }
		public IReadOnlyList<Justification> Justifications { get; }
		public IReadOnlyList<MulliganTip> Tips { get; }

		public bool HasError => CardStatus is OfferedCardStatus.UnknownCard or OfferedCardStatus.NoData;

		public bool HasWarning => CardStatus is OfferedCardStatus.LowData
			or OfferedCardStatus.ValidWithInvalidNeighbors
			or OfferedCardStatus.LowDataWithInvalidNeighbors;

		/// <summary>
		/// The keep rate when every other offered card is kept too. G-V2 calls this the contextual keep rate.
		/// Picked by content rather than list order, so it does not depend on how the engine sorted its output.
		/// </summary>
		public Justification? Contextual => Justifications
			.OrderByDescending(j => j.OtherKeptDbfIds.Count)
			.FirstOrDefault();
	}

	/// <summary>
	/// The keep rate of a card for one exact set of other cards that end up kept alongside it.
	/// </summary>
	public sealed class Justification
	{
		public Justification(IReadOnlyList<int> otherKeptDbfIds, double confidence)
		{
			OtherKeptDbfIds = otherKeptDbfIds ?? Array.Empty<int>();
			Confidence = confidence;
		}

		public IReadOnlyList<int> OtherKeptDbfIds { get; }

		/// <summary>Keep rate between 0 and 1.</summary>
		public double Confidence { get; }

		/// <summary>Parses a G-V2 justification key such as "[]", "[118222]" or "[118222, 121064]".</summary>
		public static IReadOnlyList<int> ParseKey(string key)
		{
			var trimmed = (key ?? "").Trim().TrimStart('[').TrimEnd(']');
			if(trimmed.Trim().Length == 0)
				return Array.Empty<int>();
			return trimmed
				.Split(',')
				.Select(part => int.TryParse(part.Trim(), out var id) ? (int?)id : null)
				.Where(id => id.HasValue)
				.Select(id => id!.Value)
				.ToArray();
		}
	}

	/// <summary>
	/// One factor that moves a card's keep rate. <see cref="BaseKeepRate"/> is the rate without the factor
	/// and <see cref="AdjustedKeepRate"/> the rate with it, whatever the tip family.
	/// </summary>
	public sealed class MulliganTip
	{
		public MulliganTip(
			TipType type,
			int arrows,
			double baseKeepRate,
			double adjustedKeepRate,
			int? dbfId = null,
			string? opponentClass = null)
		{
			Type = type;
			Arrows = arrows;
			BaseKeepRate = baseKeepRate;
			AdjustedKeepRate = adjustedKeepRate;
			DbfId = dbfId;
			OpponentClass = opponentClass;
		}

		public TipType Type { get; }

		/// <summary>Positive for up arrows, negative for down arrows; the magnitude is the arrow count.</summary>
		public int Arrows { get; }

		public double BaseKeepRate { get; }
		public double AdjustedKeepRate { get; }

		/// <summary>The other card, for card interaction tips.</summary>
		public int? DbfId { get; }

		/// <summary>The opponent's class, for opponent class tips.</summary>
		public string? OpponentClass { get; }

		/// <summary>Whole-percent difference as the overlay shows it.</summary>
		public int DeltaPercent => Percent(AdjustedKeepRate) - Percent(BaseKeepRate);

		public TipFamily Family => Type switch
		{
			TipType.KeptMore or TipType.KeptLess => TipFamily.Card,
			TipType.KeptMoreSecondCopy or TipType.KeptLessSecondCopy => TipFamily.SecondCopy,
			TipType.KeptMoreVsThisOpponent or TipType.KeptLessVsThisOpponent => TipFamily.OpponentClass,
			_ => TipFamily.Initiative,
		};

		public bool IsGoingSecond => Type is TipType.KeptMoreGoingSecond or TipType.KeptLessGoingSecond;

		/// <summary>Rounds a 0..1 rate to whole percent the way the original overlay does.</summary>
		public static int Percent(double rate) => (int)Math.Round(rate * 100);
	}

	public enum DeckStatus
	{
		None,
		Supported,
		Partial,
	}

	public enum OfferedCardStatus
	{
		Valid,
		LowData,
		ValidWithInvalidNeighbors,
		LowDataWithInvalidNeighbors,
		UnknownCard,
		NoData,
	}

	public enum TipType
	{
		KeptMore,
		KeptLess,
		KeptMoreSecondCopy,
		KeptLessSecondCopy,
		KeptMoreVsThisOpponent,
		KeptLessVsThisOpponent,
		KeptMoreGoingFirst,
		KeptLessGoingFirst,
		KeptMoreGoingSecond,
		KeptLessGoingSecond,
	}

	public enum TipFamily
	{
		Card,
		SecondCopy,
		OpponentClass,
		Initiative,
	}
}
