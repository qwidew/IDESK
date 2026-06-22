using IDESK.Core.Agent.History;
using IDESK.Core.Agent.Prompts;

namespace IDESK.Core.Agent;

/// <summary>
/// 对话上下文压缩器。当历史记录超过 MaxContext 的 75% 时，
/// 将前一半历史 + 现有背景发给 AI 压缩成新背景，用户无感。
/// </summary>
public class ChatCompactor
{
    private readonly AgentService _agent = new();

    /// <summary>
    /// 检查是否需要压缩，若需要则返回压缩后的背景文本，否则返回 null。
    /// </summary>
    public async Task<string?> TryCompactAsync(
        IEnumerable<(string Role, string Content)> messages,
        string? currentBackground,
        int maxContext,
        string? userReason = null)
    {
        var msgs = messages.ToList();
        var context = HistoryBuilder.BuildContext(msgs);
        if (context.Length < maxContext * 0.75 && userReason == null) return null;

        int half = msgs.Count / 2;
        if (half < 1) return null;

        var firstHalf = msgs.Take(half).ToList();
        var bgInput = currentBackground != null
            ? $"(当前背景：{currentBackground})\n\n"
            : "";
        var historyText = HistoryBuilder.BuildContext(firstHalf);
        var reasonInput = userReason != null
            ? $"\n用户要求保留的重点：{userReason}\n"
            : "";
        var prompt = CompactPrompt.System
            .Replace("{MaxContext}", (maxContext / 10).ToString())
            + bgInput + historyText + reasonInput;

        string newBg = "";
        await _agent.SendAsync(prompt, chunk => newBg = chunk);
        return newBg;
    }
}
