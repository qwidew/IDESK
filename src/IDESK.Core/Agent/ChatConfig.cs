namespace IDESK.Core.Agent;

public static class ChatConfig
{
    /// <summary>上下文窗口最大字符数（历史记录部分），超过 75% 触发压缩。</summary>
    public static int MaxContext => LlmConfig.Load().MaxContext;
}
