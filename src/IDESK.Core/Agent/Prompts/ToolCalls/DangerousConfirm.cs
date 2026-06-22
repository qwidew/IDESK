using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace IDESK.Core.Agent.Prompts.ToolCalls;

public static class DangerousConfirm
{
    private static readonly HashSet<string> DangerousActions =
    [
        "DeleteTodo", "DeleteGroup", "ClearContext", "Compress"
    ];

    public static bool IsDangerous(string action) => DangerousActions.Contains(action);

    /// <summary>
    /// 弹出一个内嵌确认面板，返回 true 表示用户确认，false 表示取消。
    /// </summary>
    public static Task<bool> ShowAsync(Panel parent, string message)
    {
        var tcs = new TaskCompletionSource<bool>();

        var border = new Border
        {
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(0, 0, 0, 10),
        };
        border.SetResourceReference(Border.BackgroundProperty, "DangerBgBrush");

        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = "⚠️ 危险操作确认",
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 6),
        }.WithFg("DangerBrush"));

        stack.Children.Add(new TextBlock
        {
            Text = message,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8),
        }.WithFg("TextPrimaryBrush"));

        var btnPanel = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
        };

        var confirmBtn = new Button
        {
            Content = "确认",
            Width = 70,
            Height = 28,
            Margin = new Thickness(0, 0, 8, 0),
            Cursor = Cursors.Hand,
        };
        confirmBtn.Style = (Style)Application.Current.FindResource("DangerButtonStyle");
        confirmBtn.Click += (_, _) => { tcs.TrySetResult(true); parent.Children.Remove(border); };

        var cancelBtn = new Button
        {
            Content = "取消",
            Width = 70,
            Height = 28,
            Cursor = Cursors.Hand,
        };
        cancelBtn.Style = (Style)Application.Current.FindResource("DefaultButtonStyle");
        cancelBtn.Click += (_, _) => { tcs.TrySetResult(false); parent.Children.Remove(border); };

        btnPanel.Children.Add(confirmBtn);
        btnPanel.Children.Add(cancelBtn);
        stack.Children.Add(btnPanel);
        border.Child = stack;
        parent.Children.Add(border);

        return tcs.Task;
    }
}

internal static class TbFg
{
    public static T WithFg<T>(this T tb, string key) where T : TextBlock
    {
        tb.SetResourceReference(TextBlock.ForegroundProperty, key);
        return tb;
    }
}
