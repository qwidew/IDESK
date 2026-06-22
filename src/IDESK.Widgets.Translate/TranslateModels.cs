using System.Windows;

namespace IDESK.Widgets.Translate;

public class TranslateDisplayItem
{
    public string Text { get; set; } = "";
    public FontWeight Weight { get; set; } = FontWeights.Normal;
    public double FontSize { get; set; } = 14;
    public Thickness Margin { get; set; } = new(0, 2, 0, 2);
    /// <summary>主题资源名称（如 "Blue9Brush"），由 ThemeHelper 解析为 DynamicResource。</summary>
    public string? ForegroundKey { get; set; }
}
