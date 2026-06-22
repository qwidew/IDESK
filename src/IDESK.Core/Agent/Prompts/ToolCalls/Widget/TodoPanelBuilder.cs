using System.Windows;
using System.Windows.Controls;
namespace IDESK.Core.Agent.Prompts.ToolCalls.Widget;

public static class TodoPanelBuilder
{
    /// <summary>工具调用面板：显示 AI 正在调用什么。</summary>
    public static UIElement BuildCallPanel(TodoAction action)
    {
        var stack = new StackPanel { Margin = new Thickness(0, 0, 0, 6) };

        var title = new TextBlock
        {
            Text = $"🔧 调用：{action.Action}",
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 4),
        }.WithFg("TextSecondaryBrush");

        var details = new TextBlock
        {
            Text = string.Join("  ", action.Params.Select(p => $"{p.Key}={p.Value}")),
            FontSize = 11,
        }.WithFg("TextSecondaryBrush");

        stack.Children.Add(title);
        stack.Children.Add(details);
        return stack;
    }

    /// <summary>执行结果面板：显示操作结果。</summary>
    public static UIElement BuildResultPanel(string result)
    {
        var border = new Border
        {
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 6, 10, 6),
            Margin = new Thickness(0, 0, 0, 10),
        };
        border.SetResourceReference(Border.BackgroundProperty, "SectionBgBrush");

        border.Child = new TextBlock
        {
            Text = result,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
        }.WithFg("TextSecondaryBrush");

        return border;
    }
}

internal static class TbExt
{
    public static T WithFg<T>(this T tb, string key) where T : TextBlock
    {
        tb.SetResourceReference(TextBlock.ForegroundProperty, key);
        return tb;
    }
}
