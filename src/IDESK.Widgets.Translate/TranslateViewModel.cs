using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using IDESK.Core.Agent;
using IDESK.Core.Agent.Prompts;
using IDESK.Core.Helper;

namespace IDESK.Widgets.Translate;

public class TranslateViewModel : INotifyPropertyChanged
{
    private readonly AgentService _agent = new();
    private string _inputText = "";
    private bool _isProcessing;
    private bool _isZhToEn;
    private TranslateDisplayItem? _pendingSource;

    public string InputText
    {
        get => _inputText;
        set { _inputText = value; Notify(nameof(InputText)); }
    }

    public bool IsProcessing
    {
        get => _isProcessing;
        set { _isProcessing = value; Notify(nameof(IsProcessing)); }
    }

    public bool IsZhToEn
    {
        get => _isZhToEn;
        set
        {
            _isZhToEn = value;
            Notify(nameof(IsZhToEn));
            Notify(nameof(DirectionText));
        }
    }

    public string DirectionText => _isZhToEn ? "中译英" : "英译中";

    public ObservableCollection<TranslateDisplayItem> DisplayItems { get; } = [];

    public ICommand TranslateCommand { get; }
    public ICommand ToggleDirectionCommand { get; }

    public TranslateViewModel()
    {
        TranslateCommand = new RelayCommand(async _ => await TranslateAsync(), _ => !IsProcessing);
        ToggleDirectionCommand = new RelayCommand(_ => IsZhToEn = !IsZhToEn);
    }

    public async Task TranslateAsync()
    {
        string text = InputText.Trim();
        if (string.IsNullOrEmpty(text)) return;

        InputText = "";
        DisplayItems.Clear();
        IsProcessing = true;

        // 先显示原文（与 WORD 行相同样式）
        _pendingSource = new TranslateDisplayItem
        {
            Text = text,
            FontSize = 22,
            Weight = FontWeights.Bold,
            ForegroundKey = "Blue9Brush",
            Margin = new Thickness(0, 0, 0, 10),
        };
        DisplayItems.Add(_pendingSource);

        try
        {
            var rawResult = "";
            var prompt = IsZhToEn ? TranslateZhToEnPrompt.System + text : TranslatePrompt.System + text;
            await _agent.SendAsync(prompt, chunk =>
            {
                rawResult = chunk;
            });

            if (IsZhToEn)
                ParseZhToEnResult(rawResult);
            else
                ParseResult(rawResult);

            RemovePendingSource();
        }
        catch (Exception ex)
        {
            RemovePendingSource();
            DisplayItems.Add(new TranslateDisplayItem
            {
                Text = $"请求失败：{ex.Message}",
                ForegroundKey = "DangerBrush",
                FontSize = 14,
            });
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private void ParseZhToEnResult(string raw)
    {
        foreach (var line in raw.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var t = line.Trim();
            if (t.StartsWith("MODE:")) continue;

            if (t.StartsWith("CAND:"))
            {
                DisplayItems.Add(new TranslateDisplayItem
                {
                    Text = t[5..].Trim(),
                    FontSize = 18,
                    Weight = FontWeights.Bold,
                    ForegroundKey = "Blue9Brush",
                    Margin = new Thickness(0, 8, 0, 2),
                });
            }
            else if (t.StartsWith("MEAN:"))
            {
                DisplayItems.Add(new TranslateDisplayItem
                {
                    Text = t[5..].Trim(),
                    FontSize = 13,
                    ForegroundKey = "Blue8Brush",
                    Margin = new Thickness(0, 0, 0, 12),
                });
            }
        }
    }

    private void ParseResult(string raw)
    {
        foreach (var line in raw.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var t = line.Trim();
            if (t.StartsWith("MODE:")) continue;

            if (t.StartsWith("WORD:"))
            {
                DisplayItems.Add(new TranslateDisplayItem
                {
                    Text = t[5..].Trim(),
                    FontSize = 22,
                    Weight = FontWeights.Bold,
                    ForegroundKey = "Blue9Brush",
                    Margin = new Thickness(0, 0, 0, 10),
                });
            }
            else if (t.StartsWith("POS:"))
            {
                var content = t[4..].Trim();
                var parts = content.Split('|');
                var text = parts.Length == 2 ? $"{parts[0].Trim()}  {parts[1].Trim()}" : content;
                DisplayItems.Add(new TranslateDisplayItem
                {
                    Text = text,
                    FontSize = 14,
                    ForegroundKey = "Blue8Brush",
                    Margin = new Thickness(0, 1, 0, 1),
                });
            }
            else if (t.StartsWith("SYN:"))
            {
                DisplayItems.Add(new TranslateDisplayItem
                {
                    Text = "同义词",
                    FontSize = 12,
                    Weight = FontWeights.Bold,
                    ForegroundKey = "Blue5Brush",
                    Margin = new Thickness(0, 8, 0, 2),
                });
                DisplayItems.Add(new TranslateDisplayItem
                {
                    Text = t[4..].Trim(),
                    FontSize = 13,
                    ForegroundKey = "Blue9Brush",
                    Margin = new Thickness(0, 0, 0, 4),
                });
            }
            else if (t.StartsWith("DEF:"))
            {
                DisplayItems.Add(new TranslateDisplayItem
                {
                    Text = "释义",
                    FontSize = 12,
                    Weight = FontWeights.Bold,
                    ForegroundKey = "Blue5Brush",
                    Margin = new Thickness(0, 8, 0, 2),
                });
                DisplayItems.Add(new TranslateDisplayItem
                {
                    Text = t[4..].Trim(),
                    FontSize = 14,
                    ForegroundKey = "Blue9Brush",
                    Margin = new Thickness(0, 0, 0, 4),
                });
            }
            else if (t.StartsWith("EX:"))
            {
                if (!DisplayItems.Any(d => d.Text == "例句"))
                {
                    DisplayItems.Add(new TranslateDisplayItem
                    {
                        Text = "例句",
                        FontSize = 12,
                        Weight = FontWeights.Bold,
                        ForegroundKey = "Blue5Brush",
                        Margin = new Thickness(0, 8, 0, 4),
                    });
                }
                DisplayItems.Add(new TranslateDisplayItem
                {
                    Text = $"• {t[3..].Trim()}",
                    FontSize = 13,
                    ForegroundKey = "Blue8Brush",
                    Margin = new Thickness(0, 2, 0, 2),
                });
            }
            else if (t.StartsWith("TRANS:"))
            {
                DisplayItems.Add(new TranslateDisplayItem
                {
                    Text = t[6..].Trim(),
                    FontSize = 16,
                    Weight = FontWeights.Bold,
                    ForegroundKey = "Blue9Brush",
                    Margin = new Thickness(0, 0, 0, 12),
                });
            }
            else if (t.StartsWith("NOTE:"))
            {
                DisplayItems.Add(new TranslateDisplayItem
                {
                    Text = "说明",
                    FontSize = 12,
                    Weight = FontWeights.Bold,
                    ForegroundKey = "Blue5Brush",
                    Margin = new Thickness(0, 8, 0, 2),
                });
                DisplayItems.Add(new TranslateDisplayItem
                {
                    Text = t[5..].Trim(),
                    FontSize = 13,
                    ForegroundKey = "Blue8Brush",
                    Margin = new Thickness(0, 0, 0, 4),
                });
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

    private void RemovePendingSource()
    {
        if (_pendingSource != null)
        {
            DisplayItems.Remove(_pendingSource);
            _pendingSource = null;
        }
    }
}
