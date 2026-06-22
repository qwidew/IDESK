namespace IDESK.Core.Agent.Prompts.ToolCalls.Basic;

public static class BasicPrompt
{
    public const string Name = "基础工具";
    public const string Description =
        "提供当前日期、时间、星期等基本信息查询。";
    public const string Format =
        """
        [DATETIME]
        """;
}
