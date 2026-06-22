using System.Text;

namespace IDESK.Core.Agent.History;

public static class HistoryBuilder
{
    /// <summary>
    /// 将对话记录转换为上下文文本，格式：
    /// user: 消息内容
    ///
    /// you: 回复内容
    /// </summary>
    public static string BuildContext(IEnumerable<(string Role, string Content)> messages)
    {
        var sb = new StringBuilder();
        foreach (var msg in messages)
        {
            var prefix = msg.Role switch
            {
                "user" => "user",
                "system" => "system",
                _ => "you"
            };
            sb.AppendLine($"{prefix}: {msg.Content}");
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }
}
