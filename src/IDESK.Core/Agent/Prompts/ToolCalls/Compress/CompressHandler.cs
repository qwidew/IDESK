using System.Text.RegularExpressions;

namespace IDESK.Core.Agent.Prompts.ToolCalls.Compress;

public class CompressData
{
    public string? Reason { get; set; }
}

public static class CompressHandler
{
    private static readonly Regex CompressRegex = new(
        @"\[COMPRESS\](?:\s*reason:\s*(.+?))?\s*\[/COMPRESS\]",
        RegexOptions.Singleline | RegexOptions.Compiled);

    public static CompressData? Parse(string text)
    {
        var m = CompressRegex.Match(text);
        if (!m.Success) return null;
        return new CompressData { Reason = m.Groups[1].Success ? m.Groups[1].Value.Trim() : null };
    }

    public static string StripBlock(string text) =>
        CompressRegex.Replace(text, "").Trim();
}
