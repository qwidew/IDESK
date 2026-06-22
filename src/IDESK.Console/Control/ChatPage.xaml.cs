using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using IDESK.Core;
using IDESK.Core.Agent;
using IDESK.Core.Agent.History;
using IDESK.Core.Agent.Prompts;
using IDESK.Core.Agent.Prompts.ToolCalls;
using IDESK.Core.Agent.Prompts.ToolCalls.Choice;
using IDESK.Core.Agent.Prompts.ToolCalls.ClearContext;
using IDESK.Core.Agent.Prompts.ToolCalls.Compress;
using IDESK.Core.Agent.Prompts.ToolCalls.Help;
using IDESK.Core.Agent.Prompts.ToolCalls.Widget;
using IDESK.Console.Service;
using IDESK.Core.Audio;

namespace IDESK.Console.Control;

public partial class ChatPage : UserControl
{
    private readonly AgentService _agent = new();
    private readonly ChatCompactor _compactor = new();
    private readonly ChatTopicStore _topicStore = ChatTopicStore.Load();
    private readonly VoiceService _voice = new();
    private ChatHistoryConfig _history = new();
    private int _currentTopicId;
    private int? _markedTopicId;
    private bool _isWaiting;

    public ChatPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;

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
        CapsuleBody.SetResourceReference(BackgroundProperty, "SectionBgBrush");
        TopicSettingsPopup.PlacementTarget = TopicSettingsBtn;

        if (_topicStore.Topics.Count == 0)
            _topicStore.CreateTopic("默认");

        _topicStore.Save();

        // 点击空白区域取消标记
        MouseDown += (_, _) => ClearMark();

        RefreshTopicButtons();

        int startId = _topicStore.DefaultTopicId;
        if (startId == 0 || !_topicStore.Topics.Any(t => t.Id == startId))
            startId = _topicStore.Topics[0].Id;
        SwitchTopic(startId);
    }

    private void ClearMark()
    {
        if (_markedTopicId != null)
        {
            _markedTopicId = null;
            RefreshTopicButtons();
        }
    }

    // ── 主题名称编辑 ──

    private void OnTopicTitleClick(object sender, MouseButtonEventArgs e)
    {
        TopicTitle.Visibility = Visibility.Collapsed;
        TopicTitleBox.Text = TopicTitle.Content as string ?? "";
        TopicTitleBox.Visibility = Visibility.Visible;
        TopicTitleBox.Focus();
        TopicTitleBox.SelectAll();
    }

    private void OnTopicTitleKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            CommitTopicTitle();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            TopicTitleBox.Visibility = Visibility.Collapsed;
            TopicTitle.Visibility = Visibility.Visible;
            e.Handled = true;
        }
    }

    private void OnTopicTitleEndEdit(object sender, RoutedEventArgs e)
    {
        CommitTopicTitle();
    }

    private void CommitTopicTitle()
    {
        var newName = TopicTitleBox.Text.Trim();
        if (!string.IsNullOrEmpty(newName))
        {
            TopicTitle.Content = newName;
            var topic = _topicStore.Topics.FirstOrDefault(t => t.Id == _currentTopicId);
            if (topic != null)
            {
                topic.Name = newName;
                _topicStore.Save();
                RefreshTopicButtons();
            }
        }
        TopicTitleBox.Visibility = Visibility.Collapsed;
        TopicTitle.Visibility = Visibility.Visible;
    }

    private void OnTopicSettingsClick(object sender, RoutedEventArgs e)
    {
        TopicTitle.Content = _topicStore.Topics.FirstOrDefault(t => t.Id == _currentTopicId)?.Name ?? "";
        DefaultCheckMark.Visibility = _currentTopicId == _topicStore.DefaultTopicId ? Visibility.Visible : Visibility.Collapsed;
        LiteChatCheck.Visibility = _history.LiteChat ? Visibility.Visible : Visibility.Collapsed;
        TempChatCheck.Visibility = _history.TempChat ? Visibility.Visible : Visibility.Collapsed;
        NoHistoryCheck.Visibility = _history.NoHistory ? Visibility.Visible : Visibility.Collapsed;
        TopicSettingsPopup.IsOpen = true;
    }

    private void OnSetDefaultTopic(object sender, MouseButtonEventArgs e)
    {
        _topicStore.DefaultTopicId = _currentTopicId;
        _topicStore.Save();
        DefaultCheckMark.Visibility = Visibility.Visible;
    }

    private void OnToggleLiteChat(object sender, MouseButtonEventArgs e)
    {
        _history.LiteChat = !_history.LiteChat;
        LiteChatCheck.Visibility = _history.LiteChat ? Visibility.Visible : Visibility.Collapsed;
        _history.Save(_currentTopicId);
    }

    private void OnToggleTempChat(object sender, MouseButtonEventArgs e)
    {
        _history.TempChat = !_history.TempChat;
        TempChatCheck.Visibility = _history.TempChat ? Visibility.Visible : Visibility.Collapsed;
        _history.Save(_currentTopicId);
    }

    private void OnToggleNoHistory(object sender, MouseButtonEventArgs e)
    {
        _history.NoHistory = !_history.NoHistory;
        NoHistoryCheck.Visibility = _history.NoHistory ? Visibility.Visible : Visibility.Collapsed;
        _history.Save(_currentTopicId);
    }

    // ── 主题切换 ──

    private void SwitchTopic(int id)
    {
        // 保存当前主题的聊天记录（Temp / No History 不写入磁盘）
        if (_currentTopicId > 0 && !_history.TempChat && !_history.NoHistory)
            _history.Save(_currentTopicId);

        _currentTopicId = id;
        _history = ChatHistoryConfig.Load(id);

        var topic = _topicStore.Topics.FirstOrDefault(t => t.Id == id);
        TopicTitle.Content = topic?.Name ?? "";

        RefreshTopicButtons();
        RestoreHistory();
    }

    private void RefreshTopicButtons()
    {
        TopicPanel.Children.Clear();
        foreach (var topic in _topicStore.Topics)
        {
            var isActive = topic.Id == _currentTopicId;
            var isMarked = topic.Id == _markedTopicId;
            var border = new Border
            {
                Width = 40,
                Height = 40,
                CornerRadius = new CornerRadius(20),
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 4, 0, 4),
            };

            if (isMarked)
            {
                border.Background = Brushes.Red;
            }
            else
            {
                border.SetResourceReference(BackgroundProperty,
                    isActive ? "AccentBrush" : "ItemBgBrush");
            }

            UIElement? content;
            if (isMarked)
            {
                content = new Path
                {
                    Data = (StreamGeometry)FindResource("IconMinus"),
                    Fill = Brushes.White,
                    Width = 16, Height = 16,
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
            }
            else
            {
                var tb = new TextBlock
                {
                    Text = ChatTopic.GetAbbreviation(topic.Name),
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = isActive ? Brushes.White : null,
                };
                if (!isActive)
                    tb.SetResourceReference(ForegroundProperty, "TextPrimaryBrush");
                content = tb;
            }

            border.Child = content;
            border.MouseDown += (_, e) =>
            {
                if (e.ChangedButton == MouseButton.Right)
                {
                    _markedTopicId = _markedTopicId == topic.Id ? null : topic.Id;
                    RefreshTopicButtons();
                    e.Handled = true;
                }
                else if (_markedTopicId == topic.Id)
                {
                    DeleteTopic(topic.Id);
                }
                else
                {
                    _markedTopicId = null;
                    SwitchTopic(topic.Id);
                }
            };
            TopicPanel.Children.Add(border);
        }

        // 添加按钮
        var addBtn = new Border
        {
            Width = 40,
            Height = 40,
            CornerRadius = new CornerRadius(20),
            Cursor = Cursors.Hand,
            Margin = new Thickness(0, 4, 0, 4),
        };
        addBtn.SetResourceReference(BackgroundProperty, "ItemBgBrush");
        addBtn.Child = new TextBlock
        {
            Text = "+",
            FontSize = 22,
            FontWeight = FontWeights.Light,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        ((TextBlock)addBtn.Child).SetResourceReference(ForegroundProperty, "TextPrimaryBrush");
        addBtn.MouseDown += (_, _) => OnAddTopic();
        TopicPanel.Children.Add(addBtn);
    }

    private void DeleteTopic(int id)
    {
        _markedTopicId = null;
        bool wasCurrent = id == _currentTopicId;
        _topicStore.DeleteTopic(id);
        RefreshTopicButtons();

        if (wasCurrent)
        {
            if (_topicStore.Topics.Count > 0)
                SwitchTopic(_topicStore.Topics[0].Id);
            else
                SwitchTopic(_topicStore.CreateTopic("默认").Id);
        }
    }

    private void OnAddTopic()
    {
        var topic = _topicStore.CreateTopic($"新对话 {_topicStore.Topics.Count + 1}");
        SwitchTopic(topic.Id);
        _ = Dispatcher.BeginInvoke(() => InputBox.Focus());
    }

    // ── 聊天 ──

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

            var todoService = new TodoAgentService();
            string currentInput = text;

            int maxLoops = TransientConfig.Load().MaxAgentLoops;
            for (int round = 0; round < maxLoops; round++)
            {
                // 构建 prompt
                var context = _history.NoHistory ? "" : HistoryBuilder.BuildContext(
                    _history.Messages.Select(m => (m.Role, m.Content)));
                var template = _history.LiteChat ? ChatPromptLite.Template : ChatPrompt.Template;
                var prompt = template
                    .Replace("{background}", _history.Background != null ? $"对话背景：{_history.Background}\n\n" : "")
                    .Replace("{history}", context)
                    .Replace("{datetime}", DateTime.Now.ToString("yyyy年MM月dd日 HH:mm"))
                    .Replace("{tool_calls}", _history.LiteChat ? "" : ToolCallSpec.Build())
                    .Replace("{user_request}", "")
                    .Replace("{query}", currentInput);
                DebugState.LastPrompt = prompt;

                // 流式回复
                string aiContent = "";
                var (aiBorder, aiText) = AddEmptyBubble(HorizontalAlignment.Left, "SectionBgBrush", "TextPrimaryBrush");
                await _agent.SendAsync(prompt, chunk =>
                {
                    aiContent = chunk;
                    _ = Dispatcher.InvokeAsync(() => aiText.Text = chunk);
                });

                // 工具调用检测
                var todos = TodoHandler.ParseAll(aiContent);
                var habits = HabitHandler.ParseAll(aiContent);
                var schedules = ScheduleHandler.ParseAll(aiContent);
                var choice = ToolCallParser.ParseChoice(aiContent);
                var clearReq = ClearContextHandler.IsClearRequest(aiContent);
                var compress = CompressHandler.Parse(aiContent);
                var helpReq = HelpHandler.IsHelpRequest(aiContent);

                var plans = PlanHandler.ParseAll(aiContent);
                var allTodos = todos.Concat(habits).Concat(schedules).Concat(plans).ToList();
                bool hasTool = allTodos.Count > 0 || choice != null || clearReq || compress != null || helpReq;

                if (!hasTool)
                {
                    // 最终回复
                    if (!_history.NoHistory)
                    {
                        _history.Messages.Add(new ChatMessage { Role = "assistant", Content = aiContent });
                        if (!_history.TempChat)
                        {
                            _history.Save(_currentTopicId);
                            _ = TryCompactAsync();
                        }
                    }
                    break;
                }

                // 保存 AI 回复到历史（保留原文含标记，仅 UI 隐藏标记）
                if (!_history.NoHistory)
                    _history.Messages.Add(new ChatMessage { Role = "assistant", Content = aiContent });

                // 执行 [TODO] 工具调用（全部有返回值，需要 AI 继续处理）
                foreach (var todo in allTodos)
                {
                    var stripped = PlanHandler.StripAll(HabitHandler.StripAll(ScheduleHandler.StripAll(TodoHandler.StripAll(aiContent))));
                    _ = Dispatcher.InvokeAsync(() => aiText.Text = stripped);
                    var idx = MessagePanel.Children.IndexOf(aiBorder);
                    MessagePanel.Children.Insert(idx + 1, TodoPanelBuilder.BuildCallPanel(todo));
                    // 危险操作确认
                    var confirmCfg = TransientConfig.Load();
                    if (DangerousConfirm.IsDangerous(todo.Action) && confirmCfg.DangerousConfirm)
                    {
                        var msg = await BuildDangerMessage(todo, todoService);
                        var confirmed = await DangerousConfirm.ShowAsync(MessagePanel, msg);
                        if (!confirmed)
                        {
                            var cancelMsg = $"用户取消了操作：{todo.Action}";
                            MessagePanel.Children.Insert(idx + 1, TodoPanelBuilder.BuildResultPanel(cancelMsg));
                            _history.Messages.Add(new ChatMessage { Role = "system", Content = cancelMsg });
                            continue;
                        }
                    }
                    var execResult = await ExecuteTodoAction(todo, todoService);
                    MessagePanel.Children.Insert(idx + 2, TodoPanelBuilder.BuildResultPanel(execResult));
                    _history.Messages.Add(new ChatMessage { Role = "system", Content = execResult });
                }

                if (allTodos.Count > 0)
                {
                    currentInput = "请根据以上执行结果继续。";
                }

                // 选择题（无输出回馈，渲染面板后直接结束循环）
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

                if (clearReq)
                {
                    var confirmCfg = TransientConfig.Load();
                    if (confirmCfg.DangerousConfirm)
                    {
                        var msg = await BuildDangerMessage(new TodoAction { Action = "ClearContext" }, todoService);
                        if (!await DangerousConfirm.ShowAsync(MessagePanel, msg))
                        {
                            _history.Messages.Add(new ChatMessage { Role = "system", Content = "用户取消清空上下文" });
                            clearReq = false;
                        }
                    }
                    if (!clearReq) continue;
                    _history.Messages.Clear();
                    _history.Background = null;
                    if (!_history.TempChat) _history.Save(_currentTopicId);
                    MessagePanel.Children.Clear();
                    WelcomePanel.Visibility = Visibility.Visible;
                    break;
                }

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
                    var result = HelpHandler.LoadHelpContent();
                    _history.Messages.Add(new ChatMessage { Role = "system", Content = result });
                    currentInput = "请根据以上帮助文档内容回答用户的问题。";
                }

                // 没有任何工具调用触发 → 结束
                if (allTodos.Count == 0 && !clearReq && compress == null && !helpReq)
                    break;
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

    private async Task<string> ExecuteTodoAction(TodoAction todo, TodoAgentService svc)
    {
        try
        {
            return todo.Action switch
            {
                "GetAllGroups" => await ExecuteGetAllGroups(svc),
                "GetTodos" => await ExecuteGetTodos(svc, todo),
                "AddGroup" => await ExecuteAddGroup(svc, todo),
                "AddTodo" => await ExecuteAddTodo(svc, todo),
                "DeleteTodo" => await svc.DeleteTodoAsync(GetInt(todo, "groupid"), GetInt(todo, "itemid")),
                "ToggleTodo" => await svc.ToggleTodoAsync(GetInt(todo, "groupid"), GetInt(todo, "itemid")),
                "SetDdl" => await svc.SetDdlAsync(GetInt(todo, "groupid"), GetInt(todo, "itemid"), ParseDdl(todo)),
                "DeleteGroup" => await svc.DeleteGroupAsync(GetInt(todo, "groupid")),
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
        catch (Exception ex)
        {
            return $"执行失败：{ex.Message}";
        }
    }

    private async Task<string> ExecuteGetAllGroups(TodoAgentService svc)
    {
        var groups = await svc.GetAllGroupsAsync();
        if (groups.Count == 0) return "【查询结果】当前没有任何待办分组。";
        var lines = groups.Select(g => $"  - 分组ID={g.Id}，名称「{g.Name}」");
        return $"【查询结果】共有 {groups.Count} 个待办分组：\n" + string.Join("\n", lines);
    }

    private async Task<string> ExecuteGetTodos(TodoAgentService svc, TodoAction todo)
    {
        if (!int.TryParse(todo.Params.GetValueOrDefault("groupid"), out var gid))
            return "【错误】参数 groupId 缺失或无效";
        var items = await svc.GetTodosAsync(gid);
        if (items.Count == 0) return $"【查询结果】分组 {gid} 中暂无待办事项。";
        var total = items.Count;
        var done = items.Count(i => i.IsDone);
        var lines = items.Select(i =>
            $"  - 待办ID={i.Id} [{(i.IsDone ? "已完成" : "未完成")}]「{i.Content}」" +
            (i.Ddl.HasValue ? $" 截止日期：{i.Ddl:yyyy-MM-dd}" : "") +
            (i.CompleteDate.HasValue ? $" 完成于 {i.CompleteDate:yyyy-MM-dd}" : ""));
        return $"【查询结果】分组 {gid} 共有 {total} 项待办（{done} 项已完成）：\n" + string.Join("\n", lines);
    }

    private async Task<string> ExecuteAddGroup(TodoAgentService svc, TodoAction todo)
    {
        var name = todo.Params.GetValueOrDefault("name", "");
        if (string.IsNullOrEmpty(name)) return "【错误】缺少参数 name";
        var (ok, id, msg) = await svc.AddGroupAsync(name);
        return ok
            ? $"【操作成功】已创建新分组：ID={id}，名称「{msg}」"
            : $"【操作失败】{msg}";
    }

    private async Task<string> ExecuteAddTodo(TodoAgentService svc, TodoAction todo)
    {
        if (!int.TryParse(todo.Params.GetValueOrDefault("groupid"), out var gid))
            return "【错误】参数 groupId 缺失或无效";
        var content = todo.Params.GetValueOrDefault("content", "");
        if (string.IsNullOrWhiteSpace(content))
            return "【错误】参数 content 不能为空";
        DateTime? ddl = null;
        if (todo.Params.TryGetValue("ddl", out var ddlStr) && DateTime.TryParse(ddlStr, out var dt))
            ddl = dt;
        return await svc.AddTodoAsync(gid, content, ddl);
    }

    // ── 上下文压缩 ──

    private readonly object _compactLock = new();

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

    private async Task<string> BuildDangerMessage(TodoAction todo, TodoAgentService svc)
    {
        var gid = GetInt(todo, "groupid");
        var iid = GetInt(todo, "itemid");
        return todo.Action switch
        {
            "DeleteTodo" => await BuildDeleteTodoMsg(svc, gid, iid),
            "DeleteGroup" => await BuildDeleteGroupMsg(svc, gid),
            "ClearContext" => "将清空当前对话的所有历史记录，此操作不可撤销。",
            "Compress" => "将压缩当前对话历史，较早的对话将被合并为背景摘要。",
            _ => $"执行操作：{todo.Action}"
        };
    }

    private async Task<string> BuildDeleteTodoMsg(TodoAgentService svc, int gid, int iid)
    {
        var items = await svc.GetTodosAsync(gid);
        var item = items.FirstOrDefault(i => i.Id == iid);
        if (item != null)
            return $"将删除待办「{item.Content}」(ID={iid})，此操作不可撤销。";
        return $"将删除待办 ID={iid}，此操作不可撤销。";
    }

    private async Task<string> BuildDeleteGroupMsg(TodoAgentService svc, int gid)
    {
        var groups = await svc.GetAllGroupsAsync();
        var g = groups.FirstOrDefault(x => x.Id == gid);
        var name = g?.Name ?? $"ID={gid}";
        return $"将删除分组「{name}」及其所有待办事项，此操作不可撤销。";
    }

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
            _history.Save(_currentTopicId);
        }
        finally { Monitor.Exit(_compactLock); }
    }

    private async Task ForceCompactAsync(string? reason = null)
    {
        if (!Monitor.TryEnter(_compactLock)) return;
        try
        {
            var msgs = _history.Messages;
            // 保留最后一条对话（user + assistant），其余全部压缩
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
            _history.Save(_currentTopicId);

            // 显示压缩结果面板
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
