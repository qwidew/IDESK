using System.IO;
using System.Text.Json;

namespace IDESK.Core;

/// <summary>
/// 临时窗口位置/尺寸持久化配置，复用 IWidgetPosition 接口。
/// 与 HotkeyConfig / ChatWidgetConfig 的 JSON 存取模式一致，但通用化可复用于任意 TransientWidget。
/// </summary>
public class TransientPositionConfig : IWidgetPosition
{
    public double PositionX { get; set; }
    public double PositionY { get; set; }
    public double BookmarkPositionX { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public int BookmarkPresetId { get; set; }
    public bool IsBookmarkMode { get; set; }

    private string _fileName = "";

    private string GetPath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "IDESK", _fileName);

    public void Save()
    {
        string path = GetPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(this));
    }

    public static TransientPositionConfig Load(string fileName, double defaultWidth, double defaultHeight)
    {
        try
        {
            string json = File.ReadAllText(
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "IDESK", fileName));
            var cfg = JsonSerializer.Deserialize<TransientPositionConfig>(json) ?? new TransientPositionConfig();
            cfg._fileName = fileName;
            if (cfg.Width == 0) cfg.Width = defaultWidth;
            if (cfg.Height == 0) cfg.Height = defaultHeight;
            return cfg;
        }
        catch
        {
            return new TransientPositionConfig
            {
                _fileName = fileName,
                Width = defaultWidth,
                Height = defaultHeight
            };
        }
    }
}
