using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace HakoMark.Windows;

public sealed class GlobalHotkeyService : IDisposable
{
    private const int WmHotkey = 0x0312, ModControl = 0x0002, ModShift = 0x0004, ActivationId = 0x484D;
    private HwndSource? _source;
    private const int CustomId = 0x484E;
    private string? customKey;
    private int registeredId = CustomId;
    public bool ConfigureActivation(string key)
    {
        if (_source is null) return false;
        if (customKey == key) return true;
        if (!Enum.TryParse<System.Windows.Input.Key>(key,out var parsed) || parsed == System.Windows.Input.Key.None) return false;
        var nextId=registeredId==CustomId ? CustomId+1 : CustomId;
        if (!RegisterHotKey(_source.Handle, nextId, 0x4000, (uint)System.Windows.Input.KeyInterop.VirtualKeyFromKey(parsed))) return false;
        UnregisterHotKey(_source.Handle, registeredId);
        registeredId=nextId;
        customKey=key;
        return true;
    }
    public event EventHandler? ActivationPressed;
    public event Action? OpenRequested;
    public static void OpenExisting() {
        var hwnd=FindWindow(null,"Hako Mark — Barre d’annotation");
        if(hwnd!=IntPtr.Zero) PostMessage(hwnd,0x8001,IntPtr.Zero,IntPtr.Zero);
    }
    public event Action<char>? ToolPressed;

    public void Attach(System.Windows.Window window)
    {
        var handle = new WindowInteropHelper(window).EnsureHandle(); _source = HwndSource.FromHwnd(handle); _source.AddHook(WndProc);
        RegisterHotKey(handle, ActivationId, ModControl | ModShift | 0x4000, (uint)System.Windows.Input.KeyInterop.VirtualKeyFromKey(System.Windows.Input.Key.A));
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if(msg==0x8001) { OpenRequested?.Invoke(); handled=true; }
        if (msg == WmHotkey && (wParam.ToInt32() == ActivationId || wParam.ToInt32() == registeredId)) { ActivationPressed?.Invoke(this, EventArgs.Empty); handled = true; }
        return IntPtr.Zero;
    }

    public void ProcessToolKey(System.Windows.Input.Key key, bool annotationMode)
    {
        if (!annotationMode) return;
        var letter = key switch { System.Windows.Input.Key.A => 'A', System.Windows.Input.Key.C => 'C', System.Windows.Input.Key.R => 'R', System.Windows.Input.Key.L => 'L', System.Windows.Input.Key.P => 'P', _ => '\0' };
        if (letter != '\0') ToolPressed?.Invoke(letter);
    }

    public void Dispose()
    {
        if (_source is null) return; UnregisterHotKey(_source.Handle, ActivationId); UnregisterHotKey(_source.Handle,registeredId); _source.RemoveHook(WndProc); _source = null;
    }

    [DllImport("user32.dll", SetLastError = true)] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] private static extern IntPtr FindWindow(string? className,string title);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr hwnd,int message,IntPtr w,IntPtr l);
}

public static class DpiBootstrap
{
    [DllImport("user32.dll")] private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr dpiContext);
    public static void EnablePerMonitorDpi() => SetThreadDpiAwarenessContext(new IntPtr(-4));
}
