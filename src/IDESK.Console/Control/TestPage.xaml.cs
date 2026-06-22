using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using IDESK.Console.Service;

namespace IDESK.Console.Control;

public partial class TestPage : UserControl
{
    private readonly TodoAgentService _todo = new();
    private readonly HabitAgentService _habit = new();
    private readonly ScheduleAgentService _sched = new();
    private readonly PlanAgentService _plan = new();

    public TestPage()
    {
        InitializeComponent();
    }

    private async void OnGetGroups(object sender, RoutedEventArgs e)
    {
        try
        {
            var groups = await _todo.GetAllGroupsAsync();
            GroupsOutput.Text = groups.Count == 0
                ? "（无分组）"
                : $"共有 {groups.Count} 个待办分组：\n" + string.Join("\n", groups.Select(g => $"  - 分组ID={g.Id}，名称「{g.Name}」"));
        }
        catch (System.Exception ex)
        {
            GroupsOutput.Text = $"错误：{ex.Message}";
        }
    }

    private async void OnGetTodos(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!int.TryParse(GetTodosGroupId.Text.Trim(), out int id))
            { TodosOutput.Text = "请输入有效的 GroupId"; return; }

            var items = await _todo.GetTodosAsync(id);
            if (items.Count == 0)
            { TodosOutput.Text = "（空）"; return; }

            TodosOutput.Text = string.Join("\n", items.Select(i =>
                $"  ID={i.Id} [{(i.IsDone ? "✓" : "○")}] {i.Content}" +
                (i.Ddl.HasValue ? $" (DDL: {i.Ddl:yyyy-MM-dd})" : "") +
                (i.CompleteDate.HasValue ? $" 完成于 {i.CompleteDate:yyyy-MM-dd}" : "")));
        }
        catch (System.Exception ex)
        {
            TodosOutput.Text = $"错误：{ex.Message}";
        }
    }

    private async void OnAddGroup(object sender, RoutedEventArgs e)
    {
        try
        {
            var name = AddGroupName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            { AddGroupOutput.Text = "请输入组名"; return; }

            var (ok, id, msg) = await _todo.AddGroupAsync(name);
            AddGroupOutput.Text = ok ? $"已添加：Id={id}, Name={msg}" : $"失败：{msg}";
        }
        catch (System.Exception ex)
        {
            AddGroupOutput.Text = $"错误：{ex.Message}";
        }
    }

    private async void OnAddTodo(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!int.TryParse(AddTodoGroupId.Text.Trim(), out int id))
            { AddTodoOutput.Text = "请输入有效的 GroupId"; return; }
            var content = AddTodoContent.Text.Trim();

            DateTime? ddl = null;
            if (!string.IsNullOrWhiteSpace(AddTodoDdl.Text))
            {
                if (DateTime.TryParse(AddTodoDdl.Text.Trim(), out var dt))
                    ddl = dt;
                else
                { AddTodoOutput.Text = "DDL 格式无效（示例：2026-06-15）"; return; }
            }

            var result = await _todo.AddTodoAsync(id, content, ddl);
            AddTodoOutput.Text = result;
        }
        catch (System.Exception ex)
        {
            AddTodoOutput.Text = $"错误：{ex.Message}";
        }
    }

    private async void OnDeleteTodo(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(DelTodoGroupId.Text.Trim(), out var gid)) { DeleteTodoOutput.Text = "GroupId 无效"; return; }
        if (!int.TryParse(DelTodoItemId.Text.Trim(), out var iid)) { DeleteTodoOutput.Text = "ItemId 无效"; return; }
        DeleteTodoOutput.Text = await _todo.DeleteTodoAsync(gid, iid);
    }

    private async void OnToggleTodo(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(TogTodoGroupId.Text.Trim(), out var gid)) { ToggleTodoOutput.Text = "GroupId 无效"; return; }
        if (!int.TryParse(TogTodoItemId.Text.Trim(), out var iid)) { ToggleTodoOutput.Text = "ItemId 无效"; return; }
        ToggleTodoOutput.Text = await _todo.ToggleTodoAsync(gid, iid);
    }

    private async void OnSetDdl(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(DdlGroupId.Text.Trim(), out var gid)) { SetDdlOutput.Text = "GroupId 无效"; return; }
        if (!int.TryParse(DdlItemId.Text.Trim(), out var iid)) { SetDdlOutput.Text = "ItemId 无效"; return; }
        DateTime? ddl = null;
        if (!string.IsNullOrWhiteSpace(DdlDate.Text) && DateTime.TryParse(DdlDate.Text.Trim(), out var dt)) ddl = dt;
        SetDdlOutput.Text = await _todo.SetDdlAsync(gid, iid, ddl);
    }

    private async void OnDeleteGroup(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(DelGroupId.Text.Trim(), out var gid)) { DeleteGroupOutput.Text = "GroupId 无效"; return; }
        DeleteGroupOutput.Text = await _todo.DeleteGroupAsync(gid);
    }

    private async void OnHabitGetAll(object sender, RoutedEventArgs e) =>
        HabitGetAllOutput.Text = await _habit.GetAllHabitsAsync();

    private async void OnHabitAdd(object sender, RoutedEventArgs e)
    {
        var name = HabitAddName.Text.Trim();
        if (string.IsNullOrEmpty(name)) { HabitAddOutput.Text = "请输入名称"; return; }
        HabitAddOutput.Text = await _habit.AddHabitAsync(name);
    }

    private async void OnHabitDelete(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(HabitDelId.Text.Trim(), out var id)) { HabitDelOutput.Text = "HabitId 无效"; return; }
        HabitDelOutput.Text = await _habit.DeleteHabitAsync(id);
    }

    private async void OnHabitToggle(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(HabitTogId.Text.Trim(), out var id)) { HabitTogOutput.Text = "HabitId 无效"; return; }
        if (!DateTime.TryParse(HabitTogDate.Text.Trim(), out var date)) { HabitTogOutput.Text = "日期无效"; return; }
        HabitTogOutput.Text = await _habit.ToggleDayAsync(id, date);
    }

    private async void OnSchedGetByDate(object sender, RoutedEventArgs e)
    {
        if (!DateTime.TryParse(SchedDate.Text.Trim(), out var d)) { SchedDateOutput.Text = "日期无效"; return; }
        SchedDateOutput.Text = await _sched.GetByDateAsync(d);
    }

    private async void OnSchedGetRange(object sender, RoutedEventArgs e)
    {
        if (!DateTime.TryParse(SchedRangeStart.Text.Trim(), out var s)) { SchedRangeOutput.Text = "开始日期无效"; return; }
        if (!DateTime.TryParse(SchedRangeEnd.Text.Trim(), out var en)) { SchedRangeOutput.Text = "结束日期无效"; return; }
        SchedRangeOutput.Text = await _sched.GetByRangeAsync(s, en);
    }

    private async void OnSchedAdd(object sender, RoutedEventArgs e)
    {
        if (!DateTime.TryParse(SchedAddDate.Text.Trim(), out var d)) { SchedAddOutput.Text = "日期无效"; return; }
        SchedAddOutput.Text = await _sched.AddAsync(d, SchedAddContent.Text.Trim(), SchedAddTime.Text.Trim());
    }

    private async void OnSchedDelete(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(SchedDelId.Text.Trim(), out var id)) { SchedDelOutput.Text = "ItemId 无效"; return; }
        SchedDelOutput.Text = await _sched.DeleteAsync(id);
    }

    private async void OnPlanGetByDate(object sender, RoutedEventArgs e)
    {
        if (!DateTime.TryParse(PlanDate.Text.Trim(), out var d)) { PlanDateOutput.Text = "日期无效"; return; }
        PlanDateOutput.Text = await _plan.GetByDateAsync(d);
    }

    private async void OnPlanAdd(object sender, RoutedEventArgs e)
    {
        if (!DateTime.TryParse(PlanAddDate.Text.Trim(), out var d)) { PlanAddOutput.Text = "日期无效"; return; }
        PlanAddOutput.Text = await _plan.AddAsync(d, PlanAddContent.Text.Trim(), PlanAddStart.Text.Trim(), PlanAddEnd.Text.Trim());
    }

    private async void OnPlanToggle(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(PlanTogId.Text.Trim(), out var id)) { PlanTogOutput.Text = "ItemId 无效"; return; }
        if (!DateTime.TryParse(PlanTogDate.Text.Trim(), out var d)) { PlanTogOutput.Text = "日期无效"; return; }
        PlanTogOutput.Text = await _plan.ToggleAsync(id, d);
    }

    private async void OnPlanDelete(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(PlanDelId.Text.Trim(), out var id)) { PlanDelOutput.Text = "ItemId 无效"; return; }
        if (!DateTime.TryParse(PlanDelDate.Text.Trim(), out var d)) { PlanDelOutput.Text = "日期无效"; return; }
        PlanDelOutput.Text = await _plan.DeleteAsync(id, d);
    }
}
