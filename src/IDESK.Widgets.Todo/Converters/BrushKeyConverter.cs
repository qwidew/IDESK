using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace IDESK.Widgets.Todo.Converters;

/// <summary>将主题画笔资源名称解析为 SolidColorBrush。</summary>
public class BrushKeyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string key)
            return Application.Current.FindResource(key) as SolidColorBrush;
        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
