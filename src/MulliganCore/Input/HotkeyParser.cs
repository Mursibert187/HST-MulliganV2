using System;
using System.Collections.Generic;

namespace HstMulligan.Core.Input
{
    [Flags]
    public enum HotkeyModifiers : uint
    {
        None = 0,
        Alt = 0x1,
        Control = 0x2,
        Shift = 0x4,
        Win = 0x8,
    }

    public readonly struct HotkeyBinding : IEquatable<HotkeyBinding>
    {
        public HotkeyModifiers Modifiers { get; }
        public uint VirtualKey { get; }
        public bool IsValid => VirtualKey != 0;

        public HotkeyBinding(HotkeyModifiers mods, uint vk) { Modifiers = mods; VirtualKey = vk; }

        public static readonly HotkeyBinding None = new HotkeyBinding(HotkeyModifiers.None, 0);

        public bool Equals(HotkeyBinding other) => Modifiers == other.Modifiers && VirtualKey == other.VirtualKey;
        public override bool Equals(object obj) => obj is HotkeyBinding o && Equals(o);
        public override int GetHashCode() => unchecked(((int)Modifiers << 16) ^ (int)VirtualKey);
        public override string ToString() => $"{Modifiers}+VK({VirtualKey:X})";
    }

    /// <summary>
    /// Parses human-friendly shortcut strings ("Ctrl+Shift+F9", "Alt+Space",
    /// "M", "PageDown") into a modifier bitfield + virtual-key code suitable
    /// for Win32 RegisterHotKey. Unknown input yields <see cref="HotkeyBinding.None"/>.
    /// </summary>
    public static class HotkeyParser
    {
        private static readonly Dictionary<string, uint> _namedKeys = BuildNamedKeys();

        public static HotkeyBinding Parse(string shortcut)
        {
            if (string.IsNullOrWhiteSpace(shortcut)) return HotkeyBinding.None;
            var parts = shortcut.Split(new[] { '+', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var mods = HotkeyModifiers.None;
            uint vk = 0;
            foreach (var raw in parts)
            {
                var p = raw.Trim().ToUpperInvariant();
                if (p.Length == 0) continue;
                switch (p)
                {
                    case "CTRL":
                    case "CONTROL":  mods |= HotkeyModifiers.Control; continue;
                    case "SHIFT":    mods |= HotkeyModifiers.Shift;   continue;
                    case "ALT":      mods |= HotkeyModifiers.Alt;     continue;
                    case "WIN":
                    case "META":
                    case "SUPER":    mods |= HotkeyModifiers.Win;     continue;
                }
                if (_namedKeys.TryGetValue(p, out var mapped)) { vk = mapped; continue; }
                if (p.Length == 1)
                {
                    var ch = p[0];
                    if (ch >= 'A' && ch <= 'Z') vk = ch;
                    else if (ch >= '0' && ch <= '9') vk = ch;
                }
            }
            return new HotkeyBinding(mods, vk);
        }

        private static Dictionary<string, uint> BuildNamedKeys()
        {
            var map = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
            for (int i = 1; i <= 24; i++) map["F" + i] = (uint)(0x6F + i);
            map["ESC"] = 0x1B; map["ESCAPE"] = 0x1B;
            map["TAB"] = 0x09;
            map["SPACE"] = 0x20;
            map["BACKSPACE"] = 0x08; map["BS"] = 0x08;
            map["ENTER"] = 0x0D; map["RETURN"] = 0x0D;
            map["INSERT"] = 0x2D; map["INS"] = 0x2D;
            map["DELETE"] = 0x2E; map["DEL"] = 0x2E;
            map["HOME"] = 0x24; map["END"] = 0x23;
            map["PAGEUP"] = 0x21; map["PGUP"] = 0x21;
            map["PAGEDOWN"] = 0x22; map["PGDN"] = 0x22;
            map["LEFT"] = 0x25; map["UP"] = 0x26; map["RIGHT"] = 0x27; map["DOWN"] = 0x28;
            return map;
        }
    }
}
