using System.Text.RegularExpressions;

namespace IDESK.Core.Agent.Prompts.ToolCalls.ClearContext;

public static class ClearContextHandler
{
    private static readonly Regex ClearRegex = new(
        @"\s*\[CLEAR_CONTEXT\]\s*", RegexOptions.Compiled);

    public static bool IsClearRequest(string text) =>
        ClearRegex.IsMatch(text);

    public static string StripClearBlock(string text) =>
        ClearRegex.Replace(text, "").Trim();
}
