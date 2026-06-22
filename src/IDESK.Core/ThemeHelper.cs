using System.Windows;
using System.Windows.Controls;

namespace IDESK.Core;

/// <summary>
/// 附加属性工具，让代码创建的 UI 或 DataTemplate 中的 Foreground 使用 DynamicResource，
/// 从而实现切换主题时自动更新颜色。
/// </summary>
public static class ThemeHelper
{
    public static readonly DependencyProperty ForegroundKeyProperty =
        DependencyProperty.RegisterAttached("ForegroundKey", typeof(string), typeof(ThemeHelper),
            new PropertyMetadata(null, OnForegroundKeyChanged));

    public static string? GetForegroundKey(DependencyObject obj) => (string)obj.GetValue(ForegroundKeyProperty);
    public static void SetForegroundKey(DependencyObject obj, string? value) => obj.SetValue(ForegroundKeyProperty, value);

    private static void OnForegroundKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not string key || string.IsNullOrEmpty(key)) return;
        if (d is TextBlock tb)
            tb.SetResourceReference(TextBlock.ForegroundProperty, key);
        else if (d is Control ctrl)
            ctrl.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, key);
    }
}
