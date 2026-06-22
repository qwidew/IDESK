using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using IDESK.Console.Models;

namespace IDESK.Console.Control;

public partial class KeyBindingsPage : UserControl
{
    private readonly HotkeyConfig _config;
    private bool _isRecording;
    private string _currentAction = "";
    private uint _pendingModifiers;

    public event Action? BackRequested;

    private const uint DefaultModifiers = HotkeyConfig.MOD_ALT;

    public KeyBindingsPage()
    {
        InitializeComponent();
        _config = HotkeyConfig.Load();
        RefreshDisplay();
    }

    private void OnBackClick(object sender, RoutedEventArgs e) => BackRequested?.Invoke();

    private void OnResetClick(object sender, RoutedEventArgs e)
    {
        if (_isRecording) CancelRecording();

        var action = (string)((Button)sender).Tag;
        if (action == "OpenConsole")
        {
            _config.Modifiers = DefaultModifiers;
            _config.VirtualKey = 0x57;
        }
        else if (action == "ToggleChat")
        {
            _config.ChatModifiers = HotkeyConfig.MOD_ALT;
            _config.ChatVirtualKey = 0x20;
        }
        else if (action == "ToggleTranslate")
        {
            _config.TranslateModifiers = HotkeyConfig.MOD_ALT;
            _config.TranslateVirtualKey = 0x43;
        }
        else if (action == "Screenshot")
        {
            _config.ScreenshotModifiers = HotkeyConfig.MOD_ALT | HotkeyConfig.MOD_SHIFT;
            _config.ScreenshotVirtualKey = 0x53;
        }
        else if (action == "ScreenshotTranslate")
        {
            _config.ScreenshotTranslateModifiers = HotkeyConfig.MOD_ALT | HotkeyConfig.MOD_SHIFT;
            _config.ScreenshotTranslateVirtualKey = 0x43;
        }
        else if (action == "Topmost")
        {
            _config.TopmostModifiers = HotkeyConfig.MOD_ALT;
            _config.TopmostVirtualKey = 0x58;
        }
        else if (action == "Minimize")
        {
            _config.MinimizeModifiers = HotkeyConfig.MOD_ALT;
            _config.MinimizeVirtualKey = 0x56;
        }
        else if (action == "Debug")
        {
            _config.DebugModifiers = HotkeyConfig.MOD_ALT;
            _config.DebugVirtualKey = 0x72;
        }
        else
        {
            _config.ToggleModifiers = DefaultModifiers;
            _config.ToggleVirtualKey = 0x53;
        }
        _config.Save();
        RefreshDisplay();
        HintText.Content = "快捷键已恢复默认，重启后生效。";
    }

    private void OnRecordClick(object sender, RoutedEventArgs e)
    {
        if (_isRecording)
        {
            CancelRecording();
            return;
        }

        var btn = (Button)sender;
        _currentAction = (string)btn.Tag;
        _isRecording = true;
        _pendingModifiers = 0;

        var recordIcon = _currentAction switch
        {
            "OpenConsole" => ConsoleRecordIcon,
            "ToggleChat" => ChatRecordIcon,
            "ToggleTranslate" => TranslateRecordIcon,
            "Screenshot" => ScreenshotRecordIcon,
            "ScreenshotTranslate" => ScreenshotTranslateRecordIcon,
            "Topmost" => TopmostRecordIcon,
            "Minimize" => MinimizeRecordIcon,
            "Debug" => DebugRecordIcon,
            _ => ToggleRecordIcon
        };
        recordIcon.Data = FindResource("IconClose") as StreamGeometry;
        recordIcon.Fill = FindResource("DangerBrush") as SolidColorBrush ?? recordIcon.Fill;

        AltBtn.Visibility = Visibility.Visible;
        AltBtn.IsEnabled = true;
        ActionHeader.Content = "快捷键（录制中）";

        var keyText = _currentAction switch
        {
            "OpenConsole" => ConsoleKeyText,
            "ToggleChat" => ChatKeyText,
            "ToggleTranslate" => TranslateKeyText,
            "Screenshot" => ScreenshotKeyText,
            "ScreenshotTranslate" => ScreenshotTranslateKeyText,
            "Topmost" => TopmostKeyText,
            "Minimize" => MinimizeKeyText,
            "Debug" => DebugKeyText,
            _ => ToggleKeyText
        };
        keyText.Content = "按下组合键...";
        HintText.Content = "按下组合键，或点击右下角 Alt";

        var win = Window.GetWindow(this);
        if (win != null) win.PreviewKeyDown += OnKeyDown;
    }

    private void OnAltClick(object sender, RoutedEventArgs e)
    {
        if (!_isRecording) return;

        _pendingModifiers ^= HotkeyConfig.MOD_ALT;
        AltBtnLabel.Foreground = _pendingModifiers != 0
            ? FindResource("TextPrimaryBrush") as SolidColorBrush
            : FindResource("AccentBrush") as SolidColorBrush;

        var keyText = _currentAction switch
        {
            "OpenConsole" => ConsoleKeyText,
            "ToggleChat" => ChatKeyText,
            "ToggleTranslate" => TranslateKeyText,
            "Screenshot" => ScreenshotKeyText,
            "ScreenshotTranslate" => ScreenshotTranslateKeyText,
            "Topmost" => TopmostKeyText,
            "Minimize" => MinimizeKeyText,
            "Debug" => DebugKeyText,
            _ => ToggleKeyText
        };
        keyText.Content = FormatModifiers(_pendingModifiers) + "…";
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (!_isRecording) return;
        e.Handled = true;

        uint mods = _pendingModifiers;
        if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            mods |= HotkeyConfig.MOD_CONTROL;
        if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
            mods |= HotkeyConfig.MOD_ALT;
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            mods |= HotkeyConfig.MOD_SHIFT;
        if (Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin))
            mods |= HotkeyConfig.MOD_WIN;

        var key = e.Key;
        if (IsModifier(key))
            return;

        if (mods == 0 || key == Key.None)
        {
            HintText.Content = "至少需要一个修饰键（Ctrl / Alt / Shift / Win），可点击右下角 Alt";
            return;
        }

        FinishRecording(mods, key);
    }

    private void FinishRecording(uint mods, Key key)
    {
        _isRecording = false;
        DetachKeyHook();
        HideAltButton();
        RestoreRecordIcon();

        switch (_currentAction)
        {
            case "OpenConsole":
                _config.Modifiers = mods;
                _config.VirtualKey = KeyInterop.VirtualKeyFromKey(key);
                break;
            case "ToggleChat":
                _config.ChatModifiers = mods;
                _config.ChatVirtualKey = KeyInterop.VirtualKeyFromKey(key);
                break;
            case "ToggleTranslate":
                _config.TranslateModifiers = mods;
                _config.TranslateVirtualKey = KeyInterop.VirtualKeyFromKey(key);
                break;
            case "Screenshot":
                _config.ScreenshotModifiers = mods;
                _config.ScreenshotVirtualKey = KeyInterop.VirtualKeyFromKey(key);
                break;
            case "ScreenshotTranslate":
                _config.ScreenshotTranslateModifiers = mods;
                _config.ScreenshotTranslateVirtualKey = KeyInterop.VirtualKeyFromKey(key);
                break;
            case "Topmost":
                _config.TopmostModifiers = mods;
                _config.TopmostVirtualKey = KeyInterop.VirtualKeyFromKey(key);
                break;
            case "Minimize":
                _config.MinimizeModifiers = mods;
                _config.MinimizeVirtualKey = KeyInterop.VirtualKeyFromKey(key);
                break;
            case "Debug":
                _config.DebugModifiers = mods;
                _config.DebugVirtualKey = KeyInterop.VirtualKeyFromKey(key);
                break;
            default:
                _config.ToggleModifiers = mods;
                _config.ToggleVirtualKey = KeyInterop.VirtualKeyFromKey(key);
                break;
        }
        _config.Save();

        RefreshDisplay();
        ActionHeader.Content = "快捷键";
        HintText.Content = "快捷键已保存，重启后生效。";
    }

    private void CancelRecording()
    {
        _isRecording = false;
        DetachKeyHook();
        HideAltButton();
        RestoreRecordIcon();
        RefreshDisplay();
        ActionHeader.Content = "快捷键";
        HintText.Content = "";
    }

    private void RestoreRecordIcon()
    {
        var icon = _currentAction switch
        {
            "OpenConsole" => ConsoleRecordIcon,
            "ToggleChat" => ChatRecordIcon,
            "ToggleTranslate" => TranslateRecordIcon,
            "Screenshot" => ScreenshotRecordIcon,
            "ScreenshotTranslate" => ScreenshotTranslateRecordIcon,
            "Topmost" => TopmostRecordIcon,
            "Minimize" => MinimizeRecordIcon,
            "Debug" => DebugRecordIcon,
            _ => ToggleRecordIcon
        };
        icon.Data = FindResource("IconEdit") as StreamGeometry;
        icon.Fill = FindResource("AccentBrush") as SolidColorBrush ?? icon.Fill;
    }

    private void HideAltButton()
    {
        AltBtn.Visibility = Visibility.Collapsed;
        AltBtn.IsEnabled = false;
    }

    private void DetachKeyHook()
    {
        var win = Window.GetWindow(this);
        if (win != null) win.PreviewKeyDown -= OnKeyDown;
    }

    private void RefreshDisplay()
    {
        ConsoleKeyText.Content = FormatBinding(_config.Modifiers, _config.Key);
        ToggleKeyText.Content = FormatBinding(_config.ToggleModifiers, _config.ToggleKey);
        ChatKeyText.Content = FormatBinding(_config.ChatModifiers, _config.ChatKey);
        TranslateKeyText.Content = FormatBinding(_config.TranslateModifiers, _config.TranslateKey);
        ScreenshotKeyText.Content = FormatBinding(_config.ScreenshotModifiers, _config.ScreenshotKey);
        ScreenshotTranslateKeyText.Content = FormatBinding(_config.ScreenshotTranslateModifiers, _config.ScreenshotTranslateKey);
        TopmostKeyText.Content = FormatBinding(_config.TopmostModifiers, _config.TopmostKey);
        MinimizeKeyText.Content = FormatBinding(_config.MinimizeModifiers, _config.MinimizeKey);
        DebugKeyText.Content = FormatBinding(_config.DebugModifiers, _config.DebugKey);
    }

    private static string FormatBinding(uint mods, Key key)
    {
        var parts = new List<string>();
        if ((mods & HotkeyConfig.MOD_CONTROL) != 0) parts.Add("Ctrl");
        if ((mods & HotkeyConfig.MOD_ALT) != 0) parts.Add("Alt");
        if ((mods & HotkeyConfig.MOD_SHIFT) != 0) parts.Add("Shift");
        if ((mods & HotkeyConfig.MOD_WIN) != 0) parts.Add("Win");
        parts.Add(key.ToString());
        return string.Join(" + ", parts);
    }

    private static string FormatModifiers(uint mods)
    {
        var parts = new List<string>();
        if ((mods & HotkeyConfig.MOD_CONTROL) != 0) parts.Add("Ctrl");
        if ((mods & HotkeyConfig.MOD_ALT) != 0) parts.Add("Alt");
        if ((mods & HotkeyConfig.MOD_SHIFT) != 0) parts.Add("Shift");
        if ((mods & HotkeyConfig.MOD_WIN) != 0) parts.Add("Win");
        return string.Join(" + ", parts);
    }

    private static bool IsModifier(Key key) => key switch
    {
        Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin => true,
        _ => false
    };
}
