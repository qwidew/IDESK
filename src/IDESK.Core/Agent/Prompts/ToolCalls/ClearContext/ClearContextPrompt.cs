namespace IDESK.Core.Agent.Prompts.ToolCalls.ClearContext;

public static class ClearContextPrompt
{
    public const string Name = "清空上下文";
    public const string Description =
        "当用户要求重置对话、清空历史、开始全新对话时使用。";
    public const string Format =
        """
        [CLEAR_CONTEXT]
        """;
}
