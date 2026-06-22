using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace IDESK.Host;

internal sealed class HotkeyService : IDisposable
{
    private readonly HwndSource _source;
    private readonly Dictionary<int, Action> _hotkeys = [];
    private int _nextId;

    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;

    private const int WM_HOTKEY = 0x0312;

    public HotkeyService()
    {
        var p = new HwndSourceParameters("IDESK_HotkeyWindow")
        {
            WindowStyle = 0,
            ExtendedWindowStyle = 0,
            Width = 0,
            Height = 0,
        };
        _source = new HwndSource(p);
        _source.AddHook(WndProc);
    }

    public int Register(uint modifiers, Key key, Action callback)
    {
        if (!TryRegister(modifiers, key, callback, out int id))
            throw new InvalidOperationException($"Failed to register hotkey (modifiers={modifiers}, key={key})");
        return id;
    }

    public bool TryRegister(uint modifiers, Key key, Action callback, out int id)
    {
        int vk = KeyInterop.VirtualKeyFromKey(key);
        id = Interlocked.Increment(ref _nextId);

        if (!RegisterHotKey(_source.Handle, id, modifiers, (uint)vk))
        {
            id = 0;
            return false;
        }

        _hotkeys[id] = callback;
        return true;
    }

    public void Unregister(int id)
    {
        if (_hotkeys.Remove(id))
            UnregisterHotKey(_source.Handle, id);
    }

    public void Clear()
    {
        foreach (int id in _hotkeys.Keys)
            UnregisterHotKey(_source.Handle, id);
        _hotkeys.Clear();
    }

    public void Dispose()
    {
        Clear();
        _source.Dispose();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            if (_hotkeys.TryGetValue(id, out var callback))
                callback();
            handled = true;
        }
        return IntPtr.Zero;
    }

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(nint hWnd, int id);
}
