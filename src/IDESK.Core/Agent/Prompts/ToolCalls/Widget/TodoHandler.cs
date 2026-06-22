using System.Text.RegularExpressions;

namespace IDESK.Core.Agent.Prompts.ToolCalls.Widget;

public class TodoAction
{
    public string Action { get; set; } = "";
    public Dictionary<string, string> Params { get; set; } = [];
}

public static class TodoHandler
{
    private static readonly Regex TodoRegex = new(
        @"\[TODO\]\s*(.*?)\s*\[/TODO\]",
        RegexOptions.Singleline | RegexOptions.Compiled);

    /// <summary>解析 AI 回复中的所有 [TODO] 块，返回列表。</summary>
    public static List<TodoAction> ParseAll(string text)
    {
        var results = new List<TodoAction>();
        foreach (Match m in TodoRegex.Matches(text))
        {
            var body = m.Groups[1].Value.Trim();
            var lines = body.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var action = new TodoAction();
            foreach (var line in lines)
            {
                var parts = line.Split(':', 2);
                if (parts.Length != 2) continue;
                var key = parts[0].Trim().ToLower();
                var val = parts[1].Trim();
                if (key == "action") action.Action = val;
                else action.Params[key] = val;
            }
            if (!string.IsNullOrEmpty(action.Action))
                results.Add(action);
        }
        return results;
    }

    /// <summary>从文本中移除所有 [TODO] 块。</summary>
    public static string StripAll(string text) =>
        TodoRegex.Replace(text, "").Trim();
}
