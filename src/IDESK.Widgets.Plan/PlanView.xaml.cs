using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using IDESK.Core;
using IDESK.Core.Agent;
using IDESK.Core.Agent.History;
using IDESK.Core.Agent.Prompts;
using IDESK.Core.Agent.Prompts.ToolCalls.Widget;
using IDESK.Core.Agent.Prompts.ToolCalls;
using IDESK.Core.Agent.Prompts.ToolCalls.Basic;
using IDESK.Core.Agent.Prompts.ToolCalls.Choice;
using IDESK.Core.Agent.Prompts.ToolCalls.ClearContext;
using IDESK.Core.Agent.Prompts.ToolCalls.Compress;
using IDESK.Widgets.Plan.Models;
using IDESK.Widgets.Habit.Service;
using IDESK.Widgets.Plan.Service;
using IDESK.Widgets.Schedule.Models;
using IDESK.Widgets.Todo.Models;
using IDESK.Widgets.Schedule.Service;
using IDESK.Widgets.Todo.Control;
using IDESK.Widgets.Todo.Service;

namespace IDESK.Widgets.Plan;

public partial class PlanView : UserControl
{
    private readonly PlanViewModel _vm;
    private readonly AgentService _agent = new();
    private readonly List<ChatMessage> _chatMessages = [];

    public PlanView(PlanViewModel vm)
    {
        _vm = vm;
        InitializeComponent();
        DataContext = vm;
    }

    private void OnDateClick(object sender, MouseButtonEventArgs e)
    {
        var win = Window.GetWindow(this);
        if (win == null) return;
        var dialog = new CalendarDialog
        {
            SelectedDate = _vm.SelectedDate,
            Topmost = true,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
        };
        dialog.DateSelected += date =>
        {
            if (date.HasValue) _vm.SelectedDate = date.Value;
        };
        dialog.Show();
    }

    // ── AI 对话 ──

    private async void OnSettleClick(object sender, RoutedEventArgs e)
    {
        await SendRawChatAsync("核算今日");
    }

    private void OnChatSendClick(object sender, RoutedEventArgs e) => _ = SendChatAsync();

    private void OnChatPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
        {
            e.Handled = true;
            _ = SendChatAsync();
        }
    }

    private async Task SendRawChatAsync(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        AddChatBubble(text, HorizontalAlignment.Right, "ItemBgBrush", "TextPrimaryBrush");
        _chatMessages.Add(new ChatMessage { Role = "user", Content = text });
        ChatSendBtn.IsEnabled = false;
        ChatSendBtn.Content = "…";
        await SendChatCoreAsync(text);
    }

    private async Task SendChatAsync()
    {
        string text = ChatInput.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;
        ChatInput.Clear();
        await SendRawChatAsync(text);
    }

    private async Task SendChatCoreAsync(string text)
    {
        try
        {
            string currentInput = text;
            int maxLoops = TransientConfig.Load().MaxAgentLoops;
            for (int round = 0; round < maxLoops; round++)
            {
                var now = DateTime.Now;
                var weekDays = new[] { "日", "一", "二", "三", "四", "五", "六" };
                var todayStr = $"{now:yyyy年MM月dd日} 星期{weekDays[(int)now.DayOfWeek]}";
                var context = HistoryBuilder.BuildContext(_chatMessages.Select(m => (m.Role, m.Content)));
                var prompt = IDESK.Core.Agent.Prompts.PlanPrompt.Template
                    .Replace("{today}", todayStr)
                    .Replace("{time}", now.ToString("HH:mm"))
                    .Replace("{history}", context)
                    .Replace("{tool_calls}", ToolCallSpec.Build())
                    .Replace("{query}", currentInput);
                DebugState.LastPrompt = prompt;

                var aiBorder = AddEmptyChatBubble(HorizontalAlignment.Left, "SectionBgBrush", "TextPrimaryBrush");
                string aiContent = "";
                await _agent.SendAsync(prompt, chunk =>
                {
                    aiContent = chunk;
                    _ = Dispatcher.InvokeAsync(() =>
                    {
                        if (aiBorder.Child is TextBox tb) tb.Text = chunk;
                    });
                });

                var todos = TodoHandler.ParseAll(aiContent);
                var habits = HabitHandler.ParseAll(aiContent);
                var scheds = ScheduleHandler.ParseAll(aiContent);
                var plans = PlanHandler.ParseAll(aiContent);
                var allTodos = todos.Concat(habits).Concat(scheds).Concat(plans).ToList();
                var choice = ToolCallParser.ParseChoice(aiContent);
                var datetimeReq = BasicHandler.IsDateTimeRequest(aiContent);

                bool hasTool = allTodos.Count > 0 || choice != null || datetimeReq;

                if (!hasTool)
                {
                    _chatMessages.Add(new ChatMessage { Role = "assistant", Content = aiContent });
                    break;
                }

                _chatMessages.Add(new ChatMessage { Role = "assistant", Content = aiContent });

                foreach (var todo in allTodos)
                {
                    var stripped = HabitHandler.StripAll(ScheduleHandler.StripAll(TodoHandler.StripAll(aiContent)));
                    if (aiBorder.Child is TextBox tb) tb.Text = stripped;
                    ChatPanel.Children.Add(TodoPanelBuilder.BuildCallPanel(todo));
                    var result = await ExecutePlanAction(todo);
                    ChatPanel.Children.Add(TodoPanelBuilder.BuildResultPanel(result));
                    _chatMessages.Add(new ChatMessage { Role = "system", Content = result });
                }

                if (choice != null)
                {
                    var stripped = ToolCallParser.StripChoiceBlock(aiContent);
                    if (aiBorder.Child is TextBox tb) tb.Text = stripped;
                    var panel = ChoicePanelBuilder.Build(choice, key =>
                        _ = Dispatcher.InvokeAsync(() => SendRawChatAsync($"我选 {key}")));
                    ChatPanel.Children.Add(panel);
                    break;
                }

                if (datetimeReq)
                {
                    var result = BasicHandler.ExecuteDateTime();
                    var stripped = BasicHandler.StripBlock(aiContent);
                    if (aiBorder.Child is TextBox tb) tb.Text = stripped;
                    ChatPanel.Children.Add(new Label
                    {
                        Content = result, FontSize = 11,
                        Foreground = Brushes.Gray, Margin = new Thickness(0, 0, 0, 4),
                    });
                    _chatMessages.Add(new ChatMessage { Role = "system", Content = result });
                }

                if (allTodos.Count == 0 && !datetimeReq) break;
                currentInput = "请根据以上执行结果继续。";
            }
        }
        finally
        {
            ChatSendBtn.IsEnabled = true;
            ChatSendBtn.Content = "发送";
        }
    }

    private Border AddEmptyChatBubble(HorizontalAlignment align, string bgBrush, string fgBrush)
    {
        var tb = new TextBox
        {
            FontSize = 13, TextWrapping = TextWrapping.Wrap,
            IsReadOnly = true, BorderThickness = new Thickness(0),
            Background = Brushes.Transparent, IsTabStop = false, Padding = new Thickness(0),
        };
        tb.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, fgBrush);

        var border = new Border
        {
            HorizontalAlignment = align,
            MaxWidth = 300,
            CornerRadius = new CornerRadius(10, 10, 4, 10),
            Padding = new Thickness(10, 6, 10, 6),
            Margin = new Thickness(0, 0, 0, 6),
            Child = tb,
        };
        border.SetResourceReference(BackgroundProperty, bgBrush);
        ChatPanel.Children.Add(border);
        _ = Dispatcher.BeginInvoke(() => ChatScroll.ScrollToBottom());
        return border;
    }

    private void AddChatBubble(string text, HorizontalAlignment align, string bgBrush, string fgBrush)
    {
        var tb = new TextBox
        {
            Text = text, FontSize = 13, TextWrapping = TextWrapping.Wrap,
            IsReadOnly = true, BorderThickness = new Thickness(0),
            Background = Brushes.Transparent, IsTabStop = false, Padding = new Thickness(0),
        };
        tb.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, fgBrush);

        var border = new Border
        {
            HorizontalAlignment = align,
            MaxWidth = 300,
            CornerRadius = new CornerRadius(10, 10, 4, 10),
            Padding = new Thickness(10, 6, 10, 6),
            Margin = new Thickness(0, 0, 0, 6),
            Child = tb,
        };
        border.SetResourceReference(BackgroundProperty, bgBrush);
        ChatPanel.Children.Add(border);
        _ = Dispatcher.BeginInvoke(() => ChatScroll.ScrollToBottom());
    }

    private async Task<string> ExecutePlanAction(TodoAction todo)
    {
        try
        {
            var planSvc = new PlanService();
            var gid = GetInt(todo, "groupid");
            var iid = GetInt(todo, "itemid");
            var d = ParseDate(todo, "date");
            return todo.Action switch
            {
                // Plan
                "PlanGetByDate" => await ExecutePlanGetByDate(planSvc, todo),
                "PlanAdd" => await ExecutePlanAdd(planSvc, todo),
                "PlanToggle" => await ExecutePlanToggle(planSvc, todo),
                "PlanDelete" => await ExecutePlanDelete(planSvc, todo),
                // Todo
                "GetAllGroups" => await ExecuteGetAllGroups(),
                "GetTodos" => await ExecuteGetTodos(gid),
                "AddTodo" => await ExecuteAddTodo(gid, todo),
                "ToggleTodo" => await ExecuteToggleTodo(gid, iid),
                "DeleteTodo" => await ExecuteDeleteTodo(gid, iid),
                // Habit
                "HabitGetAll" => await ExecuteHabitGetAll(),
                "HabitToggle" => await ExecuteHabitToggle(GetInt(todo, "habitid"), d),
                // Schedule
                "ScheduleGetByDate" => await ExecuteSchedGetByDate(d),
                "ScheduleAdd" => await ExecuteScheduleAdd(d, todo),
                "ScheduleDelete" => await ExecuteScheduleDelete(iid, d),
                _ => $"{todo.Action} 执行完成"
            };
        }
        catch (Exception ex) { return $"执行失败：{ex.Message}"; }
    }

    private async Task<string> ExecutePlanGetByDate(PlanService svc, TodoAction todo)
    {
        var d = ParseDate(todo, "date");
        if (d == null) return "【错误】缺少 date 参数";
        var items = await svc.GetByDateAsync(d.Value);
        if (items.Count == 0) return $"【查询结果】{d:MM/dd} 暂无计划。";
        var lines = items.Select(i =>
            $"  - 计划ID={i.Id} {(string.IsNullOrEmpty(i.StartTime) ? "" : $"[{i.StartTime}-{i.EndTime}]")} " +
            $"{(i.IsDone ? "[已完成]" : "[未完成]")} {i.Content}");
        return $"【查询结果】{d:MM/dd} 共有 {items.Count} 项计划：\n" + string.Join("\n", lines);
    }

    private async Task<string> ExecutePlanAdd(PlanService svc, TodoAction todo)
    {
        var d = ParseDate(todo, "date");
        if (d == null) return "【错误】缺少 date 参数";
        var content = todo.Params.GetValueOrDefault("content", "");
        if (string.IsNullOrWhiteSpace(content)) return "【错误】内容不能为空";
        var item = new PlanItem
        {
            PlannedDate = d.Value.Date,
            Content = content.Trim(),
            StartTime = (todo.Params.GetValueOrDefault("starttime") ?? "").Trim(),
            EndTime = (todo.Params.GetValueOrDefault("endtime") ?? "").Trim(),
        };
        await svc.AddAsync(item);
        return $"【操作成功】已添加计划：{item.StartTime}-{item.EndTime}「{content}」";
    }

    private async Task<string> ExecutePlanToggle(PlanService svc, TodoAction todo)
    {
        var d = ParseDate(todo, "date");
        var id = GetInt(todo, "itemid");
        if (d == null || id == 0) return "【错误】缺少参数";
        var items = await svc.GetByDateAsync(d.Value);
        var item = items.FirstOrDefault(i => i.Id == id);
        if (item == null) return $"【错误】未找到计划 ID={id}";
        item.IsDone = !item.IsDone;
        await svc.UpdateAsync(item);
        return $"【操作成功】已{(item.IsDone ? "完成" : "取消完成")}计划「{item.Content}」";
    }

    private async Task<string> ExecutePlanDelete(PlanService svc, TodoAction todo)
    {
        var d = ParseDate(todo, "date");
        var id = GetInt(todo, "itemid");
        if (d == null || id == 0) return "【错误】缺少参数";
        var items = await svc.GetByDateAsync(d.Value);
        var item = items.FirstOrDefault(i => i.Id == id);
        if (item == null) return $"【错误】未找到计划 ID={id}";
        await svc.DeleteAsync(item);
        return $"【操作成功】已删除计划 ID={id}";
    }

    private async Task<string> ExecuteGetAllGroups()
    {
        var baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IDESK", "DataBase", "TODO_WIDGET");
        if (!Directory.Exists(baseDir)) return "【查询结果】暂无待办分组。";
        var groupDirs = Directory.GetDirectories(baseDir, "Group_*");
        if (groupDirs.Length == 0) return "【查询结果】暂无待办分组。";
        var lines = new List<string>();
        foreach (var dir in groupDirs)
        {
            var idStr = dir.Split('_').Last();
            if (!int.TryParse(idStr, out var gid) || gid == 0) continue;
            var svc = new TodoDataService { GroupId = gid };
            lines.Add($"  - 分组ID={gid}，名称「{await svc.GetGroupNameAsync()}」");
        }
        return $"【查询结果】共有 {lines.Count} 个待办分组：\n" + string.Join("\n", lines);
    }

    private async Task<string> ExecuteGetTodos(int gid)
    {
        if (gid == 0) return "【错误】缺少 groupId";
        var svc = new TodoDataService { GroupId = gid };
        var items = await svc.GetItemsAsync();
        if (items.Count == 0) return $"【查询结果】分组 {gid} 暂无待办。";
        return $"【查询结果】共 {items.Count} 项待办：\n" + string.Join("\n", items.Select(i =>
            $"  - 待办ID={i.Id} [{(i.IsDone ? "已完成" : "未完成")}]「{i.Content}」{(i.Ddl.HasValue ? $" 截止：{i.Ddl:yyyy-MM-dd}" : "")}"));
    }

    private async Task<string> ExecuteAddTodo(int gid, TodoAction todo)
    {
        if (gid == 0) return "【错误】缺少 groupId";
        var content = todo.Params.GetValueOrDefault("content", "");
        if (string.IsNullOrWhiteSpace(content)) return "【错误】内容不能为空";
        var svc = new TodoDataService { GroupId = gid };
        var item = new TodoItem(content.Trim());
        if (ParseDate(todo, "ddl") is DateTime ddl) item.Ddl = ddl;
        await svc.AddItemAsync(item);
        return $"【操作成功】已添加待办「{content}」";
    }

    private async Task<string> ExecuteToggleTodo(int gid, int iid)
    {
        if (gid == 0 || iid == 0) return "【错误】缺少参数";
        var svc = new TodoDataService { GroupId = gid };
        var items = await svc.GetItemsAsync();
        var item = items.FirstOrDefault(i => i.Id == iid);
        if (item == null) return $"【错误】未找到待办 ID={iid}";
        item.IsDone = !item.IsDone;
        item.CompleteDate = item.IsDone ? DateTime.Now : null;
        await svc.UpdateItemAsync(item);
        return $"【操作成功】已{(item.IsDone ? "完成" : "取消完成")}「{item.Content}」";
    }

    private async Task<string> ExecuteDeleteTodo(int gid, int iid)
    {
        if (gid == 0 || iid == 0) return "【错误】缺少参数";
        var svc = new TodoDataService { GroupId = gid };
        var item = (await svc.GetItemsAsync()).FirstOrDefault(i => i.Id == iid);
        if (item == null) return $"【错误】未找到待办 ID={iid}";
        await svc.RemoveItemAsync(iid);
        return $"【操作成功】已删除待办「{item.Content}」";
    }

    private async Task<string> ExecuteHabitGetAll()
    {
        var svc = new HabitService();
        var habits = await svc.GetAllHabitsAsync();
        if (habits.Count == 0) return "【查询结果】暂无习惯。";
        var ws = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + (int)DayOfWeek.Monday);
        var lines = new List<string>();
        foreach (var h in habits)
        {
            var dates = await svc.GetCompletedDatesAsync(h.Id, ws, ws.AddDays(6));
            lines.Add($"  - 习惯ID={h.Id}「{h.Title}」 本周 {dates.Count}/7");
        }
        return $"【查询结果】共 {habits.Count} 个习惯：\n" + string.Join("\n", lines);
    }

    private async Task<string> ExecuteHabitToggle(int hid, DateTime? date)
    {
        if (hid == 0 || date == null) return "【错误】缺少参数";
        var svc = new HabitService();
        await svc.ToggleCompleteAsync(hid, date.Value);
        return $"【操作成功】已切换习惯 {hid} 在 {date:MM/dd} 的打卡状态";
    }

    private async Task<string> ExecuteSchedGetByDate(DateTime? date)
    {
        if (date == null) return "【错误】缺少 date 参数";
        var svc = new ScheduleService();
        var items = await svc.GetByDateAsync(date.Value);
        if (items.Count == 0) return $"【查询结果】{date.Value:MM/dd} 暂无日程。";
        var lines = items.Select(i =>
            $"  - 日程ID={i.Id} {(string.IsNullOrEmpty(i.Time) ? "" : $"[{i.Time}]")} {i.Content}");
        return $"【查询结果】{date.Value:MM/dd} 共 {items.Count} 项日程：\n" + string.Join("\n", lines);
    }

    private async Task<string> ExecuteScheduleAdd(DateTime? date, TodoAction todo)
    {
        if (date == null) date = ParseDate(todo, "date");
        if (date == null) return "【错误】缺少 date 参数";
        var content = todo.Params.GetValueOrDefault("content", "");
        if (string.IsNullOrWhiteSpace(content)) return "【错误】内容不能为空";
        var svc = new ScheduleService();
        await svc.AddAsync(new ScheduleItem { Date = date.Value.Date, Content = content.Trim(), Time = (todo.Params.GetValueOrDefault("time") ?? "").Trim() });
        return $"【操作成功】已添加日程「{content}」";
    }

    private async Task<string> ExecuteScheduleDelete(int iid, DateTime? date)
    {
        if (iid == 0) return "【错误】缺少 itemId";
        var svc = new ScheduleService();
        await svc.DeleteAsync(iid);
        return $"【操作成功】已删除日程 ID={iid}";
    }

    private static int GetInt(TodoAction a, string key) =>
        int.TryParse(a.Params.GetValueOrDefault(key), out var v) ? v : 0;
    private static DateTime? ParseDate(TodoAction a, string key) =>
        a.Params.TryGetValue(key, out var s) && DateTime.TryParse(s, out var dt) ? dt : null;
}
