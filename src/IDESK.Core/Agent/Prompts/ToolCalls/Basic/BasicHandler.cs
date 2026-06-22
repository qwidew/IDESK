using System.Text.RegularExpressions;

namespace IDESK.Core.Agent.Prompts.ToolCalls.Basic;

public static class BasicHandler
{
    private static readonly Regex DateTimeRegex = new(
        @"\s*\[DATETIME\]\s*", RegexOptions.Compiled);

    public static bool IsDateTimeRequest(string text) =>
        DateTimeRegex.IsMatch(text);

    public static string StripBlock(string text) =>
        DateTimeRegex.Replace(text, "").Trim();

    /// <summary>执行 [DATETIME]，返回当前日期时间字符串。</summary>
    public static string ExecuteDateTime()
    {
        var now = DateTime.Now;
        var weekDays = new[] { "星期日", "星期一", "星期二", "星期三", "星期四", "星期五", "星期六" };
        return $"当前时间：{now:yyyy年MM月dd日} {weekDays[(int)now.DayOfWeek]} {now:HH:mm:ss}";
    }
}
