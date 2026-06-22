namespace IDESK.Core.Agent.Prompts.ToolCalls.Help;

public static class HelpPrompt
{
    public const string Name = "帮助查询";
    public const string Description =
        "当用户询问软件使用帮助、功能介绍、快捷键、操作指南时，使用此工具查询完整的帮助文档。";
    public const string Format =
        """
        [HELP]
        [/HELP]
        """;
}
