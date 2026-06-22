using System.Text;
using IDESK.Core.Agent.Prompts.ToolCalls.Choice;
using IDESK.Core.Agent.Prompts.ToolCalls.ClearContext;
using IDESK.Core.Agent.Prompts.ToolCalls.Compress;
using IDESK.Core.Agent.Prompts.ToolCalls.Help;
using IDESK.Core.Agent.Prompts.ToolCalls.Widget;

namespace IDESK.Core.Agent.Prompts.ToolCalls;

/// <summary>
/// 构建 prompt 中 {tool_calls} 占位符的替换内容。
/// 动态收集所有已注册的工具调用格式说明。
/// </summary>
public static class ToolCallSpec
{
    private static readonly List<ToolDef> _tools = [];

    static ToolCallSpec()
    {
        Register(Choice.ChoicePrompt.Name, Choice.ChoicePrompt.Description, Choice.ChoicePrompt.Format);
        Register(ClearContext.ClearContextPrompt.Name, ClearContext.ClearContextPrompt.Description, ClearContextPrompt.Format);
        Register(Compress.CompressPrompt.Name, Compress.CompressPrompt.Description, CompressPrompt.Format);
        Register(Widget.TodoPrompt.Name, Widget.TodoPrompt.Description, Widget.TodoPrompt.Format);
        Register(Widget.HabitPrompt.Name, Widget.HabitPrompt.Description, Widget.HabitPrompt.Format);
        Register(Widget.SchedulePrompt.Name, Widget.SchedulePrompt.Description, Widget.SchedulePrompt.Format);
        Register(Widget.PlanPrompt.Name, Widget.PlanPrompt.Description, Widget.PlanPrompt.Format);
        Register(HelpPrompt.Name, HelpPrompt.Description, HelpPrompt.Format);
        // BasicPrompt (DATETIME) 已移除，日期时间自动注入到 prompt 的 {datetime} 占位符
    }

    public static void Register(string name, string description, string format)
    {
        _tools.Add(new ToolDef(name, description, format));
    }

    public static string Build()
    {
        if (_tools.Count == 0) return "";

        var sb = new StringBuilder();
        sb.AppendLine("以下是可以使用的工具调用格式。当你需要使用某个工具时，必须在回复中严格按照下面的格式输出，否则客户端无法识别。");
        sb.AppendLine();

        for (int i = 0; i < _tools.Count; i++)
        {
            var t = _tools[i];
            sb.AppendLine("==================================");
            sb.AppendLine($"工具：{t.Name}");
            sb.AppendLine();
            sb.AppendLine(t.Description);
            sb.AppendLine("格式：");
            sb.AppendLine(t.Format);
            sb.AppendLine();
        }

        sb.AppendLine("==================================");
        sb.AppendLine("注意：");
        sb.AppendLine("1.每个工具调用块必须严格按照上面指定的格式输出，包括标记名称和换行");
        sb.AppendLine("2.不需要使用该工具时不要在回复中输出上述内容");
        sb.AppendLine("3.一次回复中可以包含多个工具调用块（不同类型或同类型多次）");
        sb.AppendLine("4.如果用户取消了某个操作（例如删除确认时选择取消），说明用户不想要这个操作，不要再次尝试执行");
        sb.AppendLine("5.执行修改操作（删除、添加、切换状态等）之前，务必先查询当前状态（如 GetAllGroups / GetTodos），不要仅依赖对话历史，因为用户可能在其他地方手动修改了数据");

        return sb.ToString();
    }

    private record ToolDef(string Name, string Format, string Description);
}
