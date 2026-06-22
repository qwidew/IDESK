using System.IO;
using System.Text.Json;

namespace IDESK.Core;

/// <summary>
/// 临时窗口全局配置，存储在 %LOCALAPPDATA%/IDESK/transient.json。
/// </summary>
public class TransientConfig
{
    /// <summary>临时窗口失焦时自动关闭，默认开启。</summary>
    public bool AutoCloseOnDeactivated { get; set; } = true;
    /// <summary>危险操作（删除/清空）执行前弹出确认框，默认开启。</summary>
    public bool DangerousConfirm { get; set; } = true;
    /// <summary>显示调试页面，默认关闭。</summary>
    public bool DebugPageVisible { get; set; }
    /// <summary>AI 工具调用最大循环次数，默认 5。</summary>
    public int MaxAgentLoops { get; set; } = 5;
    /// <summary>开机自启时不打开主控制台。</summary>
    public bool StartMinimized { get; set; }

    private static string GetPath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "IDESK", "transient.json");

    public static TransientConfig Load()
    {
        try
        {
            string json = File.ReadAllText(GetPath());
            return JsonSerializer.Deserialize<TransientConfig>(json) ?? new TransientConfig();
        }
        catch
        {
            return new TransientConfig();
        }
    }

    public void Save()
    {
        string path = GetPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(this));
    }
}
