namespace IDESK.Core.Agent.Prompts.ToolCalls.Widget;

public static class HabitPrompt
{
    public const string Name = "习惯操作";
    public const string Description =
        "当用户需要查询、添加、删除习惯或打卡时使用。";
    public const string Format =
        """
        [HABIT]
        action: HabitGetAll
        [/HABIT]
        -- 查询所有习惯及本周完成情况

        [HABIT]
        action: HabitAdd
        title: 晨跑
        [/HABIT]
        -- 创建新习惯，名称2~4个字

        [HABIT]
        action: HabitDelete
        habitId: 3
        [/HABIT]
        -- 删除指定习惯

        [HABIT]
        action: HabitToggle
        habitId: 3
        date: 2026-06-15
        [/HABIT]
        -- 切换习惯在某一天的打卡状态
        """;
}
