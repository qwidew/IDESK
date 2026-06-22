using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace IDESK.Core;

/// <summary>
/// 桌面 Widget 基类：跨虚拟桌面可见 + 拖到屏幕边缘变形为书签。
/// 子窗口用 NormalContent 属性注入正常内容。
/// 书签视觉树在 Themes/DeskWidget.xaml 的 BookmarkTemplate 中定义，可替换。
/// </summary>
public class DeskWidget : Window
{
    // ── NormalContent DP ──

    public static readonly DependencyProperty NormalContentProperty =
        DependencyProperty.Register(nameof(NormalContent), typeof(UIElement), typeof(DeskWidget),
            new PropertyMetadata(null, (d, e) =>
            {
                var w = (DeskWidget)d;
                if (w._normalHost != null && e.NewValue is UIElement el)
                {
                    w._normalHost.Children.Clear();
                    w._normalHost.Children.Add(el);
                }
            }));

    public UIElement NormalContent
    {
        get => (UIElement)GetValue(NormalContentProperty);
        set => SetValue(NormalContentProperty, value);
    }

    // ── 书签预设切换 ──

    private int _bookmarkPresetId = 11;
    public int BookmarkPresetId
    {
        get => _bookmarkPresetId;
        set
        {
            if (_bookmarkPresetId == value) return;
            _bookmarkPresetId = value;
            if (BookmarkContent != null) ReloadBookmark();
        }
    }

    // ── 书签视觉元素（由 XAML DataTemplate 填充） ──

    internal Grid BookmarkContent { get; set; } = null!;
    internal Path BookmarkTri { get; set; } = null!;
    internal Border BookmarkBody { get; set; } = null!;

    // ── UI 元素 ──

    private Grid _normalLayer = null!;
    private Grid _normalHost = null!;
    private Grid _bookmarkLayer = null!;
    private Border _mainBody = null!;

    // ── 状态 ──

    public bool IsBookmarkMode => _st != DockState.Normal;
    public event Action? BookmarkEntering;

    /// <summary>从已保存的状态恢复书签模式（窗口显示前调用）。</summary>
    public void RestoreBookmarkState(double savedLeft)
    {
        var wa = SystemParameters.WorkArea;
        _st = savedLeft < wa.Left + wa.Width / 2 ? DockState.Left : DockState.Right;
        UpdateBookmarkTextAlignment(_st);
        SaveNormalRect();

        double tabW = 12, bodyW = _bookmarkW - tabW;
        if (_st == DockState.Right)
        {
            BookmarkContent.ColumnDefinitions[0].Width = new GridLength(tabW);
            BookmarkContent.ColumnDefinitions[1].Width = new GridLength(bodyW);
            Grid.SetColumn(BookmarkTri, 0); Grid.SetColumn(BookmarkBody, 1);
            BookmarkTri.Data = Geometry.Parse(
                $"M {tabW},0 L 0,{_bookmarkH / 2} L {tabW},{_bookmarkH} Z");
        }
        else
        {
            BookmarkContent.ColumnDefinitions[0].Width = new GridLength(bodyW);
            BookmarkContent.ColumnDefinitions[1].Width = new GridLength(tabW);
            Grid.SetColumn(BookmarkTri, 1); Grid.SetColumn(BookmarkBody, 0);
            BookmarkTri.Data = Geometry.Parse(
                $"M 0,0 L {tabW},{_bookmarkH / 2} L 0,{_bookmarkH} Z");
        }

        double edgeX = _st == DockState.Left ? wa.Left : wa.Right - _bookmarkW;
        _savedBookmarkOffset = savedLeft - edgeX;
        Left = savedLeft;
        Top = Math.Clamp(Top, wa.Top, wa.Bottom - _bookmarkH);
        Width = _bookmarkW;
        Height = _bookmarkH;
        _normalLayer.Opacity = 0;
        _normalLayer.IsHitTestVisible = false;
        _bookmarkLayer.Opacity = 1;
    }

    private enum DockState { Normal, Left, Right }
    private DockState _st = DockState.Normal;

    private const double DT = 30;

    /// <summary>上次书签态的水平偏移记忆（退出时保存，再次进入时恢复）。</summary>
    private double _savedBookmarkOffset;

    /// <summary>书签实际尺寸（从当前模板读取）。</summary>
    private double _bookmarkW = 150;
    private double _bookmarkH = 50;

    /// <summary>Inward = 书签藏在屏幕内的部分，Outward = 书签边缘露在屏幕外的部分（默认 0 = 贴紧）。</summary>
    private double _bookmarkDragInward = 80;
    private double _bookmarkDragOutward = 0;

    // 附加属性 → 允许每个预设模板单独设置拖拽范围
    public static readonly DependencyProperty DragInwardProperty =
        DependencyProperty.RegisterAttached("DragInward", typeof(double), typeof(DeskWidget),
            new PropertyMetadata(80.0));
    public static void SetDragInward(Grid element, double value) => element.SetValue(DragInwardProperty, value);
    public static double GetDragInward(Grid element) => (double)element.GetValue(DragInwardProperty);

    public static readonly DependencyProperty DragOutwardProperty =
        DependencyProperty.RegisterAttached("DragOutward", typeof(double), typeof(DeskWidget),
            new PropertyMetadata(0.0));
    public static void SetDragOutward(Grid element, double value) => element.SetValue(DragOutwardProperty, value);
    public static double GetDragOutward(Grid element) => (double)element.GetValue(DragOutwardProperty);

    private static readonly Duration AD = new(TimeSpan.FromMilliseconds(200));

    private Rect _saved;

    private bool _drag;
    private Point _dragLast;
    private double _dpiScale = 1.0;
    private double _dragTot;

    // ── 跨虚拟桌面 ──

    private nint _winEventHook;
    private WinEventDelegate? _desktopSwitchDelegate;

    // ── 全局实例管理（用于一键隐藏/显示所有组件） ──

    public static List<DeskWidget> Instances { get; } = [];

    public static void ToggleAllVisibility()
    {
        bool anyVisible = Instances.Any(w => w.IsVisible);
        foreach (var w in Instances)
        {
            if (anyVisible) w.Hide();
            else w.Show();
        }
    }

    public static void ToggleAllTopmost()
    {
        foreach (var w in Instances) w.Topmost = true;
        foreach (var w in Instances) w.Topmost = false;
    }

    public static void MinimizeAllToBookmark()
    {
        var wa = SystemParameters.WorkArea;
        double center = wa.Left + wa.Width / 2;
        foreach (var w in Instances)
            w.MinimizeToBookmark(center);
    }

    public void MinimizeToBookmark(double screenCenter)
    {
        if (_st != DockState.Normal || !IsVisible) return;
        var side = Left + Width / 2 < screenCenter ? DockState.Left : DockState.Right;
        if (_st == DockState.Normal) EnterBookmark(side);
    }

    // ── 构造 ──

    public DeskWidget()
    {
        Instances.Add(this);
        Closed += (_, _) => Instances.Remove(this);

        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        ResizeMode = ResizeMode.CanResize;

        // 正常内容层
        _normalLayer = new Grid();
        _mainBody = new Border();
        _normalLayer.Children.Add(_mainBody);
        _normalHost = new Grid { Background = Brushes.Transparent };
        _normalLayer.Children.Add(_normalHost);

        // 书签预设切换按钮（左上）
        var bmBtn = new Button
        {
            Content = "◈",
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(5, 5, 5, 5),
            Cursor = Cursors.Hand
        };
        bmBtn.SetResourceReference(StyleProperty, "FadedButtonStyle");
        bmBtn.Click += (_, _) => PromptBookmarkPreset();
        _normalLayer.Children.Add(bmBtn);

        // 最小化按钮（右上角 → 收缩到最近边缘的书签）
        var minBtn = new Button
        {
            Content = "─",
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(5, 5, 15, 5),
            Cursor = Cursors.Hand
        };
        minBtn.SetResourceReference(StyleProperty, "FadedButtonStyle");
        minBtn.Click += (_, _) =>
        {
            if (_st != DockState.Normal) return;
            var wa = SystemParameters.WorkArea;
            double center = wa.Left + wa.Width / 2;
            EnterBookmark(Left + Width / 2 < center ? DockState.Left : DockState.Right);
        };
        _normalLayer.Children.Add(minBtn);

        // 书签从 XAML DataTemplate 加载
        _bookmarkLayer = new Grid { Opacity = 0, IsHitTestVisible = false };
        LoadBookmarkTemplate();
        _bookmarkLayer.Children.Add(BookmarkContent);

        DependencyPropertyDescriptor.FromProperty(TitleProperty, typeof(Window))
            ?.AddValueChanged(this, (_, _) => SyncBookmarkText());

        var root = new Grid();
        root.Children.Add(_normalLayer);
        root.Children.Add(_bookmarkLayer);
        Content = root;

        // 加载资源配色（动画用）与主窗口样式
        LoadResources();

        StateChanged += (_, _) => { if (WindowState == WindowState.Maximized) WindowState = WindowState.Normal; };
        SourceInitialized += OnFirstInit;
    }

    private void LoadBookmarkTemplate() => ApplyPreset(11);

    private void ReloadBookmark()
    {
        // 记住当前是否书签态
        bool wasBookmark = _st != DockState.Normal;
        _bookmarkLayer.Children.Clear();
        ApplyPreset(_bookmarkPresetId);
        _bookmarkLayer.Children.Add(BookmarkContent);
        if (wasBookmark) _st = DockState.Normal; // 重设状态，下次 CheckEdge 会重新进入
    }

    private void ApplyPreset(int presetId)
    {
        var key = $"Bookmark_{presetId}";
        if (TryFindResource(key) is DataTemplate template)
        {
            var root = (Grid)template.LoadContent();
            BookmarkContent = root;
            BookmarkTri = (Path)FindVisualChild(root, "BookmarkTri")!;
            BookmarkBody = (Border)FindVisualChild(root, "BookmarkBody")!;
            // 从模板读取实际尺寸与拖拽范围
            if (!double.IsNaN(BookmarkContent.Width)) _bookmarkW = BookmarkContent.Width;
            if (!double.IsNaN(BookmarkContent.Height)) _bookmarkH = BookmarkContent.Height;
            _bookmarkDragInward = GetDragInward(BookmarkContent);
            _bookmarkDragOutward = GetDragOutward(BookmarkContent);
        }
        else
        {
            // 缺省书签（无 XAML 资源时保底）
            BookmarkContent = new Grid { Width = _bookmarkW, Height = _bookmarkH };
            BookmarkContent.ColumnDefinitions.Add(new ColumnDefinition());
            BookmarkContent.ColumnDefinitions.Add(new ColumnDefinition());
            BookmarkTri = new Path();
            Grid.SetColumn(BookmarkTri, 0);
            BookmarkContent.Children.Add(BookmarkTri);
            BookmarkBody = new Border();
            Grid.SetColumn(BookmarkBody, 1);
            BookmarkBody.Child = new Grid();
            BookmarkContent.Children.Add(BookmarkBody);
        }
        SyncBookmarkText();
    }

    private static FrameworkElement? FindVisualChild(DependencyObject parent, string name)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is FrameworkElement fe && fe.Name == name) return fe;
            var found = FindVisualChild(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private void LoadResources()
    {
        if (TryFindResource("MainBodyStyle") is Style ms) _mainBody.Style = ms;
        else _mainBody.Background = Brushes.White;
    }

    private void OnFirstInit(object? sender, EventArgs e)
    {
        SourceInitialized -= OnFirstInit;
        var hwnd = new WindowInteropHelper(this).Handle;

        int ex = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, ex | WS_EX_NOACTIVATE);

        var hwndCopy = hwnd;
        PreviewMouseDown += (_, _) =>
            SetWindowLong(hwndCopy, GWL_EXSTYLE, GetWindowLong(hwndCopy, GWL_EXSTYLE) & ~WS_EX_NOACTIVATE);
        Deactivated += (_, _) =>
            SetWindowLong(hwndCopy, GWL_EXSTYLE, GetWindowLong(hwndCopy, GWL_EXSTYLE) | WS_EX_NOACTIVATE);

        var t = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice;
        if (t.HasValue) _dpiScale = t.Value.M22;

        _desktopSwitchDelegate = OnDesktopSwitch;
        _winEventHook = SetWinEventHook(
            EVENT_SYSTEM_DESKTOP_SWITCH, EVENT_SYSTEM_DESKTOP_SWITCH,
            nint.Zero, _desktopSwitchDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);
        Closed += (_, _) =>
        {
            if (_winEventHook != nint.Zero)
                UnhookWinEvent(_winEventHook);
        };

        if (double.IsNaN(Left))
        {
            var wa = SystemParameters.WorkArea;
            Left = (wa.Width - Width) / 2;
            Top = (wa.Height - Height) / 2;
        }
        if (_st == DockState.Normal)
            SaveNormalRect();
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

    private void SaveNormalRect() => _saved = new Rect(Left, Top, Width, Height);

    /// <summary>当前书签态的 X 拖拽范围（绝对屏幕坐标）。
    /// Inward = 书签收入屏幕内的部分，Outward = 书签突出屏幕外的部分。</summary>
    private (double min, double max) BookmarkXRange
    {
        get
        {
            var wa = SystemParameters.WorkArea;
            if (_st == DockState.Left)
            {
                double edge = wa.Left;
                return (edge - _bookmarkDragInward, edge + _bookmarkDragOutward);
            }
            else
            {
                double edge = wa.Right - _bookmarkW;
                return (edge - _bookmarkDragOutward, edge + _bookmarkDragInward);
            }
        }
    }

    // ── Win32 ──

    private const uint EVENT_SYSTEM_DESKTOP_SWITCH = 0x004E;
    private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_NOACTIVATE = 0x08000000;

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

    // ── WndProc（边缘缩放） ──

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        var source = HwndSource.FromHwnd(hwnd)!;
        source.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_NCHITTEST = 0x0084,
                  HTLEFT = 10, HTRIGHT = 11, HTTOP = 12, HTTOPLEFT = 13, HTTOPRIGHT = 14,
                  HTBOTTOM = 15, HTBOTTOMLEFT = 16, HTBOTTOMRIGHT = 17;

        if (msg == WM_NCHITTEST && _st == DockState.Normal)
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

    // ── 鼠标 ──

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        _drag = true;
        _dragLast = PointToScreen(e.GetPosition(this));
        _dragTot = 0;
        CaptureMouse();
    }



    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_drag || e.LeftButton != MouseButtonState.Pressed) return;

        var cur = PointToScreen(e.GetPosition(this));
        double dx = (cur.X - _dragLast.X) / _dpiScale;
        double dy = (cur.Y - _dragLast.Y) / _dpiScale;

        Left = _st == DockState.Normal
            ? Left + dx
            : Math.Clamp(Left + dx, BookmarkXRange.min, BookmarkXRange.max);
        Top += dy;

        _dragTot += Math.Abs(dx) + Math.Abs(dy);
        _dragLast = cur;
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (!_drag) return;
        _drag = false;
        ReleaseMouseCapture();

        if (_st == DockState.Normal)
        {
            ClampVertical();
            SaveNormalRect();
            if (_dragTot > 5) CheckEdge();
        }
        else
        {
            if (_dragTot < 5) { ExitBookmark(); return; }
            Left = Math.Clamp(Left, BookmarkXRange.min, BookmarkXRange.max);
            ClampVertical();
        }
    }

    private void CheckEdge()
    {
        if (_st != DockState.Normal) return;
        var wa = SystemParameters.WorkArea;
        if (Left <= wa.Left + DT) EnterBookmark(DockState.Left);
        else if (Left + Width >= wa.Right - DT) EnterBookmark(DockState.Right);
    }

    private void ClampVertical()
    {
        var wa = SystemParameters.WorkArea;
        if (Top < wa.Top) Top = wa.Top;
        else if (Top + Height > wa.Bottom) Top = wa.Bottom - Height;
    }

    // ── 出入书签 ──

    private void EnterBookmark(DockState side)
    {
        BookmarkEntering?.Invoke();
        SaveNormalRect();
        _st = side;
        var wa = SystemParameters.WorkArea;

        double tabW = 12, bodyW = _bookmarkW - tabW;
        if (side == DockState.Right)
        {
            BookmarkContent.ColumnDefinitions[0].Width = new GridLength(tabW);
            BookmarkContent.ColumnDefinitions[1].Width = new GridLength(bodyW);
            Grid.SetColumn(BookmarkTri, 0); Grid.SetColumn(BookmarkBody, 1);
            BookmarkTri.Data = Geometry.Parse(
                $"M {tabW},0 L 0,{_bookmarkH / 2} L {tabW},{_bookmarkH} Z");
        }
        else
        {
            BookmarkContent.ColumnDefinitions[0].Width = new GridLength(bodyW);
            BookmarkContent.ColumnDefinitions[1].Width = new GridLength(tabW);
            Grid.SetColumn(BookmarkTri, 1); Grid.SetColumn(BookmarkBody, 0);
            BookmarkTri.Data = Geometry.Parse(
                $"M 0,0 L {tabW},{_bookmarkH / 2} L 0,{_bookmarkH} Z");
        }

        double cy = _saved.Y + _saved.Height / 2;
        double tTop = cy - _bookmarkH / 2;
        tTop = Math.Max(wa.Top, Math.Min(tTop, wa.Bottom - _bookmarkH));
        UpdateBookmarkTextAlignment(side);

        double edgeX = side == DockState.Left ? wa.Left : wa.Right - _bookmarkW;
        double tLeft = Math.Clamp(edgeX + _savedBookmarkOffset, BookmarkXRange.min, BookmarkXRange.max);

        var fadeOut = new Storyboard();
        Fade(fadeOut, _normalLayer, 0);
        fadeOut.Completed += (_, _) =>
        {
            Left = tLeft; Top = tTop; Width = _bookmarkW; Height = _bookmarkH;
            ClampVertical();
            _normalLayer.IsHitTestVisible = false;
            _bookmarkLayer.BeginAnimation(OpacityProperty, new DoubleAnimation(1, AD));
        };
        fadeOut.Begin();
    }

    private void ExitBookmark()
    {
        var wa = SystemParameters.WorkArea;
        double edgeX = _st == DockState.Left ? wa.Left : wa.Right - _bookmarkW;
        _savedBookmarkOffset = Left - edgeX;

        _st = DockState.Normal;

        var fadeBook = new DoubleAnimation(0, new Duration(TimeSpan.FromMilliseconds(80)));
        fadeBook.Completed += (_, _) =>
        {
            Left = _saved.X;
            double cy = Top + _bookmarkH / 2; // 书签中心
            _saved.Y = cy - _saved.Height / 2;
            Top = _saved.Y;
            Width = _saved.Width; Height = _saved.Height;
            ClampVertical();
            _bookmarkLayer.Opacity = 0;
            _normalLayer.IsHitTestVisible = true;
            _normalLayer.BeginAnimation(OpacityProperty, new DoubleAnimation(1, AD));
        };
        _bookmarkLayer.BeginAnimation(OpacityProperty, fadeBook);
    }

    private void SyncBookmarkText()
    {
        var tb = FindVisualChild<TextBlock>(BookmarkBody);
        if (tb != null)
            tb.Text = Title;
    }

    private void UpdateBookmarkTextAlignment(DockState side)
    {
        var tb = FindVisualChild<TextBlock>(BookmarkBody);
        if (tb != null)
        {
            tb.HorizontalAlignment = side == DockState.Left
                ? HorizontalAlignment.Right
                : HorizontalAlignment.Left;
            tb.Margin = side == DockState.Left
                ? new Thickness(0, 0, 12, 0)
                : new Thickness(12, 0, 0, 0);
        }
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
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

    private void PromptBookmarkPreset()
    {
        var win = new Window
        {
            Title = "选择书签样式",
            Width = 540, Height = 400,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            ResizeMode = ResizeMode.NoResize,
            Background = Brushes.Transparent,
            AllowsTransparency = true,
            WindowStyle = WindowStyle.None,
        };

        var outer = new Border { CornerRadius = new CornerRadius(10) };
        outer.SetResourceReference(BackgroundProperty, "CardBackgroundBrush");
        win.Content = outer;

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Padding = new Thickness(10, 12, 10, 12),
        };
        if (TryFindResource("ThinScrollBarStyle") is Style thinSb)
            scroll.Resources.Add(typeof(ScrollBar), thinSb);
        outer.Child = scroll;

        var root = new StackPanel();

        // 右上角关闭按钮
        var closeBtn = new Button
        {
            Content = "×", Width = 26, Height = 26,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, -4, -4, 0),
            Cursor = Cursors.Hand, FontSize = 14,
        };
        closeBtn.SetResourceReference(StyleProperty, "BorderlessButtonStyle");
        closeBtn.Click += (_, _) => win.Close();

        var closeLayer = new Grid();
        closeLayer.Children.Add(root);
        closeLayer.Children.Add(closeBtn);
        scroll.Content = closeLayer;

        // 分组
        int[] groups = [0, 1, 11, 21, 31, 51];
        string[] groupLabels = ["默认", "特殊形状", "纯色系列", "深色系列", "赛博朋克", "涂鸦"];

        for (int g = 0; g < groups.Length; g++)
        {
            int start = groups[g];
            int end = (g < groups.Length - 1 ? groups[g + 1] : 81) - 1;

            // 分隔线（除第一组外）
            if (g > 0)
            {
                root.Children.Add(new Rectangle
                {
                    Height = 1, Fill = Brushes.Gray, Opacity = 0.25,
                    Margin = new Thickness(0, 4, 0, 8),
                });
            }

            // 组标题
            root.Children.Add(new TextBlock
            {
                Text = groupLabels[g], FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.Gray, Margin = new Thickness(2, 0, 0, 6),
            });

            // 书签网格
            var wrap = new WrapPanel { Orientation = Orientation.Horizontal };

            for (int i = start; i <= end; i++)
            {
                int id = i;
                var template = TryFindResource($"Bookmark_{id}") as DataTemplate;

                var card = new Border
                {
                    Width = 162, Margin = new Thickness(0, 0, 6, 6),
                    CornerRadius = new CornerRadius(6),
                    Cursor = Cursors.Hand,
                    Padding = new Thickness(4),
                };
                card.SetResourceReference(BackgroundProperty, "ItemBgBrush");
                if (id == BookmarkPresetId)
                    card.SetResourceReference(Border.BorderBrushProperty, "AccentBrush");
                card.BorderThickness = new Thickness(id == BookmarkPresetId ? 2 : 0);

                var stack = new StackPanel();

                if (template != null)
                {
                    var preview = (FrameworkElement)template.LoadContent();
                    preview.Width = 150; preview.Height = 22;
                    preview.Margin = new Thickness(0, 0, 0, 2);
                    preview.HorizontalAlignment = HorizontalAlignment.Center;
                    stack.Children.Add(preview);
                }

                stack.Children.Add(new TextBlock
                {
                    Text = $"#{id}", FontSize = 11,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = Brushes.Gray,
                });

                card.Child = stack;
                card.MouseDown += (_, _) => { BookmarkPresetId = id; win.Close(); };
                wrap.Children.Add(card);
            }

            root.Children.Add(wrap);
        }

        win.ShowDialog();
    }

    private static void Fade(Storyboard sb, UIElement el, double to)
    {
        var a = new DoubleAnimation(to, AD) { EasingFunction = new QuadraticEase() };
        Storyboard.SetTarget(a, el); Storyboard.SetTargetProperty(a, new PropertyPath(OpacityProperty));
        sb.Children.Add(a);
    }

}
