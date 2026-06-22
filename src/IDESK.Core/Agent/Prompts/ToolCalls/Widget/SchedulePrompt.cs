namespace IDESK.Core.Agent.Prompts.ToolCalls.Widget;

public static class SchedulePrompt
{
    public const string Name = "日程操作";
    public const string Description =
        "当用户需要查询、添加或删除日程时使用。";
    public const string Format =
        """
        [SCHEDULE]
        action: ScheduleGetByDate
        date: 2026-06-15
        [/SCHEDULE]
        -- 查询指定日期的所有日程

        [SCHEDULE]
        action: ScheduleGetRange
        startDate: 2026-06-15
        endDate: 2026-06-21
        [/SCHEDULE]
        -- 查询日期范围内的所有日程

        [SCHEDULE]
        action: ScheduleAdd
        date: 2026-06-15
        content: 开会
        time: 14:30
        [/SCHEDULE]
        -- 添加新日程，time 可选

        [SCHEDULE]
        action: ScheduleDelete
        itemId: 3
        [/SCHEDULE]
        -- 删除指定日程
        """;
}
