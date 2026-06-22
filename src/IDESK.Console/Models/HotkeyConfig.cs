using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Input;

namespace IDESK.Console.Models;

public class HotkeyConfig
{
    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;

    public uint Modifiers { get; set; } = MOD_ALT;
    public int VirtualKey { get; set; } = 0x57; // W

    public uint ToggleModifiers { get; set; } = MOD_ALT;
    public int ToggleVirtualKey { get; set; } = 0x53; // S

    public uint ChatModifiers { get; set; } = MOD_ALT;
    public int ChatVirtualKey { get; set; } = 0x20; // Space

    public uint TranslateModifiers { get; set; } = MOD_ALT;
    public int TranslateVirtualKey { get; set; } = 0x43; // C

    public uint ScreenshotModifiers { get; set; } = MOD_ALT | MOD_SHIFT;
    public int ScreenshotVirtualKey { get; set; } = 0x53; // S

    public uint ScreenshotTranslateModifiers { get; set; } = MOD_ALT | MOD_SHIFT;
    public int ScreenshotTranslateVirtualKey { get; set; } = 0x43; // C

    public uint TopmostModifiers { get; set; } = MOD_ALT;
    public int TopmostVirtualKey { get; set; } = 0x58; // X

    public uint MinimizeModifiers { get; set; } = MOD_ALT;
    public int MinimizeVirtualKey { get; set; } = 0x56; // V

    public uint DebugModifiers { get; set; } = MOD_ALT;
    public int DebugVirtualKey { get; set; } = 0x72; // F3

    private static string GetPath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "IDESK", "hotkey.json");

    public static HotkeyConfig Load()
    {
        try
        {
            string json = File.ReadAllText(GetPath());
            return JsonSerializer.Deserialize<HotkeyConfig>(json) ?? new HotkeyConfig();
        }
        catch
        {
            return new HotkeyConfig();
        }
    }

    public void Save()
    {
        string path = GetPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(this));
    }

    [JsonIgnore]
    public Key Key
    {
        get => KeyInterop.KeyFromVirtualKey(VirtualKey);
        set => VirtualKey = KeyInterop.VirtualKeyFromKey(value);
    }

    [JsonIgnore]
    public Key ToggleKey
    {
        get => KeyInterop.KeyFromVirtualKey(ToggleVirtualKey);
        set => ToggleVirtualKey = KeyInterop.VirtualKeyFromKey(value);
    }

    [JsonIgnore]
    public Key ChatKey
    {
        get => KeyInterop.KeyFromVirtualKey(ChatVirtualKey);
        set => ChatVirtualKey = KeyInterop.VirtualKeyFromKey(value);
    }

    [JsonIgnore]
    public Key TranslateKey
    {
        get => KeyInterop.KeyFromVirtualKey(TranslateVirtualKey);
        set => TranslateVirtualKey = KeyInterop.VirtualKeyFromKey(value);
    }

    [JsonIgnore]
    public Key ScreenshotKey
    {
        get => KeyInterop.KeyFromVirtualKey(ScreenshotVirtualKey);
        set => ScreenshotVirtualKey = KeyInterop.VirtualKeyFromKey(value);
    }

    [JsonIgnore]
    public Key ScreenshotTranslateKey
    {
        get => KeyInterop.KeyFromVirtualKey(ScreenshotTranslateVirtualKey);
        set => ScreenshotTranslateVirtualKey = KeyInterop.VirtualKeyFromKey(value);
    }

    [JsonIgnore]
    public Key TopmostKey
    {
        get => KeyInterop.KeyFromVirtualKey(TopmostVirtualKey);
        set => TopmostVirtualKey = KeyInterop.VirtualKeyFromKey(value);
    }

    [JsonIgnore]
    public Key MinimizeKey
    {
        get => KeyInterop.KeyFromVirtualKey(MinimizeVirtualKey);
        set => MinimizeVirtualKey = KeyInterop.VirtualKeyFromKey(value);
    }

    [JsonIgnore]
    public Key DebugKey
    {
        get => KeyInterop.KeyFromVirtualKey(DebugVirtualKey);
        set => DebugVirtualKey = KeyInterop.VirtualKeyFromKey(value);
    }
}
