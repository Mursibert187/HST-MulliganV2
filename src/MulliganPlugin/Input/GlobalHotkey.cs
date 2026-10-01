using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using HstMulligan.Core.Abstractions;

namespace HstMulligan.Plugin.Input
{
    [Flags]
    internal enum HotkeyModifiers : uint
    {
        None = 0,
        Alt = 0x1,
        Control = 0x2,
        Shift = 0x4,
        Win = 0x8,
    }

    internal readonly struct HotkeyBinding
    {
        public HotkeyModifiers Modifiers { get; }
        public uint VirtualKey { get; }
        public bool IsValid => VirtualKey != 0;
        public HotkeyBinding(HotkeyModifiers mods, uint vk) { Modifiers = mods; VirtualKey = vk; }
        public static readonly HotkeyBinding None = new HotkeyBinding(HotkeyModifiers.None, 0);
    }

    internal static class HotkeyParser
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
                switch (p)
                {
                    case "CTRL":
                    case "CONTROL":  mods |= HotkeyModifiers.Control; break;
                    case "SHIFT":    mods |= HotkeyModifiers.Shift;   break;
                    case "ALT":      mods |= HotkeyModifiers.Alt;     break;
                    case "WIN":
                    case "META":
                    case "SUPER":    mods |= HotkeyModifiers.Win;     break;
                    default:
                        if (_namedKeys.TryGetValue(p, out var mapped)) vk = mapped;
                        else if (p.Length == 1)
                        {
                            var ch = p[0];
                            if (ch >= 'A' && ch <= 'Z') vk = ch;
                            else if (ch >= '0' && ch <= '9') vk = ch;
                        }
                        break;
                }
            }
            return new HotkeyBinding(mods, vk);
        }

        private static Dictionary<string, uint> BuildNamedKeys()
        {
            var map = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
            for (int i = 1; i <= 24; i++) map["F" + i] = (uint)(0x6F + i); // F1..F24 = 0x70..0x87
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

    /// <summary>
    /// Registers a Win32 global hotkey against a message-only window and
    /// invokes a callback when WM_HOTKEY arrives. Works regardless of which
    /// process has foreground focus, which is the point of making it
    /// &quot;global&quot; rather than a WPF InputBinding.
    /// </summary>
    internal sealed class GlobalHotkey : IDisposable
    {
        private const int WM_HOTKEY = 0x0312;
        private static readonly IntPtr HWND_MESSAGE = new IntPtr(-3);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private readonly ILogger _log;
        private HwndSource _hwndSource;
        private int _nextId = 9000;
        private readonly Dictionary<int, Action> _handlers = new Dictionary<int, Action>();

        public GlobalHotkey(ILogger log = null)
        {
            _log = log ?? NullLogger.Instance;
            var parameters = new HwndSourceParameters("HstMulliganV2.HotkeyBridge")
            {
                WindowStyle = 0,
                ExtendedWindowStyle = 0,
                ParentWindow = HWND_MESSAGE,
            };
            _hwndSource = new HwndSource(parameters);
            _hwndSource.AddHook(WndProc);
        }

        public bool Register(string shortcut, Action onPressed)
        {
            if (onPressed == null) return false;
            var binding = HotkeyParser.Parse(shortcut);
            if (!binding.IsValid)
            {
                _log.Warn($"GlobalHotkey: unparseable shortcut '{shortcut}'");
                return false;
            }
            var id = _nextId++;
            if (!RegisterHotKey(_hwndSource.Handle, id, (uint)binding.Modifiers, binding.VirtualKey))
            {
                _log.Warn($"GlobalHotkey: RegisterHotKey failed for '{shortcut}' (error {Marshal.GetLastWin32Error()})");
                return false;
            }
            _handlers[id] = onPressed;
            _log.Info($"GlobalHotkey: '{shortcut}' bound as id {id}");
            return true;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg != WM_HOTKEY) return IntPtr.Zero;
            var id = wParam.ToInt32();
            if (_handlers.TryGetValue(id, out var cb))
            {
                try { cb(); } catch (Exception ex) { _log.Warn("GlobalHotkey handler threw", ex); }
                handled = true;
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            if (_hwndSource == null) return;
            foreach (var id in _handlers.Keys)
            {
                try { UnregisterHotKey(_hwndSource.Handle, id); } catch { }
            }
            _handlers.Clear();
            _hwndSource.RemoveHook(WndProc);
            _hwndSource.Dispose();
            _hwndSource = null;
        }
    }
}
