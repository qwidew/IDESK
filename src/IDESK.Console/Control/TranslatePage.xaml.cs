using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using IDESK.Core.Agent;
using IDESK.Core.Agent.Prompts;
using IDESK.Core.Audio;
using IDESK.Widgets.Translate.Service;

namespace IDESK.Console.Control;

public partial class TranslatePage : UserControl
{
    private readonly ITranslateService _translateService;
    private readonly AgentService _agent = new();
    private readonly VoiceService _voice = new();
    private bool _isCreated;
    private bool _isWaiting;
    private bool _isZhToEn;
    public event Action? Created;
    public event Action? DeleteRequested;

    public TranslatePage(ITranslateService translateService)
    {
        _translateService = translateService;
        InitializeComponent();

        Loaded += async (_, _) =>
        {
            _isCreated = await _translateService.GetCreatedAsync();
            UpdateButton();
        };

        _voice.TranscriptionCompleted += text =>
            _ = Dispatcher.InvokeAsync(() =>
            {
                InputBox.Text = text;
                _ = TranslateAsync();
            });
        _voice.TranscriptionFailed += err =>
            _ = Dispatcher.InvokeAsync(() => InputBox.Text = $"语音识别失败：{err}");
        _voice.RecordingStarted += () =>
            _ = Dispatcher.InvokeAsync(() =>
            {
                MicIcon.Fill = FindResource("DangerBrush") as Brush;
                MicBtn.ToolTip = "点击停止录音";
            });
        _voice.RecordingStopped += () =>
            _ = Dispatcher.InvokeAsync(() =>
            {
                MicIcon.Fill = FindResource("ActionIconBrush") as Brush;
                MicBtn.ToolTip = "语音输入";
            });
    }

    private void OnMicClick(object sender, RoutedEventArgs e)
    {
        if (_voice.IsRecording)
            _voice.StopRecording();
        else
            _voice.StartRecording();
    }

    private async void OnSendClick(object sender, RoutedEventArgs e) => await TranslateAsync();

    private void OnInputPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
        {
            e.Handled = true;
            _ = TranslateAsync();
        }
    }

    private async Task TranslateAsync()
    {
        string text = InputBox.Text.Trim();
        if (string.IsNullOrEmpty(text) || _isWaiting) return;

        InputBox.Clear();
        AddBubble(text, HorizontalAlignment.Right, "ItemBgBrush", "TextPrimaryBrush");

        _isWaiting = true;
        SendBtnLabel.Content = "…";
        InputBox.IsEnabled = false;

        try
        {
            var rawResult = "";
            var prompt = _isZhToEn ? TranslateZhToEnPrompt.System + text : TranslatePrompt.System + text;
            await _agent.SendAsync(prompt, chunk =>
            {
                rawResult = chunk;
            });

            if (_isZhToEn)
                AddZhToEnResult(rawResult);
            else
                AddParsedResult(rawResult);
        }
        catch (Exception ex)
        {
            AddBubble($"请求失败：{ex.Message}", HorizontalAlignment.Left, "DangerBgBrush", "TextOnDangerBrush");
        }
        finally
        {
            _isWaiting = false;
            SendBtnLabel.Content = "翻译";
            InputBox.IsEnabled = true;
            InputBox.Focus();
        }
    }

    private void OnDirectionToggle(object sender, RoutedEventArgs e)
    {
        _isZhToEn = !_isZhToEn;
        DirectionBtn.Content = _isZhToEn ? "中译英" : "英译中";
    }

    private void AddZhToEnResult(string raw)
    {
        var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };

        foreach (var line in raw.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var t = line.Trim();
            if (t.StartsWith("MODE:")) continue;

            if (t.StartsWith("CAND:"))
            {
                panel.Children.Add(new TextBox
                {
                    Text = t[5..].Trim(),
                    FontSize = 18, FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 8, 0, 2),
                }.Selectable("Blue9Brush"));
            }
            else if (t.StartsWith("MEAN:"))
            {
                panel.Children.Add(new TextBox
                {
                    Text = t[5..].Trim(),
                    FontSize = 13,
                    Margin = new Thickness(0, 0, 0, 12),
                }.Selectable("Blue8Brush"));
            }
        }

        var border = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            MaxWidth = 450,
            CornerRadius = new CornerRadius(12, 12, 4, 12),
            Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(0, 0, 0, 10),
            Child = panel,
        };
        border.SetResourceReference(BackgroundProperty, "SectionBgBrush");

        ResultPanel.Children.Add(border);
    }

    private void AddParsedResult(string raw)
    {
        var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
        bool hasExLabel = false;

        foreach (var line in raw.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var t = line.Trim();
            if (t.StartsWith("MODE:")) continue;

            if (t.StartsWith("WORD:"))
            {
                panel.Children.Add(new TextBox
                {
                    Text = t[5..].Trim(),
                    FontSize = 20, FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 8),
                }.Selectable("Blue9Brush"));
            }
            else if (t.StartsWith("POS:"))
            {
                var content = t[4..].Trim();
                var parts = content.Split('|');
                var text = parts.Length == 2 ? $"{parts[0].Trim()}  {parts[1].Trim()}" : content;
                panel.Children.Add(new TextBox
                {
                    Text = text, FontSize = 14,
                    Margin = new Thickness(0, 1, 0, 1),
                }.Selectable("Blue8Brush"));
            }
            else if (t.StartsWith("SYN:"))
            {
                panel.Children.Add(MakeLabel("同根词", "Blue5Brush"));
                panel.Children.Add(new TextBox
                {
                    Text = t[4..].Trim(), FontSize = 13,
                    Margin = new Thickness(0, 0, 0, 4),
                }.Selectable("Blue9Brush"));
            }
            else if (t.StartsWith("DEF:"))
            {
                panel.Children.Add(MakeLabel("释义", "Blue5Brush"));
                panel.Children.Add(new TextBox
                {
                    Text = t[4..].Trim(), FontSize = 14,
                    Margin = new Thickness(0, 0, 0, 4),
                }.Selectable("Blue9Brush"));
            }
            else if (t.StartsWith("EX:"))
            {
                if (!hasExLabel)
                {
                    panel.Children.Add(MakeLabel("例句", "Blue5Brush"));
                    hasExLabel = true;
                }
                panel.Children.Add(new TextBox
                {
                    Text = $"• {t[3..].Trim()}", FontSize = 13,
                    Margin = new Thickness(0, 2, 0, 2),
                }.Selectable("Blue8Brush"));
            }
            else if (t.StartsWith("TRANS:"))
            {
                panel.Children.Add(new TextBox
                {
                    Text = t[6..].Trim(),
                    FontSize = 16, FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 10),
                }.Selectable("Blue9Brush"));
            }
            else if (t.StartsWith("NOTE:"))
            {
                panel.Children.Add(MakeLabel("说明", "Blue5Brush"));
                panel.Children.Add(new TextBox
                {
                    Text = t[5..].Trim(), FontSize = 13,
                    Margin = new Thickness(0, 0, 0, 4),
                }.Selectable("Blue8Brush"));
            }
        }

        var border = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            MaxWidth = 450,
            CornerRadius = new CornerRadius(12, 12, 4, 12),
            Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(0, 0, 0, 10),
            Child = panel,
        };
        border.SetResourceReference(BackgroundProperty, "SectionBgBrush");

        ResultPanel.Children.Add(border);
    }

    private static TextBox MakeLabel(string text, string brushKey)
    {
        return new TextBox
        {
            Text = text, FontSize = 12, FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 8, 0, 2),
        }.Selectable(brushKey);
    }

    private void AddBubble(string text, HorizontalAlignment align, string bgBrush, string fgBrush)
    {
        var textBox = new TextBox
        {
            Text = text,
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap,
            IsReadOnly = true, BorderThickness = new Thickness(0),
            Background = Brushes.Transparent, IsTabStop = false,
            Padding = new Thickness(0),
        };
        textBox.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, fgBrush);

        var border = new Border
        {
            HorizontalAlignment = align,
            MaxWidth = 400,
            CornerRadius = new CornerRadius(12, 12, 4, 12),
            Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(0, 0, 0, 10),
            Child = textBox,
        };
        border.SetResourceReference(BackgroundProperty, bgBrush);

        ResultPanel.Children.Add(border);
    }

    private async void OnToggleClick(object sender, RoutedEventArgs e)
    {
        if (_isCreated)
        {
            _isCreated = false;
            var cfg = await _translateService.GetConfigAsync();
            if (cfg != null)
            {
                cfg.Created = false;
                await _translateService.SaveConfigAsync(cfg);
            }
            DeleteRequested?.Invoke();
        }
        else
        {
            _isCreated = true;
            await _translateService.SetCreatedAsync();
            Created?.Invoke();
        }
        UpdateButton();
    }

    private void UpdateButton()
    {
        if (_isCreated)
        {
            ToggleIcon.Data = FindResource("IconDelete") as StreamGeometry ?? ToggleIcon.Data;
            ToggleLabel.Content = "删除小组件";
            ToggleBtn.Style = FindResource("DangerButtonStyle") as Style;
        }
        else
        {
            ToggleIcon.Data = FindResource("IconAdd") as StreamGeometry ?? ToggleIcon.Data;
            ToggleLabel.Content = "添加小组件";
            ToggleBtn.Style = FindResource("DefaultButtonStyle") as Style;
        }
    }
}

internal static class TextBoxHelper
{
    /// <summary>将 TextBox 设为 ReadOnly + 无样式 + 可选中 + DynamicResource 前景色。</summary>
    public static T Selectable<T>(this T tb, string brushKey) where T : TextBox
    {
        tb.IsReadOnly = true;
        tb.BorderThickness = new Thickness(0);
        tb.Background = Brushes.Transparent;
        tb.IsTabStop = false;
        tb.Padding = new Thickness(0);
        tb.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, brushKey);
        return tb;
    }
}
