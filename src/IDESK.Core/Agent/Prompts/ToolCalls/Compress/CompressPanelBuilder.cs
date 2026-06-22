using System.Windows;
using System.Windows.Controls;

namespace IDESK.Core.Agent.Prompts.ToolCalls.Compress;

public static class CompressPanelBuilder
{
    public static UIElement Build(string background)
    {
        var border = new Border
        {
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(0, 0, 0, 10),
        };
        border.SetResourceReference(Border.BackgroundProperty, "SectionBgBrush");

        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = "📋 对话已压缩",
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 6),
        }.WithFg("TextPrimaryBrush"));

        stack.Children.Add(new TextBlock
        {
            Text = background,
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
        }.WithFg("TextSecondaryBrush"));

        border.Child = stack;
        return border;
    }

    private static T WithFg<T>(this T tb, string key) where T : TextBlock
    {
        tb.SetResourceReference(TextBlock.ForegroundProperty, key);
        return tb;
    }
}
