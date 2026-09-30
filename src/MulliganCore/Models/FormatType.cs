using System;

namespace HstMulligan.Core.Models
{
    public enum FormatType
    {
        Unknown = 0,
        Standard = 1,
        Wild = 2,
        Twist = 3,
        Classic = 4,
    }

    public static class FormatTypeExtensions
    {
        public static string ToWireString(this FormatType f)
        {
            switch (f)
            {
                case FormatType.Standard: return "standard";
                case FormatType.Wild:     return "wild";
                case FormatType.Twist:    return "twist";
                case FormatType.Classic:  return "classic";
                default:                  return "unknown";
            }
        }

        public static FormatType FromWireString(string s)
        {
            if (string.IsNullOrEmpty(s)) return FormatType.Unknown;
            switch (s.Trim().ToLowerInvariant())
            {
                case "standard": return FormatType.Standard;
                case "wild":     return FormatType.Wild;
                case "twist":    return FormatType.Twist;
                case "classic":  return FormatType.Classic;
                default:         return FormatType.Unknown;
            }
        }
    }
}
