using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using HstMulligan.Core.Abstractions;
using HstMulligan.Core.Input;

namespace HstMulligan.Plugin.Input
{
    /// <summary>
    /// Registers a Win32 global hotkey against a message-only window and
    /// invokes a callback when WM_HOTKEY arrives. Works regardless of which
    /// process has foreground focus, which is the point of being
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
