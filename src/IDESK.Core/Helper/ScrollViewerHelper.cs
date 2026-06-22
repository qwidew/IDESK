using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Threading;

namespace IDESK.Core.Helper;

public static class ScrollViewerHelper
{
    public static readonly DependencyProperty AutoHideScrollBarsProperty =
        DependencyProperty.RegisterAttached("AutoHideScrollBars", typeof(bool), typeof(ScrollViewerHelper),
            new PropertyMetadata(false, OnAutoHideChanged));

    public static void SetAutoHideScrollBars(ScrollViewer element, bool value) =>
        element.SetValue(AutoHideScrollBarsProperty, value);

    public static bool GetAutoHideScrollBars(ScrollViewer element) =>
        (bool)element.GetValue(AutoHideScrollBarsProperty);

    private static readonly TimeSpan _delay = TimeSpan.FromSeconds(1.5);

    private static void OnAutoHideChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ScrollViewer sv) return;

        if ((bool)e.NewValue)
        {
            sv.Loaded += OnLoaded;
            sv.Unloaded += OnUnloaded;
        }
        else
        {
            sv.Loaded -= OnLoaded;
            sv.Unloaded -= OnUnloaded;
            Cleanup(sv);
        }
    }

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not ScrollViewer sv) return;
        sv.ScrollChanged += OnScrollChanged;
    }

    private static void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is not ScrollViewer sv) return;
        sv.ScrollChanged -= OnScrollChanged;
        Cleanup(sv);
    }

    private static readonly Dictionary<ScrollViewer, DispatcherTimer> _timers = [];

    private static void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (sender is not ScrollViewer sv) return;

        if (_timers.TryGetValue(sv, out var timer))
        {
            timer.Stop();
            // 恢复透明度（取消可能正在进行的淡化动画）
            var bar = FindScrollBar(sv);
            if (bar != null)
            {
                bar.BeginAnimation(UIElement.OpacityProperty, null);
                bar.Opacity = 1;
            }
        }
        else
        {
            timer = new DispatcherTimer(_delay, DispatcherPriority.Normal, (_, _) => FadeOut(sv), sv.Dispatcher);
            _timers[sv] = timer;
            var bar = FindScrollBar(sv);
            if (bar != null) bar.Opacity = 1;
        }

        timer.Start();
    }

    private static void FadeOut(ScrollViewer sv)
    {
        if (_timers.TryGetValue(sv, out var timer))
        {
            timer.Stop();
            _timers.Remove(sv);
        }

        var bar = FindScrollBar(sv);
        if (bar == null || bar.Opacity == 0) return;

        var anim = new DoubleAnimation(0, new Duration(TimeSpan.FromMilliseconds(400)))
        {
            EasingFunction = new QuadraticEase()
        };
        bar.BeginAnimation(UIElement.OpacityProperty, anim);
    }

    private static ScrollBar? FindScrollBar(ScrollViewer sv)
    {
        // 尝试取模板中的垂直滚动条
        if (sv.Template?.FindName("PART_VerticalScrollBar", sv) is ScrollBar vertical)
            return vertical;
        return null;
    }

    private static void Cleanup(ScrollViewer sv)
    {
        if (_timers.TryGetValue(sv, out var timer))
        {
            timer.Stop();
            _timers.Remove(sv);
        }
        var bar = FindScrollBar(sv);
        if (bar != null)
        {
            bar.BeginAnimation(UIElement.OpacityProperty, null);
            bar.Opacity = 1;
        }
    }
}
