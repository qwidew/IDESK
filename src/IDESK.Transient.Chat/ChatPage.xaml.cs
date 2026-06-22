using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using IDESK.Core;
using IDESK.Core.Agent;
using IDESK.Core.Agent.History;
using IDESK.Core.Agent.Prompts;
using IDESK.Core.Agent.Prompts.ToolCalls;
using IDESK.Core.Agent.Prompts.ToolCalls.Choice;
using IDESK.Core.Agent.Prompts.ToolCalls.ClearContext;
using IDESK.Core.Agent.Prompts.ToolCalls.Help;
using IDESK.Core.Agent.Prompts.ToolCalls.Compress;
using IDESK.Core.Agent.Prompts.ToolCalls.Widget;
using IDESK.Core.Audio;
using IDESK.Console.Service;

namespace IDESK.Transient.Chat;

public partial class ChatPage : UserControl
{
    private readonly AgentService _agent = new();
    private readonly ChatCompactor _compactor = new();
    private readonly TodoAgentService _todoService = new();
    private readonly ChatTopicStore _topicStore = ChatTopicStore.Load();
    private readonly VoiceService _voice = new();
    private ChatHistoryConfig _history = new();
    private int _topicId;
    private bool _isWaiting;

    /// <summary>从外部向输入框填入文字（截图 OCR 结果用）</summary>
    private static event Action<string>? OnExternalText;
    public static void SendExternalText(string text) => OnExternalText?.Invoke(text);

    public ChatPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        OnExternalText += input =>
        {
            if (!_isWaiting && IsLoaded)
                _ = Dispatcher.InvokeAsync(() => SendMessageAsync(input));
        };

        _voice.TranscriptionCompleted += text =>
            _ = Dispatcher.InvokeAsync(() =>
            {
                InputBox.Text = text;
                InputBox.CaretIndex = text.Length;
                _ = SendMessageAsync();
            });
        _voice.TranscriptionFailed += err =>
            _ = Dispatcher.InvokeAsync(() => InputBox.Text = $"语音识别失败：{err}");
        _voice.RecordingStarted += () =>
            _ = Dispatcher.InvokeAsync(() =>
            {
                MicIcon.Fill = FindResource("DangerBrush") as Brush;
                MicBtn.ToolTip = "点击停止录音";
            });
        _voice.RecordingStopped += () =>
            _ = Dispatcher.InvokeAsync(() =>
            {
                MicIcon.Fill = FindResource("ActionIconBrush") as Brush;
                MicBtn.ToolTip = "语音输入";
            });
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;

        if (_topicStore.Topics.Count == 0)
            _topicStore.CreateTopic("默认");

        if (_topicStore.DefaultTopicId == 0 || !_topicStore.Topics.Any(t => t.Id == _topicStore.DefaultTopicId))
            _topicStore.DefaultTopicId = _topicStore.Topics[0].Id;

        _topicId = _topicStore.DefaultTopicId;
        _history = ChatHistoryConfig.Load(_topicId);

        var topic = _topicStore.Topics.FirstOrDefault(t => t.Id == _topicId);
        TopicTitle.Content = topic?.Name ?? "";

        RestoreHistory();

        Dispatcher.BeginInvoke(() =>
        {
            InputBox.Focus();
            Keyboard.Focus(InputBox);
        });
    }

    // ── 聊天（多轮工具调用循环） ──

    private void RestoreHistory()
    {
        MessagePanel.Children.Clear();

        foreach (var msg in _history.Messages.Where(m => m.Role != "system"))
        {
            var align = msg.Role == "user" ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            var bg = msg.Role == "user" ? "ItemBgBrush" : "SectionBgBrush";
            AddBubble(msg.Content, align, bg, "TextPrimaryBrush");
        }
        WelcomePanel.Visibility = _history.Messages.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OnSendClick(object sender, RoutedEventArgs e) => _ = SendMessageAsync();

    private void OnMicClick(object sender, RoutedEventArgs e)
    {
        if (_voice.IsRecording)
            _voice.StopRecording();
        else
            _voice.StartRecording();
    }

    private void OnInputPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
        {
            e.Handled = true;
            _ = SendMessageAsync();
        }
    }

    private async Task SendMessageAsync(string? presetText = null)
    {
        string text = (presetText ?? InputBox.Text).Trim();
        if (string.IsNullOrEmpty(text) || _isWaiting) return;

        if (presetText == null) InputBox.Clear();
        WelcomePanel.Visibility = Visibility.Collapsed;
        AddBubble(text, HorizontalAlignment.Right, "ItemBgBrush", "TextPrimaryBrush");

        _isWaiting = true;
        SendBtnLabel.Content = "…";
        InputBox.IsEnabled = false;

        try
        {
            if (!_history.NoHistory)
                _history.Messages.Add(new ChatMessage { Role = "user", Content = text });

            string currentInput = text;
            int maxLoops = TransientConfig.Load().MaxAgentLoops;

            for (int round = 0; round < maxLoops; round++)
            {
                var context = _history.NoHistory ? "" : HistoryBuilder.BuildContext(
                    _history.Messages.Select(m => (m.Role, m.Content)));
                var template = _history.LiteChat ? ChatPromptLite.Template : ChatPrompt.Template;
                var now = DateTime.Now;
                var prompt = template
                    .Replace("{background}", _history.Background != null ? $"对话背景：{_history.Background}\n\n" : "")
                    .Replace("{history}", context)
                    .Replace("{datetime}", $"{now:yyyy年MM月dd日 HH:mm} 星期{new[]{"日","一","二","三","四","五","六"}[(int)now.DayOfWeek]}")
                    .Replace("{tool_calls}", _history.LiteChat ? "" : ToolCallSpec.Build())
                    .Replace("{user_request}", "")
                    .Replace("{query}", currentInput);
                DebugState.LastPrompt = prompt;

                string aiContent = "";
                var (aiBorder, aiText) = AddEmptyBubble(HorizontalAlignment.Left, "SectionBgBrush", "TextPrimaryBrush");
                await _agent.SendAsync(prompt, chunk =>
                {
                    aiContent = chunk;
                    _ = Dispatcher.InvokeAsync(() => aiText.Text = chunk);
                });

                var todos = TodoHandler.ParseAll(aiContent);
                var habits = HabitHandler.ParseAll(aiContent);
                var schedules = ScheduleHandler.ParseAll(aiContent);
                var plans = PlanHandler.ParseAll(aiContent);
                var allTodos = todos.Concat(habits).Concat(schedules).Concat(plans).ToList();
                var choice = ToolCallParser.ParseChoice(aiContent);
                var clearReq = ClearContextHandler.IsClearRequest(aiContent);
                var compress = CompressHandler.Parse(aiContent);
                var helpReq = HelpHandler.IsHelpRequest(aiContent);

                bool hasTool = allTodos.Count > 0 || choice != null || clearReq || compress != null || helpReq;

                if (!hasTool)
                {
                    if (!_history.NoHistory)
                    {
                        _history.Messages.Add(new ChatMessage { Role = "assistant", Content = aiContent });
                        if (!_history.TempChat)
                        {
                            _history.Save(_topicId);
                            _ = TryCompactAsync();
                        }
                    }
                    break;
                }

                // 保存 AI 回复到历史
                if (!_history.NoHistory)
                    _history.Messages.Add(new ChatMessage { Role = "assistant", Content = aiContent });

                // 执行 [TODO] 工具调用
                foreach (var todo in allTodos)
                {
                    var confirmCfg = TransientConfig.Load();
                    if (DangerousConfirm.IsDangerous(todo.Action) && confirmCfg.DangerousConfirm)
                    {
                        var msg = await BuildDangerMessage(todo);
                        if (!await DangerousConfirm.ShowAsync(MessagePanel, msg))
                        {
                            var cancelMsg = $"用户取消了操作：{todo.Action}";
                            var ci = MessagePanel.Children.IndexOf(aiBorder);
                            MessagePanel.Children.Insert(ci + 1, TodoPanelBuilder.BuildResultPanel(cancelMsg));
                            _history.Messages.Add(new ChatMessage { Role = "system", Content = cancelMsg });
                            continue;
                        }
                    }
                    var stripped = PlanHandler.StripAll(HabitHandler.StripAll(ScheduleHandler.StripAll(TodoHandler.StripAll(aiContent))));
                    _ = Dispatcher.InvokeAsync(() => aiText.Text = stripped);
                    var idx = MessagePanel.Children.IndexOf(aiBorder);
                    MessagePanel.Children.Insert(idx + 1, TodoPanelBuilder.BuildCallPanel(todo));
                    var execResult = await ExecuteTodoAction(todo);
                    MessagePanel.Children.Insert(idx + 2, TodoPanelBuilder.BuildResultPanel(execResult));
                    _history.Messages.Add(new ChatMessage { Role = "system", Content = execResult });
                }

                if (allTodos.Count > 0)
                    currentInput = "请根据以上执行结果继续。";

                // 选择题
                if (choice != null)
                {
                    if (!_history.NoHistory)
                        _history.Messages.Add(new ChatMessage { Role = "assistant", Content = ToolCallParser.StripChoiceBlock(aiContent) });
                    var stripped = ToolCallParser.StripChoiceBlock(aiContent);
                    _ = Dispatcher.InvokeAsync(() => aiText.Text = stripped);
                    var idx = MessagePanel.Children.IndexOf(aiBorder);
                    var panel = ChoicePanelBuilder.Build(choice, key =>
                        _ = Dispatcher.InvokeAsync(() => SendMessageAsync($"我选 {key}")));
                    MessagePanel.Children.Insert(idx + 1, panel);
                    break;
                }

                // 清空上下文
                if (clearReq)
                {
                    var cfg = TransientConfig.Load();
                    if (cfg.DangerousConfirm)
                    {
                        if (!await DangerousConfirm.ShowAsync(MessagePanel, "将清空当前对话的所有历史记录，此操作不可撤销。"))
                        {
                            _history.Messages.Add(new ChatMessage { Role = "system", Content = "用户取消清空上下文" });
                            continue;
                        }
                    }
                    _history.Messages.Clear();
                    _history.Background = null;
                    if (!_history.TempChat) _history.Save(_topicId);
                    MessagePanel.Children.Clear();
                    WelcomePanel.Visibility = Visibility.Visible;
                    break;
                }

                // 压缩
                if (compress != null)
                {
                    var stripped = CompressHandler.StripBlock(aiContent);
                    _ = Dispatcher.InvokeAsync(() => aiText.Text = stripped);
                    _ = ForceCompactAsync(compress.Reason);
                    break;
                }

                // 帮助查询
                if (helpReq)
                {
                    var stripped = HelpHandler.StripAll(aiContent);
                    _ = Dispatcher.InvokeAsync(() => aiText.Text = stripped);
                    var idx = MessagePanel.Children.IndexOf(aiBorder);
                    var result = HelpHandler.LoadHelpContent();
                    MessagePanel.Children.Insert(idx + 1, TodoPanelBuilder.BuildResultPanel(result));
                    _history.Messages.Add(new ChatMessage { Role = "system", Content = result });
                    currentInput = "请根据以上帮助文档内容回答用户的问题。";
                }
            }
        }
        finally
        {
            _isWaiting = false;
            SendBtnLabel.Content = "发送";
            InputBox.IsEnabled = true;
            InputBox.Focus();
        }
    }

    // ── 工具执行 ──

    private async Task<string> ExecuteTodoAction(TodoAction todo)
    {
        try
        {
            return todo.Action switch
            {
                "GetAllGroups" => await ExecuteGetAllGroups(),
                "GetTodos" => await ExecuteGetTodos(todo),
                "AddGroup" => await ExecuteAddGroup(todo),
                "AddTodo" => await ExecuteAddTodo(todo),
                "DeleteTodo" => await _todoService.DeleteTodoAsync(GetInt(todo, "groupid"), GetInt(todo, "itemid")),
                "ToggleTodo" => await _todoService.ToggleTodoAsync(GetInt(todo, "groupid"), GetInt(todo, "itemid")),
                "SetDdl" => await _todoService.SetDdlAsync(GetInt(todo, "groupid"), GetInt(todo, "itemid"), ParseDdl(todo)),
                "DeleteGroup" => await _todoService.DeleteGroupAsync(GetInt(todo, "groupid")),
                "HabitGetAll" => await new HabitAgentService().GetAllHabitsAsync(),
                "HabitAdd" => await new HabitAgentService().AddHabitAsync(todo.Params.GetValueOrDefault("title", "")),
                "HabitDelete" => await new HabitAgentService().DeleteHabitAsync(GetInt(todo, "habitid")),
                "HabitToggle" => await ExecHabitToggle(todo),
                "ScheduleGetByDate" => await ExecSchedGetByDate(todo),
                "ScheduleGetRange" => await ExecSchedGetRange(todo),
                "ScheduleAdd" => await ExecSchedAdd(todo),
                "ScheduleDelete" => await new ScheduleAgentService().DeleteAsync(GetInt(todo, "itemid")),
                "PlanGetByDate" => await new PlanAgentService().GetByDateAsync(ParseDate(todo, "date") ?? DateTime.Now),
                "PlanAdd" => await new PlanAgentService().AddAsync(ParseDate(todo, "date") ?? DateTime.Now, todo.Params.GetValueOrDefault("content", ""), todo.Params.GetValueOrDefault("starttime", ""), todo.Params.GetValueOrDefault("endtime", "")),
                "PlanToggle" => await new PlanAgentService().ToggleAsync(GetInt(todo, "itemid"), ParseDate(todo, "date") ?? DateTime.Now),
                "PlanDelete" => await new PlanAgentService().DeleteAsync(GetInt(todo, "itemid"), ParseDate(todo, "date") ?? DateTime.Now),
                _ => $"未知操作：{todo.Action}"
            };
        }
        catch (Exception ex) { return $"执行失败：{ex.Message}"; }
    }

    private async Task<string> ExecuteGetAllGroups()
    {
        var groups = await _todoService.GetAllGroupsAsync();
        if (groups.Count == 0) return "【查询结果】当前没有任何待办分组。";
        var lines = groups.Select(g => $"  - 分组ID={g.Id}，名称「{g.Name}」");
        return $"【查询结果】共有 {groups.Count} 个待办分组：\n" + string.Join("\n", lines);
    }

    private async Task<string> ExecuteGetTodos(TodoAction todo)
    {
        var gid = GetInt(todo, "groupid");
        if (gid == 0) return "【错误】缺少 groupId";
        var items = await _todoService.GetTodosAsync(gid);
        if (items.Count == 0) return $"【查询结果】分组 {gid} 中暂无待办事项。";
        var total = items.Count;
        var done = items.Count(i => i.IsDone);
        var lines = items.Select(i =>
            $"  - 待办ID={i.Id} [{(i.IsDone ? "已完成" : "未完成")}]「{i.Content}」" +
            (i.Ddl.HasValue ? $" 截止日期：{i.Ddl:yyyy-MM-dd}" : "") +
            (i.CompleteDate.HasValue ? $" 完成于 {i.CompleteDate:yyyy-MM-dd}" : ""));
        return $"【查询结果】分组 {gid} 共有 {total} 项待办（{done} 项已完成）：\n" + string.Join("\n", lines);
    }

    private async Task<string> ExecuteAddGroup(TodoAction todo)
    {
        var name = todo.Params.GetValueOrDefault("name", "");
        if (string.IsNullOrEmpty(name)) return "【错误】缺少 name";
        var (ok, id, msg) = await _todoService.AddGroupAsync(name);
        return ok ? $"【操作成功】已创建新分组：ID={id}，名称「{msg}」" : $"【操作失败】{msg}";
    }

    private async Task<string> ExecuteAddTodo(TodoAction todo)
    {
        var gid = GetInt(todo, "groupid");
        var content = todo.Params.GetValueOrDefault("content", "");
        DateTime? ddl = ParseDdl(todo);
        return await _todoService.AddTodoAsync(gid, content, ddl);
    }

    private static int GetInt(TodoAction a, string key) =>
        int.TryParse(a.Params.GetValueOrDefault(key), out var v) ? v : 0;
    private static DateTime? ParseDdl(TodoAction a) =>
        a.Params.TryGetValue("ddl", out var s) && DateTime.TryParse(s, out var dt) ? dt : null;
    private static DateTime? ParseDate(TodoAction a, string key) =>
        a.Params.TryGetValue(key, out var s) && DateTime.TryParse(s, out var dt) ? dt : null;

    private async Task<string> ExecSchedGetByDate(TodoAction a)
    {
        var d = ParseDate(a, "date");
        if (d == null) return "【错误】缺少 date 参数";
        return await new ScheduleAgentService().GetByDateAsync(d.Value);
    }
    private async Task<string> ExecSchedGetRange(TodoAction a)
    {
        var s = ParseDate(a, "startdate");
        var e = ParseDate(a, "enddate");
        if (s == null || e == null) return "【错误】缺少 startDate 或 endDate 参数";
        return await new ScheduleAgentService().GetByRangeAsync(s.Value, e.Value);
    }
    private async Task<string> ExecHabitToggle(TodoAction a)
    {
        var d = ParseDate(a, "date");
        if (d == null) return "【错误】缺少 date 参数";
        return await new HabitAgentService().ToggleDayAsync(GetInt(a, "habitid"), d.Value);
    }

    private async Task<string> ExecSchedAdd(TodoAction a)
    {
        var d = ParseDate(a, "date");
        if (d == null) return "【错误】缺少 date 参数";
        return await new ScheduleAgentService().AddAsync(d.Value, a.Params.GetValueOrDefault("content", ""), a.Params.GetValueOrDefault("time", ""));
    }

    private async Task<string> BuildDangerMessage(TodoAction todo)
    {
        var gid = GetInt(todo, "groupid");
        var iid = GetInt(todo, "itemid");
        return todo.Action switch
        {
            "DeleteTodo" => await BuildDeleteTodoMsg(gid, iid),
            "DeleteGroup" => await BuildDeleteGroupMsg(gid),
            "ClearContext" => "将清空当前对话的所有历史记录，此操作不可撤销。",
            "Compress" => "将压缩当前对话历史。",
            _ => $"执行操作：{todo.Action}"
        };
    }

    private async Task<string> BuildDeleteTodoMsg(int gid, int iid)
    {
        var items = await _todoService.GetTodosAsync(gid);
        var item = items.FirstOrDefault(i => i.Id == iid);
        return item != null
            ? $"将删除待办「{item.Content}」(ID={iid})，此操作不可撤销。"
            : $"将删除待办 ID={iid}，此操作不可撤销。";
    }

    private async Task<string> BuildDeleteGroupMsg(int gid)
    {
        var groups = await _todoService.GetAllGroupsAsync();
        var g = groups.FirstOrDefault(x => x.Id == gid);
        return $"将删除分组「{g?.Name ?? $"ID={gid}"}」及其所有待办事项，此操作不可撤销。";
    }

    // ── 上下文压缩 ──

    private readonly object _compactLock = new();

    private async Task TryCompactAsync()
    {
        if (!Monitor.TryEnter(_compactLock)) return;
        try
        {
            var newBg = await _compactor.TryCompactAsync(
                _history.Messages.Select(m => (m.Role, m.Content)),
                _history.Background,
                ChatConfig.MaxContext);

            if (newBg == null) return;

            int half = _history.Messages.Count / 2;
            _history.Background = newBg;
            _history.Messages = _history.Messages.Skip(half).ToList();
            _history.Save(_topicId);
        }
        finally { Monitor.Exit(_compactLock); }
    }

    private async Task ForceCompactAsync(string? reason = null)
    {
        if (!Monitor.TryEnter(_compactLock)) return;
        try
        {
            var msgs = _history.Messages;
            int keep = Math.Min(2, msgs.Count);
            var toCompact = msgs.Take(msgs.Count - keep).ToList();
            var toKeep = msgs.Skip(msgs.Count - keep).ToList();
            if (toCompact.Count == 0) return;

            var newBg = await _compactor.TryCompactAsync(
                toCompact.Select(m => (m.Role, m.Content)),
                _history.Background,
                ChatConfig.MaxContext,
                reason);

            if (newBg == null) return;

            _history.Background = newBg;
            _history.Messages = toKeep;
            _history.Save(_topicId);

            var panel = CompressPanelBuilder.Build(newBg);
            MessagePanel.Children.Add(panel);
            _ = Dispatcher.BeginInvoke(() => MessageScroll.ScrollToBottom());
        }
        finally { Monitor.Exit(_compactLock); }
    }

    // ── 气泡 ──

    private (Border, TextBox) AddEmptyBubble(HorizontalAlignment align, string bgBrush, string fgBrush)
    {
        var tb = new TextBox
        {
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap,
            IsReadOnly = true, BorderThickness = new Thickness(0),
            Background = Brushes.Transparent, IsTabStop = false,
            Padding = new Thickness(0),
        };
        tb.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, fgBrush);

        var border = new Border
        {
            HorizontalAlignment = align,
            MaxWidth = 400,
            CornerRadius = new CornerRadius(12, 12, 4, 12),
            Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(0, 0, 0, 10),
            Child = tb,
        };
        border.SetResourceReference(BackgroundProperty, bgBrush);

        MessagePanel.Children.Add(border);
        _ = Dispatcher.BeginInvoke(() => MessageScroll.ScrollToBottom());

        return (border, tb);
    }

    private void AddBubble(string text, HorizontalAlignment align, string bgBrush, string fgBrush)
    {
        var (_, tb) = AddEmptyBubble(align, bgBrush, fgBrush);
        tb.Text = text;
    }
}
