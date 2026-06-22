using System.Globalization;
using System.Speech.Recognition;

namespace IDESK.Core.Audio;

public class VoiceService : IDisposable
{
    private SpeechRecognitionEngine? _engine;
    private bool _isRecording;

    public bool IsRecording => _isRecording;

    public event Action? RecordingStarted;
    public event Action? RecordingStopped;
    public event Action<string>? TranscriptionCompleted;
    public event Action<string>? TranscriptionFailed;

    public void StartRecording()
    {
        if (_isRecording) return;
        StopCleanup();

        try
        {
            _engine = new SpeechRecognitionEngine(new CultureInfo("zh-CN"));
            _engine.SetInputToDefaultAudioDevice();
            _engine.LoadGrammar(new DictationGrammar());
            _engine.SpeechRecognized += OnSpeechRecognized;
            _engine.RecognizeCompleted += OnRecognizeCompleted;
            _engine.RecognizeAsync(RecognizeMode.Single);
            _isRecording = true;
            RecordingStarted?.Invoke();
        }
        catch (Exception ex)
        {
            TranscriptionFailed?.Invoke($"启动语音识别失败：{ex.Message}");
        }
    }

    public void StopRecording()
    {
        if (_engine == null) return;
        try { _engine.RecognizeAsyncCancel(); } catch { }
        StopCleanup();
    }

    private void OnSpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
    {
        if (e.Result?.Text is { Length: > 0 } text)
            TranscriptionCompleted?.Invoke(text);
        else
            TranscriptionFailed?.Invoke("未能识别到语音内容");
    }

    private void OnRecognizeCompleted(object? sender, RecognizeCompletedEventArgs e)
    {
        StopCleanup();
        if (e.Error != null)
            TranscriptionFailed?.Invoke($"语音识别错误：{e.Error.Message}");
        else if (e.Cancelled)
            TranscriptionFailed?.Invoke("语音识别已取消");
        else if (e.Result?.Text == null || e.Result.Text.Length == 0)
            TranscriptionFailed?.Invoke("未能识别到语音内容");
    }

    public void Dispose() => StopCleanup();

    private void StopCleanup()
    {
        if (_isRecording)
        {
            _isRecording = false;
            RecordingStopped?.Invoke();
        }
        if (_engine != null)
        {
            _engine.SpeechRecognized -= OnSpeechRecognized;
            _engine.RecognizeCompleted -= OnRecognizeCompleted;
            _engine.Dispose();
            _engine = null;
        }
    }
}
