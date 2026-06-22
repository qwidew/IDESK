using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using IDESK.Core.Audio;
using IDESK.Widgets.Translate;

namespace IDESK.Transient.Translate;

public partial class TranslateTransientView : UserControl
{
    private readonly TranslateViewModel _vm;
    private readonly VoiceService _voice = new();

    private static event Action<string>? OnExternalText;
    public static void SendExternalText(string text) => OnExternalText?.Invoke(text);

    public TranslateTransientView(TranslateViewModel vm)
    {
        _vm = vm;
        InitializeComponent();
        DataContext = vm;

        vm.DisplayItems.CollectionChanged += OnDisplayItemsChanged;

        Loaded += (_, _) => Dispatcher.BeginInvoke(() =>
        {
            InputBox.Focus();
            Keyboard.Focus(InputBox);
        });

        OnExternalText += async input =>
        {
            if (!IsLoaded) return;
            _vm.InputText = input;
            await _vm.TranslateAsync();
        };

        _voice.TranscriptionCompleted += text =>
            _ = Dispatcher.InvokeAsync(() =>
            {
                _vm.InputText = text;
                _ = _vm.TranslateAsync();
            });
        _voice.TranscriptionFailed += err =>
            _ = Dispatcher.InvokeAsync(() => _vm.InputText = $"语音识别失败：{err}");
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

    private void OnInputPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !_vm.IsProcessing)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
                return; // Shift+Enter → 换行

            e.Handled = true;
            _ = _vm.TranslateAsync();
        }
    }

    private void OnDisplayItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_vm.DisplayItems.Count == 0) return;
        if (ResultCard.Visibility == Visibility.Visible) return;

        ResultCard.Visibility = Visibility.Visible;

        if (Window.GetWindow(this) is Window win)
        {
            win.Height = win.Height + 12 + 400 + 28;
        }
    }
}
