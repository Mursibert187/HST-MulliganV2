using System;
using System.Collections.Generic;
using System.Linq;

namespace HstMulligan.Core.Overlay
{
	/// <summary>
	/// Positions inside the 212 px confidence bar, in unscaled 1080p pixels.
	/// </summary>
	public static class GaugeGeometry
	{
		public const double Width = 212;
		public const double MarkerWidth = 10;

		/// <summary>Left edge of the marker for a keep rate between 0 and 1.</summary>
		public static double MarkerX(double confidence) =>
			Math.Max(0, Math.Min(confidence * (Width - MarkerWidth), Width - MarkerWidth));

		/// <summary>
		/// The shaded keep range, from the marker position of the lowest keep rate to that of the highest.
		/// Null when there is nothing to show.
		/// </summary>
		public static (double Left, double Width)? KeepRange(IEnumerable<double> confidences)
		{
			var list = confidences.ToList();
			if(list.Count == 0)
				return null;
			var left = MarkerX(list.Min());
			var right = MarkerX(list.Max());
			return (left, right - left);
		}

		/// <summary>The strip that flashes between the old and the new marker position.</summary>
		public static (double Left, double Width)? ChangeFlash(double? previousX, double? currentX)
		{
			if(!previousX.HasValue || !currentX.HasValue)
				return null;
			return (Math.Min(previousX.Value, currentX.Value), Math.Abs(currentX.Value - previousX.Value));
		}

		/// <summary>Overlay scale for a Hearthstone window of the given height.</summary>
		public static double Scaling(double windowHeight) => windowHeight / 1080;
	}
}
