using System.Threading;
using System.Windows;
using System.Windows.Input;
using IDESK.Console;
using IDESK.Console.Models;
using IDESK.Console.Service;
using IDESK.Core;
using IDESK.Core.Logging;
using IDESK.Widgets.Habit;
using IDESK.Widgets.Habit.Service;
using IDESK.Widgets.Notes;
using IDESK.Widgets.Plan;
using IDESK.Widgets.Plan.Service;
using IDESK.Widgets.Schedule;
using IDESK.Widgets.Schedule.Service;
using IDESK.Widgets.Todo;
using IDESK.Widgets.Translate;
using IDESK.Transient.Chat;
using IDESK.Transient.Translate;
using IDESK.Widgets.Translate.Service;
using Microsoft.Extensions.DependencyInjection;

namespace IDESK.Host;

public partial class App : Application
{
    private static readonly Mutex _instanceMutex = new(true, "IDESK_AppInstance");
    public static IServiceProvider Services { get; private set; } = null!;
    private HotkeyService? _hotkeyService;
    private ConsoleWindow? _consoleWindow;
    private TransientWidget? _chatWindow;
    private TransientWidget? _translateWindow;
    private DebugWindow? _debugWindow;
    private bool _isCapturing;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (!_instanceMutex.WaitOne(TimeSpan.Zero, false))
        {
            MessageBox.Show("IDESK 已在运行中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            Current.Shutdown();
            return;
        }

        var services = new ServiceCollection();
        services.AddSingleton<ILogger, FileLogger>();
        services.AddTodoWidget();
        services.AddNotesWidget();
        services.AddInstanceService();
        services.AddSingleton<IScheduleService, ScheduleService>();
        services.AddSingleton<IHabitService, HabitService>();
        services.AddSingleton<IPlanService, PlanService>();
        services.AddSingleton<IPlanLiteService, PlanLiteService>();
        services.AddSingleton<ITranslateService, TranslateService>();
        services.AddSingleton<ConsoleWindow>();

        Services = services.BuildServiceProvider();

        ThemeManager.Load();

        _consoleWindow = Services.GetRequiredService<ConsoleWindow>();
        var cfg = TransientConfig.Load();
        _ = _consoleWindow.LoadWidgetsAsync();
        if (!cfg.StartMinimized)
            _consoleWindow.Show();

        var hotkey = HotkeyConfig.Load();
        _hotkeyService = new HotkeyService();
        if (!_hotkeyService.TryRegister(hotkey.Modifiers, hotkey.Key, ToggleConsole, out _))
            _hotkeyService.TryRegister(HotkeyService.MOD_ALT, Key.W, ToggleConsole, out _);

        if (!_hotkeyService.TryRegister(hotkey.ToggleModifiers, hotkey.ToggleKey, DeskWidget.ToggleAllVisibility, out _))
            _hotkeyService.TryRegister(HotkeyService.MOD_ALT, Key.S, DeskWidget.ToggleAllVisibility, out _);

        if (!_hotkeyService.TryRegister(hotkey.ChatModifiers, hotkey.ChatKey, ToggleChat, out _))
            _hotkeyService.TryRegister(HotkeyService.MOD_ALT, KeyInterop.KeyFromVirtualKey(0x20), ToggleChat, out _);

        if (!_hotkeyService.TryRegister(hotkey.TranslateModifiers, hotkey.TranslateKey, ToggleTranslate, out _))
            _hotkeyService.TryRegister(HotkeyService.MOD_ALT, KeyInterop.KeyFromVirtualKey(0x43), ToggleTranslate, out _);

        if (!_hotkeyService.TryRegister(hotkey.ScreenshotModifiers, hotkey.ScreenshotKey, ToggleScreenshot, out _))
            _hotkeyService.TryRegister(HotkeyService.MOD_ALT | HotkeyService.MOD_SHIFT, Key.S, ToggleScreenshot, out _);

        if (!_hotkeyService.TryRegister(hotkey.ScreenshotTranslateModifiers, hotkey.ScreenshotTranslateKey, ToggleScreenshotTranslate, out _))
            _hotkeyService.TryRegister(HotkeyService.MOD_ALT | HotkeyService.MOD_SHIFT, Key.C, ToggleScreenshotTranslate, out _);

        if (!_hotkeyService.TryRegister(hotkey.TopmostModifiers, hotkey.TopmostKey, DeskWidget.ToggleAllTopmost, out _))
            _hotkeyService.TryRegister(HotkeyService.MOD_ALT, Key.X, DeskWidget.ToggleAllTopmost, out _);

        if (!_hotkeyService.TryRegister(hotkey.MinimizeModifiers, hotkey.MinimizeKey, DeskWidget.MinimizeAllToBookmark, out _))
            _hotkeyService.TryRegister(HotkeyService.MOD_ALT, Key.V, DeskWidget.MinimizeAllToBookmark, out _);
        if (!_hotkeyService.TryRegister(hotkey.DebugModifiers, hotkey.DebugKey, ToggleDebugWindow, out _))
            _hotkeyService.TryRegister(HotkeyService.MOD_ALT, Key.F3, ToggleDebugWindow, out _);
    }

    private void ToggleConsole()
    {
        Dispatcher.Invoke(() =>
        {
            if (_consoleWindow != null)
            {
                if (_consoleWindow.IsVisible)
                {
                    _consoleWindow.Hide();
                    return;
                }
                _consoleWindow.Show();
                _consoleWindow.Activate();
                return;
            }

            _consoleWindow = Services.GetRequiredService<ConsoleWindow>();
            _consoleWindow.Show();
            _consoleWindow.Activate();
        });
    }

    private void ToggleChat()
    {
        Dispatcher.Invoke(() =>
        {
            if (_chatWindow != null)
            {
                if (_chatWindow.IsVisible)
                {
                    _chatWindow.Close();
                    return;
                }
                _chatWindow = null;
            }

            var cfg = TransientPositionConfig.Load("chat.json", 480, 640);
            _chatWindow = new ChatWindow();
            WindowPositionHelper.RestorePosition(_chatWindow, cfg);

            // 失焦自动保存位置（自动关闭时 Closed 已存过，这里防重复）
            _chatWindow.Deactivated += (_, _) =>
            {
                if (_chatWindow == null) return;
                WindowPositionHelper.SavePosition(_chatWindow, cfg);
                cfg.Save();
            };

            _chatWindow.Closed += (_, _) =>
            {
                WindowPositionHelper.SavePosition(_chatWindow, cfg);
                cfg.Save();
                _chatWindow = null;
            };

            _chatWindow.Show();
            _chatWindow.Activate();
        });
    }

    private void ToggleTranslate()
    {
        Dispatcher.Invoke(() =>
        {
            if (_translateWindow != null)
            {
                if (_translateWindow.IsVisible)
                {
                    _translateWindow.Close();
                    return;
                }
                _translateWindow = null;
            }

            var cfg = TransientPositionConfig.Load("translate.json", 480, 640);
            _translateWindow = new IDESK.Transient.Translate.TranslateWindow();
            WindowPositionHelper.RestorePosition(_translateWindow, cfg);

            _translateWindow.Deactivated += (_, _) =>
            {
                if (_translateWindow == null) return;
                WindowPositionHelper.SavePosition(_translateWindow, cfg);
                cfg.Save();
            };

            _translateWindow.Closed += (_, _) =>
            {
                WindowPositionHelper.SavePosition(_translateWindow, cfg);
                cfg.Save();
                _translateWindow = null;
            };

            _translateWindow.Show();
            _translateWindow.Activate();
        });
    }

    private async void ToggleScreenshot()
    {
        if (_isCapturing) return;
        _isCapturing = true;
        try
        {
            string? ocrResult = null;

            // 先截屏选区域
            var overlay = new ScreenshotOverlay();
            var captured = false;
            var tcs = new TaskCompletionSource<bool>();
            overlay.RegionSelected += async region =>
            {
                if (captured) return;
                captured = true;
                overlay.Hide();
                try
                {
                    ocrResult = await OcrService.CaptureAndOcrAsync(region);
                    tcs.TrySetResult(true);
                }
                catch { tcs.TrySetResult(false); }
                finally { overlay.Close(); }
            };
            overlay.Closed += (_, _) => { if (!captured) tcs.TrySetResult(false); };
            overlay.Show();
            overlay.Activate();
            await tcs.Task;

            // 识别成功后才弹出 Chat（先关旧窗口，避免跨桌面问题）
            if (string.IsNullOrEmpty(ocrResult)) return;

            if (_chatWindow != null) { _chatWindow.Close(); _chatWindow = null; }
            var cfg = TransientPositionConfig.Load("chat.json", 480, 640);
            _chatWindow = new ChatWindow();
            WindowPositionHelper.RestorePosition(_chatWindow, cfg);
            _chatWindow.Deactivated += (_, _) =>
            {
                if (_chatWindow == null) return;
                WindowPositionHelper.SavePosition(_chatWindow, cfg);
                cfg.Save();
            };
            _chatWindow.Closed += (_, _) =>
            {
                WindowPositionHelper.SavePosition(_chatWindow, cfg);
                cfg.Save();
                _chatWindow = null;
            };
            _chatWindow.Show();

            await Task.Delay(300);
            ChatPage.SendExternalText(ocrResult);
        }
        finally { _isCapturing = false; }
    }

    private async void ToggleScreenshotTranslate()
    {
        if (_isCapturing) return;
        _isCapturing = true;
        try
        {
            string? ocrResult = null;

            var overlay = new ScreenshotOverlay();
            var captured = false;
            var tcs = new TaskCompletionSource<bool>();
            overlay.RegionSelected += async region =>
            {
                if (captured) return;
                captured = true;
                overlay.Hide();
                try { ocrResult = await OcrService.CaptureAndOcrAsync(region); tcs.TrySetResult(true); }
                catch { tcs.TrySetResult(false); }
                finally { overlay.Close(); }
            };
            overlay.Closed += (_, _) => { if (!captured) tcs.TrySetResult(false); };
            overlay.Show();
            overlay.Activate();
            await tcs.Task;

            if (string.IsNullOrEmpty(ocrResult)) return;

            // 先关旧 Translate 窗口
            if (_translateWindow != null) { _translateWindow.Close(); _translateWindow = null; }
            var cfg = TransientPositionConfig.Load("translate.json", 480, 640);
            _translateWindow = new IDESK.Transient.Translate.TranslateWindow();
            WindowPositionHelper.RestorePosition(_translateWindow, cfg);
            _translateWindow.Deactivated += (_, _) =>
            {
                if (_translateWindow == null) return;
                WindowPositionHelper.SavePosition(_translateWindow, cfg);
                cfg.Save();
            };
            _translateWindow.Closed += (_, _) =>
            {
                WindowPositionHelper.SavePosition(_translateWindow, cfg);
                cfg.Save();
                _translateWindow = null;
            };
            _translateWindow.Show();

            await Task.Delay(300);
            IDESK.Transient.Translate.TranslateTransientView.SendExternalText(ocrResult);
        }
        finally { _isCapturing = false; }
    }

    private void ToggleDebugWindow()
    {
        Dispatcher.Invoke(() =>
        {
            if (_debugWindow != null)
            {
                if (_debugWindow.IsVisible)
                {
                    _debugWindow.Close();
                    _debugWindow = null;
                    return;
                }
                _debugWindow = null;
            }

            _debugWindow = new DebugWindow();
            _debugWindow.Closed += (_, _) => _debugWindow = null;
            _debugWindow.Show();
        });
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeyService?.Dispose();
        _instanceMutex.ReleaseMutex();
        base.OnExit(e);
    }
}
