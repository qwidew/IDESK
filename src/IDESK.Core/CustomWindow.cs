using System.Windows;
using System.Windows.Interop;
using System.Windows.Shell;

namespace IDESK.Core;

public class CustomWindow : Window
{
    public CustomWindow()
    {
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.CanResize;

        WindowChrome.SetWindowChrome(this, new WindowChrome
        {
            CaptionHeight = 0,
            CornerRadius = new CornerRadius(8),
            GlassFrameThickness = new Thickness(0),
            ResizeBorderThickness = new Thickness(6),
            UseAeroCaptionButtons = false,
        });
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        var source = HwndSource.FromHwnd(hwnd)!;
        source.AddHook(WndProc);
    }

    protected void DragWindow()
    {
        if (WindowState == WindowState.Maximized) return;
        DragMove();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_LBUTTONDBLCLK = 0x0203;

        if (msg == WM_LBUTTONDBLCLK)
        {
            var pt = PointFromScreen(new Point(
                (short)((uint)lParam & 0xFFFF),
                (short)(((uint)lParam >> 16) & 0xFFFF)));

            if (pt.Y >= 0 && pt.Y <= 36)
            {
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                handled = true;
            }
        }

        return IntPtr.Zero;
    }
}
