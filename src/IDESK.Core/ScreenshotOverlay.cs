using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace IDESK.Core;

public sealed class ScreenshotOverlay : Window
{
    private Point _start;
    private Point _end;
    private bool _isDragging;
    private double _dpiScale = 1.0;

    private readonly Rectangle _topMask;
    private readonly Rectangle _bottomMask;
    private readonly Rectangle _leftMask;
    private readonly Rectangle _rightMask;
    private readonly Rectangle _selectionBorder;

    public event Action<Rect>? RegionSelected;

    public ScreenshotOverlay()
    {
        Title = "截图";
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        Topmost = true;
        ShowInTaskbar = false;
        ResizeMode = ResizeMode.NoResize;
        Cursor = Cursors.Cross;

        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;

        var overlayColor = Color.FromArgb(120, 0, 0, 0);

        double sw = Width, sh = Height;
        _topMask = new Rectangle { Fill = new SolidColorBrush(overlayColor), Width = sw, Height = sh };
        _bottomMask = new Rectangle { Fill = new SolidColorBrush(overlayColor) };
        _leftMask = new Rectangle { Fill = new SolidColorBrush(overlayColor) };
        _rightMask = new Rectangle { Fill = new SolidColorBrush(overlayColor) };

        _selectionBorder = new Rectangle
        {
            Stroke = Brushes.White,
            StrokeThickness = 1,
            StrokeDashArray = [3, 3],
            Visibility = Visibility.Collapsed,
        };

        var canvas = new Canvas();
        canvas.Children.Add(_topMask);
        canvas.Children.Add(_bottomMask);
        canvas.Children.Add(_leftMask);
        canvas.Children.Add(_rightMask);
        canvas.Children.Add(_selectionBorder);
        Content = canvas;

        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape) Close();
        };
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var t = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice;
        _dpiScale = t?.M11 ?? 1.0;
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        _start = e.GetPosition(this);
        _isDragging = true;
        CaptureMouse();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_isDragging) return;

        _end = e.GetPosition(this);
        UpdateMasks();
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        ReleaseMouseCapture();
        _isDragging = false;

        _end = e.GetPosition(this);
        double x = Math.Min(_start.X, _end.X);
        double y = Math.Min(_start.Y, _end.Y);
        double w = Math.Abs(_end.X - _start.X);
        double h = Math.Abs(_end.Y - _start.Y);

        if (w > 10 && h > 10)
        {
            double absX = (Left + x) * _dpiScale;
            double absY = (Top + y) * _dpiScale;
            RegionSelected?.Invoke(new Rect(absX, absY, w * _dpiScale, h * _dpiScale));
        }

        Close();
    }

    private void UpdateMasks()
    {
        double sx = Math.Min(_start.X, _end.X);
        double sy = Math.Min(_start.Y, _end.Y);
        double ex = Math.Max(_start.X, _end.X);
        double ey = Math.Max(_start.Y, _end.Y);
        double sw = Width, sh = Height;

        // 四边遮罩，中间选区留空
        _topMask.SetValue(Canvas.LeftProperty, 0.0);
        _topMask.SetValue(Canvas.TopProperty, 0.0);
        _topMask.Width = sw;
        _topMask.Height = sy;

        _bottomMask.SetValue(Canvas.LeftProperty, 0.0);
        _bottomMask.SetValue(Canvas.TopProperty, ey);
        _bottomMask.Width = sw;
        _bottomMask.Height = sh - ey;

        _leftMask.SetValue(Canvas.LeftProperty, 0.0);
        _leftMask.SetValue(Canvas.TopProperty, sy);
        _leftMask.Width = sx;
        _leftMask.Height = ey - sy;

        _rightMask.SetValue(Canvas.LeftProperty, ex);
        _rightMask.SetValue(Canvas.TopProperty, sy);
        _rightMask.Width = sw - ex;
        _rightMask.Height = ey - sy;

        // 选区边框
        _selectionBorder.Visibility = Visibility.Visible;
        _selectionBorder.SetValue(Canvas.LeftProperty, sx);
        _selectionBorder.SetValue(Canvas.TopProperty, sy);
        _selectionBorder.Width = ex - sx;
        _selectionBorder.Height = ey - sy;
    }
}
