namespace IDESK.Core.Agent.Prompts.ToolCalls.Widget;

public static class PlanPrompt
{
    public const string Name = "计划操作";
    public const string Description =
        "当用户需要查询、添加、完成或删除每日计划时使用。";
    public const string Format =
        """
        [PLAN]
        action: PlanGetByDate
        date: 2026-06-15
        [/PLAN]
        -- 查询指定日期的所有计划

        [PLAN]
        action: PlanAdd
        date: 2026-06-15
        content: 写代码
        startTime: 09:00
        endTime: 11:00
        [/PLAN]
        -- 添加新计划，startTime 和 endTime 可选

        [PLAN]
        action: PlanToggle
        itemId: 3
        date: 2026-06-15
        [/PLAN]
        -- 切换计划的完成状态

        [PLAN]
        action: PlanDelete
        itemId: 3
        date: 2026-06-15
        [/PLAN]
        -- 删除指定计划
        """;
}
