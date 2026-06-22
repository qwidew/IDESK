using System.Text.RegularExpressions;

namespace IDESK.Core.Agent.Prompts.ToolCalls.Widget;

public static class HabitHandler
{
    private static readonly Regex BlockRegex = new(
        @"\[HABIT\]\s*(.*?)\s*\[/HABIT\]",
        RegexOptions.Singleline | RegexOptions.Compiled);

    public static List<TodoAction> ParseAll(string text)
    {
        var results = new List<TodoAction>();
        foreach (Match m in BlockRegex.Matches(text))
            if (ParseAction(m.Groups[1].Value) is TodoAction a) results.Add(a);
        return results;
    }

    public static string StripAll(string text) => BlockRegex.Replace(text, "").Trim();

    private static TodoAction? ParseAction(string body)
    {
        var action = new TodoAction();
        foreach (var line in body.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split(':', 2);
            if (parts.Length != 2) continue;
            var key = parts[0].Trim().ToLower();
            var val = parts[1].Trim();
            if (key == "action") action.Action = val;
            else action.Params[key] = val;
        }
        return string.IsNullOrEmpty(action.Action) ? null : action;
    }
}
