using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using IDESK.Core.Audio;

namespace IDESK.Widgets.Translate;

public partial class TranslateView : UserControl
{
    private readonly TranslateViewModel _vm;
    private readonly VoiceService _voice = new();

    public TranslateView(TranslateViewModel vm)
    {
        _vm = vm;
        InitializeComponent();
        DataContext = vm;

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
                return;

            e.Handled = true;
            _ = _vm.TranslateAsync();
        }
    }
}
