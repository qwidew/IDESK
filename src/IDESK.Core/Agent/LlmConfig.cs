using System.IO;
using System.Text.Json;

namespace IDESK.Core.Agent;

public class LlmConfig
{
    public string Provider { get; set; } = "openai";
    public string Url { get; set; } = "https://api.openai.com/v1";
    public string Key { get; set; } = "";
    public string Model { get; set; } = "gpt-4o-mini";
    /// <summary>上下文窗口最大字符数，超过 75% 自动压缩对话历史。</summary>
    public int MaxContext { get; set; } = 4000;

    private static string GetPath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "IDESK", "llm.json");

    public static LlmConfig Load()
    {
        try
        {
            string json = File.ReadAllText(GetPath());
            return JsonSerializer.Deserialize<LlmConfig>(json) ?? new LlmConfig();
        }
        catch
        {
            return new LlmConfig();
        }
    }

    public void Save()
    {
        string path = GetPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(this));
    }
}
