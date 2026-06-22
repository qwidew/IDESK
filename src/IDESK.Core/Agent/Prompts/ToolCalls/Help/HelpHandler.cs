using System.IO;
using System.Text.RegularExpressions;

namespace IDESK.Core.Agent.Prompts.ToolCalls.Help;

public static class HelpHandler
{
    private static readonly Regex BlockRegex = new(
        @"\[HELP\].*?\[/HELP\]",
        RegexOptions.Singleline | RegexOptions.Compiled);

    public static bool IsHelpRequest(string text) => BlockRegex.IsMatch(text);

    public static string StripAll(string text) => BlockRegex.Replace(text, "").Trim();

    public static string LoadHelpContent()
    {
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "help.md");
        return File.Exists(path) ? File.ReadAllText(path) : "暂无帮助文档";
    }
}
