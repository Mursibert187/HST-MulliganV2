using System;
using System.Collections.Generic;

namespace HstMulligan.Core.Models
{
    public enum OpponentClass
    {
        Unknown = 0,
        DeathKnight,
        DemonHunter,
        Druid,
        Hunter,
        Mage,
        Paladin,
        Priest,
        Rogue,
        Shaman,
        Warlock,
        Warrior,
        Neutral,
    }

    public static class OpponentClassExtensions
    {
        private static readonly Dictionary<string, OpponentClass> _fromWire =
            new Dictionary<string, OpponentClass>(StringComparer.OrdinalIgnoreCase)
            {
                { "DEATHKNIGHT",  OpponentClass.DeathKnight },
                { "DEMONHUNTER",  OpponentClass.DemonHunter },
                { "DRUID",        OpponentClass.Druid },
                { "HUNTER",       OpponentClass.Hunter },
                { "MAGE",         OpponentClass.Mage },
                { "PALADIN",      OpponentClass.Paladin },
                { "PRIEST",       OpponentClass.Priest },
                { "ROGUE",        OpponentClass.Rogue },
                { "SHAMAN",       OpponentClass.Shaman },
                { "WARLOCK",      OpponentClass.Warlock },
                { "WARRIOR",      OpponentClass.Warrior },
                { "NEUTRAL",      OpponentClass.Neutral },
            };

        public static string ToWireString(this OpponentClass c) => c.ToString().ToUpperInvariant();

        public static OpponentClass FromWireString(string s)
        {
            if (string.IsNullOrEmpty(s)) return OpponentClass.Unknown;
            return _fromWire.TryGetValue(s.Trim(), out var v) ? v : OpponentClass.Unknown;
        }

        public static IEnumerable<OpponentClass> AllPlayable()
        {
            yield return OpponentClass.DeathKnight;
            yield return OpponentClass.DemonHunter;
            yield return OpponentClass.Druid;
            yield return OpponentClass.Hunter;
            yield return OpponentClass.Mage;
            yield return OpponentClass.Paladin;
            yield return OpponentClass.Priest;
            yield return OpponentClass.Rogue;
            yield return OpponentClass.Shaman;
            yield return OpponentClass.Warlock;
            yield return OpponentClass.Warrior;
        }
    }
}
