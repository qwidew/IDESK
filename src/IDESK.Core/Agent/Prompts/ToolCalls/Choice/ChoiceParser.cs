using System.Text.RegularExpressions;

namespace IDESK.Core.Agent.Prompts.ToolCalls.Choice;

public class ChoiceData
{
    public string Question { get; set; } = "";
    public Dictionary<string, string> Options { get; set; } = [];
}

public static class ToolCallParser
{
    private static readonly Regex ChoiceRegex = new(
        @"\[CHOICE\]\s*Q:\s*(.+?)(?:\r?\n)((?:[A-Z]:\s*.+?(?:\r?\n|$))+)s*\[/CHOICE\]",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex StripChoiceRegex = new(
        @"\s*\[CHOICE\].*?\[/CHOICE\]\s*",
        RegexOptions.Singleline | RegexOptions.Compiled);

    public static ChoiceData? ParseChoice(string text)
    {
        var m = ChoiceRegex.Match(text);
        if (!m.Success) return null;

        var result = new ChoiceData { Question = m.Groups[1].Value.Trim() };
        var optionsText = m.Groups[2].Value;
        foreach (var line in optionsText.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var t = line.Trim();
            if (t.Length >= 3 && t[1] == ':')
            {
                var key = t[0].ToString().ToUpper();
                var value = t[2..].Trim();
                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                    result.Options[key] = value;
            }
        }
        return result;
    }

    /// <summary>从文本中移除 [CHOICE] 块，保留其余内容。</summary>
    public static string StripChoiceBlock(string text) =>
        StripChoiceRegex.Replace(text, "").Trim();
}
