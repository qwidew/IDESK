using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using IDESK.Core;

namespace IDESK.Host;

internal sealed class DebugWindow : Window
{
    private readonly TextBox _textBox;

    public DebugWindow()
    {
        Title = "Debug - Prompt";
        Width = 700;
        Height = 500;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Topmost = true;

        _textBox = new TextBox
        {
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            FontFamily = new System.Windows.Media.FontFamily("Cascadia Code, Consolas, monospace"),
            FontSize = 13,
            Margin = new Thickness(10),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
        };
        Content = _textBox;

        Loaded += (_, _) => RefreshContent();
        KeyDown += (_, e) => { if (e.Key == Key.F5) RefreshContent(); };
    }

    private void RefreshContent()
    {
        _textBox.Text = DebugState.LastPrompt ?? "(no prompt yet)";
    }
}
