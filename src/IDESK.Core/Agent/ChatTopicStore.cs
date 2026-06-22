using System.IO;
using System.Text.Json;

namespace IDESK.Core.Agent;

public class ChatTopicStore
{
    public List<ChatTopic> Topics { get; set; } = [];
    public int NextId { get; set; } = 1;
    /// <summary>默认主题 ID（临时 Chat 打开时使用）。</summary>
    public int DefaultTopicId { get; set; }

    private static string GetPath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "IDESK", "Chat", "topics.json");

    public static ChatTopicStore Load()
    {
        try
        {
            string json = File.ReadAllText(GetPath());
            return JsonSerializer.Deserialize<ChatTopicStore>(json) ?? new ChatTopicStore();
        }
        catch
        {
            return new ChatTopicStore();
        }
    }

    public void Save()
    {
        string path = GetPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(this));
    }

    public ChatTopic CreateTopic(string? name = null)
    {
        var topic = new ChatTopic
        {
            Id = NextId++,
            Name = name ?? $"新对话 {Topics.Count + 1}",
        };
        Topics.Add(topic);
        Save();
        return topic;
    }

    public void DeleteTopic(int id)
    {
        Topics.RemoveAll(t => t.Id == id);
        Save();
        // 删除对应的聊天记录文件
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "IDESK", "Chat", $"Topic_{id}");
        if (Directory.Exists(dir))
            Directory.Delete(dir, recursive: true);
    }
}
