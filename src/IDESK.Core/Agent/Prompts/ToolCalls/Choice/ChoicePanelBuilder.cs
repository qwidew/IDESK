using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace IDESK.Core.Agent.Prompts.ToolCalls.Choice;

public static class ChoicePanelBuilder
{
    public static UIElement Build(ChoiceData choice, Action<string> onSelect)
    {
        var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };

        // 题目
        panel.Children.Add(new TextBlock
        {
            Text = choice.Question,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8),
        }.WithFg("TextPrimaryBrush"));

        var keys = choice.Options.Keys.OrderBy(k => k).ToList();

        foreach (var key in keys)
        {
            var opt = choice.Options[key];
            var border = new Border
            {
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(0, 2, 0, 2),
                CornerRadius = new CornerRadius(4),
                Cursor = Cursors.Hand,
                Tag = key,
            };
            border.SetResourceReference(Border.BackgroundProperty, "ItemBgBrush");

            border.MouseEnter += (_, _) =>
                border.SetResourceReference(Border.BackgroundProperty, "ItemHoverBgBrush");
            border.MouseLeave += (_, _) =>
                border.SetResourceReference(Border.BackgroundProperty, "ItemBgBrush");

            var textBlock = new TextBlock
            {
                Text = $"{key}. {opt}",
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
            }.WithFg("TextPrimaryBrush");

            border.Child = textBlock;
            border.MouseDown += (_, _) => onSelect(key);

            panel.Children.Add(border);
        }

        return panel;
    }

    private static T WithFg<T>(this T tb, string brushKey) where T : TextBlock
    {
        tb.SetResourceReference(TextBlock.ForegroundProperty, brushKey);
        return tb;
    }
}
