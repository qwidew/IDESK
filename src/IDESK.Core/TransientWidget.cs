using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace IDESK.Core;

/// <summary>
/// 临时 Widget 基类：轻量浮动窗口，跨虚拟桌面可见，按需创建用完即毁。
/// 不包含书签停靠逻辑，提供关闭按钮与边缘缩放。
/// </summary>
public class TransientWidget : Window
{
    // ── NormalContent DP（子类注入主内容） ──

    public static readonly DependencyProperty NormalContentProperty =
        DependencyProperty.Register(nameof(NormalContent), typeof(UIElement), typeof(TransientWidget),
            new PropertyMetadata(null, (d, e) =>
            {
                var w = (TransientWidget)d;
                if (w._contentHost != null && e.NewValue is UIElement el)
                {
                    w._contentHost.Children.Clear();
                    w._contentHost.Children.Add(el);
                }
            }));

    public UIElement NormalContent
    {
        get => (UIElement)GetValue(NormalContentProperty);
        set => SetValue(NormalContentProperty, value);
    }

    // ── 跨虚拟桌面 ──

    private nint _winEventHook;
    private WinEventDelegate? _desktopSwitchDelegate;

    // ── 拖动 ──

    private bool _drag;
    private Point _dragLast;
    private double _dpiScale = 1.0;

    // ── 内容面板 ──

    private Grid _contentHost = null!;
    private Border _mainBody = null!;

    // ── 防止 Deactivated 重入 Close ──

    private bool _isClosing;

    // ── Win32 ──

    private const uint EVENT_SYSTEM_DESKTOP_SWITCH = 0x004E;
    private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
    private const int GWL_EXSTYLE = -20;
    private static readonly nint HWND_TOP = new(0);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_SHOWWINDOW = 0x0040;
    private const uint SWP_NOACTIVATE = 0x0010;

    [DllImport("user32.dll")]
    private static extern nint SetWinEventHook(uint eventMin, uint eventMax,
        nint hmodWinEventProc, WinEventDelegate lpfnFunced,
        uint idProcess, uint idThread, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool UnhookWinEvent(nint hWinEventHook);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter,
        int x, int y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(nint hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(nint hWnd, int nIndex, int dwNewLong);

    private delegate void WinEventDelegate(nint hWinEventHook, uint eventType,
        nint hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

    // ── 构造 ──

    public TransientWidget()
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        ResizeMode = ResizeMode.CanResize;
        Topmost = true;

        // 内容面板（全透明，只有子控件可见；圆角由子控件自行负责）
        _mainBody = new Border();
        _contentHost = new Grid();
        _mainBody.Child = _contentHost;

        // 关闭按钮（右上角）
        var closeBtn = new Button
        {
            Content = "✕",
            Width = 28,
            Height = 28,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(5),
            Cursor = Cursors.Hand
        };
        closeBtn.SetResourceReference(StyleProperty, "FadedButtonStyle");
        closeBtn.Click += (_, _) => Close();

        var root = new Grid();
        root.Children.Add(_mainBody);
        root.Children.Add(closeBtn);
        Content = root;

        StateChanged += (_, _) =>
        {
            if (WindowState == WindowState.Maximized)
                WindowState = WindowState.Normal;
        };

        // 无论什么方式触发 Close，都标记防止 Deactivated 重入
        Closing += (_, _) => _isClosing = true;

        // 读取全局配置：失焦自动关闭（防重入：Close 过程中可能再次触发 Deactivated）
        var transientCfg = TransientConfig.Load();
        if (transientCfg.AutoCloseOnDeactivated)
        {
            Deactivated += (_, _) =>
            {
                if (_isClosing || !IsLoaded) return;
                _isClosing = true;
                Close();
            };
        }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        var source = HwndSource.FromHwnd(hwnd)!;
        source.AddHook(WndProc);

        // DPI 缩放
        var t = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice;
        if (t.HasValue) _dpiScale = t.Value.M22;

        // 跨虚拟桌面：切换后把窗口置顶
        _desktopSwitchDelegate = OnDesktopSwitch;
        _winEventHook = SetWinEventHook(
            EVENT_SYSTEM_DESKTOP_SWITCH, EVENT_SYSTEM_DESKTOP_SWITCH,
            nint.Zero, _desktopSwitchDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);
        Closed += (_, _) =>
        {
            if (_winEventHook != nint.Zero)
                UnhookWinEvent(_winEventHook);
        };

        // 自动居中
        if (double.IsNaN(Left))
        {
            var wa = SystemParameters.WorkArea;
            Left = (wa.Width - Width) / 2;
            Top = (wa.Height - Height) / 2;
        }
    }

    private void OnDesktopSwitch(nint hWinEventHook, uint eventType, nint hwnd,
        int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        Dispatcher.Invoke(() =>
        {
            var h = new WindowInteropHelper(this).Handle;
            if (h != nint.Zero)
                SetWindowPos(h, HWND_TOP, 0, 0, 0, 0,
                    SWP_NOSIZE | SWP_NOMOVE | SWP_SHOWWINDOW | SWP_NOACTIVATE);
        });
    }

    // ── WndProc（边缘缩放） ──

    protected virtual IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_NCHITTEST = 0x0084,
                  HTLEFT = 10, HTRIGHT = 11, HTTOP = 12, HTTOPLEFT = 13, HTTOPRIGHT = 14,
                  HTBOTTOM = 15, HTBOTTOMLEFT = 16, HTBOTTOMRIGHT = 17;

        if (msg == WM_NCHITTEST)
        {
            int x = (short)((uint)lParam & 0xFFFF);
            int y = (short)(((uint)lParam >> 16) & 0xFFFF);
            var pt = PointFromScreen(new Point(x, y));

            const double edge = 8;
            bool l = pt.X <= edge, r = pt.X >= Width - edge;
            bool t = pt.Y <= edge, b = pt.Y >= Height - edge;

            if (t && l) { handled = true; return (IntPtr)HTTOPLEFT; }
            if (t && r) { handled = true; return (IntPtr)HTTOPRIGHT; }
            if (b && l) { handled = true; return (IntPtr)HTBOTTOMLEFT; }
            if (b && r) { handled = true; return (IntPtr)HTBOTTOMRIGHT; }
            if (t) { handled = true; return (IntPtr)HTTOP; }
            if (b) { handled = true; return (IntPtr)HTBOTTOM; }
            if (l) { handled = true; return (IntPtr)HTLEFT; }
            if (r) { handled = true; return (IntPtr)HTRIGHT; }
        }
        return IntPtr.Zero;
    }

    // ── 鼠标拖动 ──

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        _drag = true;
        _dragLast = PointToScreen(e.GetPosition(this));
        CaptureMouse();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_drag || e.LeftButton != MouseButtonState.Pressed) return;

        var cur = PointToScreen(e.GetPosition(this));
        double dx = (cur.X - _dragLast.X) / _dpiScale;
        double dy = (cur.Y - _dragLast.Y) / _dpiScale;

        Left += dx;
        Top += dy;

        _dragLast = cur;
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        _drag = false;
        ReleaseMouseCapture();
    }

    // ── 工具 ──

    protected static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T t) return t;
            var found = FindVisualChild<T>(child);
            if (found != null) return found;
        }
        return null;
    }
}
