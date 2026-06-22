namespace IDESK.Core.Agent.Prompts.ToolCalls.Choice;

public static class ChoicePrompt
{
    public const string Name = "选择题";
    public const string Description =
        "用户使用选择题会比输入文本更加方便，因此当你需要向用户收集信息时可以使用选择题。用户点击选项后会自动发送选择结果。注意：每次对话中只能使用一次选择题，不能连续出多道题。";
    public const string Format =
        """
        [CHOICE]
        Q: 题目内容
        A: 选项A
        B: 选项B
        C: 选项C
        D: 选项D
        [/CHOICE]
        """;
}
