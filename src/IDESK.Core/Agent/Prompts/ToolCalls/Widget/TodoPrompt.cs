namespace IDESK.Core.Agent.Prompts.ToolCalls.Widget;

public static class TodoPrompt
{
    public const string Name = "待办操作";
    public const string Description =
        "当用户需要查询、添加、删除、完成待办事项或管理待办分组时使用。支持连续多次调用。";
    public const string Format =
        """
        [TODO]
        action: GetAllGroups
        [/TODO]
        -- 查询所有待办分组，返回分组ID和名称列表

        [TODO]
        action: GetTodos
        groupId: 1
        [/TODO]
        -- 查询指定分组的全部待办事项

        [TODO]
        action: AddGroup
        name: 工作
        [/TODO]
        -- 创建新的待办分组，名称用2~4个字

        [TODO]
        action: AddTodo
        groupId: 1
        content: 买牛奶
        ddl: 2026-06-15
        [/TODO]
        -- 添加待办事项，名称一般4~6个字，不宜过长。命名风格参考用户已有的待办

        [TODO]
        action: DeleteTodo
        groupId: 1
        itemId: 3
        [/TODO]
        -- 删除指定待办

        [TODO]
        action: ToggleTodo
        groupId: 1
        itemId: 3
        [/TODO]
        -- 切换待办的完成/未完成状态

        [TODO]
        action: SetDdl
        groupId: 1
        itemId: 3
        ddl: 2026-07-01
        [/TODO]
        -- 修改待办的截止日期

        [TODO]
        action: DeleteGroup
        groupId: 2
        [/TODO]
        -- 删除整个待办分组及其所有待办
        """;
}
