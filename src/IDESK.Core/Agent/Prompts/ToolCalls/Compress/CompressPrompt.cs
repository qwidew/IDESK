namespace IDESK.Core.Agent.Prompts.ToolCalls.Compress;

public static class CompressPrompt
{
    public const string Name = "压缩对话";
    public const string Description =
        "当需要主动压缩对话历史、或响应用户压缩要求时使用。可以指定保留重点。";
    public const string Format =
        """
        [COMPRESS]
        reason: 需要保留的重点内容
        [/COMPRESS]
        """;
}
