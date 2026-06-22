using System.IO;
using System.Text.Json;

namespace IDESK.Core.Agent;

public class ChatHistoryConfig
{
    public List<ChatMessage> Messages { get; set; } = [];
    /// <summary>压缩后的对话背景（由 AI 自动生成，随上下文增长自动更新）。</summary>
    public string? Background { get; set; }
    /// <summary>简洁模式，仅显示关键信息。</summary>
    public bool LiteChat { get; set; }
    /// <summary>临时对话，关闭后不保留记录。</summary>
    public bool TempChat { get; set; }
    /// <summary>不发送历史记录作为上下文。</summary>
    public bool NoHistory { get; set; }

    private static string GetDir() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "IDESK", "Chat");

    private static string GetPath(int topicId) =>
        Path.Combine(GetDir(), $"Topic_{topicId}", "chat.json");

    public static ChatHistoryConfig Load(int topicId)
    {
        try
        {
            string json = File.ReadAllText(GetPath(topicId));
            return JsonSerializer.Deserialize<ChatHistoryConfig>(json) ?? new ChatHistoryConfig();
        }
        catch
        {
            return new ChatHistoryConfig();
        }
    }

    public void Save(int topicId)
    {
        string path = GetPath(topicId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(this));
    }
}
