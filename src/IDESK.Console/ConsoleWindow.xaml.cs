using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using IDESK.Core;
using IDESK.Console.Control;
using IDESK.Console.Models;
using IDESK.Console.Service;
using IDESK.Widgets.Habit;
using IDESK.Widgets.Habit.Models;
using IDESK.Widgets.Habit.Service;
using IDESK.Widgets.Notes;
using IDESK.Widgets.Notes.Service;
using IDESK.Widgets.Plan;
using IDESK.Widgets.Plan.Models;
using IDESK.Widgets.Plan.Service;
using IDESK.Widgets.Schedule;
using IDESK.Widgets.Schedule.Models;
using IDESK.Widgets.Schedule.Service;
using IDESK.Widgets.Todo;
using IDESK.Widgets.Todo.Service;
using IDESK.Widgets.Translate;
using IDESK.Widgets.Translate.Models;
using IDESK.Widgets.Translate.Service;
using Microsoft.Extensions.DependencyInjection;

namespace IDESK.Console;

public partial class ConsoleWindow : CustomWindow
{
    private readonly NotesPage _notesPage;
    private readonly TodoPage _todoPage;
    private readonly SchedulePage _schedulePage;
    private readonly ChatPage _chatPage;
    private readonly TranslatePage _translatePage;
    private readonly HabitPage _habitPage;
    private readonly PlanPage _planPage;
    private readonly WidgetsPage _widgetsPage;
    private readonly SettingsPage _settingsPage;
    private readonly TestPage _testPage;
    private readonly UserControl[] _pages;
    private readonly IServiceProvider _services;
    private readonly IInstanceService _todoInstanceService;
    private readonly INotesInstanceService _notesInstanceService;
    private readonly IScheduleService _scheduleService;
    private readonly IHabitService _habitService;
    private readonly IPlanService _planService;
    private readonly IPlanLiteService _planLiteService;
    private readonly ITranslateService _translateService;
    private bool _widgetsLoaded;
    private readonly List<(TodoWindow window, TodoInstance instance)> _todoWindows = [];
    private readonly List<(NotesWindow window, NotesInstance instance)> _notesWindows = [];
    private ScheduleWindow? _scheduleWindow;
    private TodayScheduleWindow? _todayScheduleWindow;
    private HabitWindow? _habitWindow;
    private PlanWindow? _planWindow;
    private PlanLiteWindow? _planLiteWindow;
    private TranslateWindow? _translateWindow;
    private bool _initialized;
    private bool _forceShutdown;

    public ConsoleWindow(IServiceProvider services, IInstanceService todoInstanceService, INotesInstanceService notesInstanceService, IScheduleService scheduleService, IHabitService habitService, IPlanService planService, IPlanLiteService planLiteService, ITranslateService translateService)
    {
        _services = services;
        _todoInstanceService = todoInstanceService;
        _notesInstanceService = notesInstanceService;
        _scheduleService = scheduleService;
        _habitService = habitService;
        _planService = planService;
        _planLiteService = planLiteService;
        _translateService = translateService;

        _todoPage = new TodoPage(todoInstanceService);
        _todoPage.InstanceCreated += OnTodoCreated;
        TodoAgentService.GroupCreated += instance => _ = Dispatcher.InvokeAsync(() => OpenTodoWindow(instance));
        TodoAgentService.GroupDeleted += id => _ = Dispatcher.InvokeAsync(async () =>
        {
            var entry = _todoWindows.FirstOrDefault(x => x.instance.Id == id);
            if (entry.window != null) { _todoWindows.Remove(entry); entry.window.Close(); }
            _todoPage.Reload();
        });
        _todoPage.InstanceDeleteRequested += OnTodoDeleteRequested;
        _todoPage.InstanceRenamed += OnTodoRenamed;

        _notesPage = new NotesPage(notesInstanceService);
        _notesPage.InstanceCreated += OnNotesCreated;
        _notesPage.InstanceDeleteRequested += OnNotesDeleteRequested;
        _notesPage.InstanceRenamed += OnNotesRenamed;

        _schedulePage = new SchedulePage(scheduleService);
        _schedulePage.ManagerCreated += () => _ = OpenScheduleWindowAsync();
        _schedulePage.ManagerDeleteRequested += OnManagerDeleteRequested;
        _schedulePage.TodayCreated += () => _ = OpenTodayScheduleWindowAsync();
        _schedulePage.TodayDeleteRequested += OnTodayDeleteRequested;

        _habitPage = new HabitPage(habitService);
        _habitPage.Created += () => _ = OpenHabitWindowAsync();
        _habitPage.DeleteRequested += OnHabitDeleteRequested;

        _planPage = new PlanPage(planService, planLiteService);
        _planPage.PlanCreated += () => _ = OpenPlanWindowAsync();
        _planPage.PlanDeleteRequested += OnPlanDeleteRequested;
        _planPage.LiteCreated += () => _ = OpenPlanLiteWindowAsync();
        _planPage.LiteDeleteRequested += OnPlanLiteDeleteRequested;

        _translatePage = new TranslatePage(translateService);
        _translatePage.Created += () => _ = OpenTranslateWindowAsync();
        _translatePage.DeleteRequested += OnTranslateDeleteRequested;

        _chatPage = new ChatPage();
        _widgetsPage = new WidgetsPage();
        _settingsPage = new SettingsPage();
        _testPage = new TestPage();

        var cfg = TransientConfig.Load();
        _pages = [
            _chatPage, _notesPage, _todoPage, _schedulePage,
            _translatePage, _habitPage, _planPage, _widgetsPage,
            _testPage, _settingsPage
        ];
        InitializeComponent();
        DebugNavItem.Visibility = cfg.DebugPageVisible ? Visibility.Visible : Visibility.Collapsed;
        _settingsPage.DebugPageVisibilityChanged += visible =>
        {
            DebugNavItem.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            if (!visible && ContentArea.Content == _testPage)
            {
                ContentArea.Content = _chatPage;
                NavList.SelectedIndex = 0;
            }
        };
        _initialized = true;
        ContentArea.Content = _chatPage;
    }

    private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragWindow();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private void OnThemeClick(object sender, RoutedEventArgs e)
    {
        ThemeManager.Toggle();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_forceShutdown)
        {
            base.OnClosing(e);
            return;
        }
        e.Cancel = true;
        Hide();
    }

    protected override async void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        foreach (var (w, i) in _todoWindows) { WidgetWindowHelper.SavePosition(w, i); await _todoInstanceService.UpdateAsync(i); }
        foreach (var (w, i) in _notesWindows) { WidgetWindowHelper.SavePosition(w, i); await _notesInstanceService.UpdateAsync(i); }
        var cfg = new ScheduleConfig { ManagerCreated = true };
        if (_scheduleWindow != null) WidgetWindowHelper.SavePosition(_scheduleWindow, cfg);
        if (_todayScheduleWindow != null) WidgetWindowHelper.SavePosition(_todayScheduleWindow, new TodayPosition(cfg));
        await _scheduleService.SaveConfigAsync(cfg);
        if (_habitWindow != null) { var hc = new HabitWidgetConfig { Created = true }; WidgetWindowHelper.SavePosition(_habitWindow, hc); await _habitService.SaveConfigAsync(hc); }
        if (_planWindow != null) { var pc = new PlanConfig { Created = true }; WidgetWindowHelper.SavePosition(_planWindow, pc); await _planService.SaveConfigAsync(pc); }
        if (_planLiteWindow != null) { var plc = new PlanConfig { Created = true }; WidgetWindowHelper.SavePosition(_planLiteWindow, plc); await _planLiteService.SaveConfigAsync(plc); }
        if (_translateWindow != null) { var tc = new TranslateConfig { Created = true }; WidgetWindowHelper.SavePosition(_translateWindow, tc); await _translateService.SaveConfigAsync(tc); }
    }

    public async Task LoadWidgetsAsync()
    {
        if (_widgetsLoaded) return;
        _widgetsLoaded = true;
        foreach (var i in await _todoInstanceService.GetAllAsync()) OpenTodoWindow(i);
        foreach (var i in await _notesInstanceService.GetAllAsync()) OpenNotesWindow(i);
        if (await _scheduleService.GetManagerCreatedAsync())
        {
            await OpenScheduleWindowAsync();
            var cfg = await _scheduleService.GetConfigAsync();
            if (cfg?.TodayCreated == true)
                await OpenTodayScheduleWindowAsync();
        }
        if (await _habitService.GetCreatedAsync())
            await OpenHabitWindowAsync();
        if (await _planService.GetCreatedAsync())
            await OpenPlanWindowAsync();
        if (await _planLiteService.GetCreatedAsync())
            await OpenPlanLiteWindowAsync();
        if (await _translateService.GetCreatedAsync())
            await OpenTranslateWindowAsync();
    }

    protected override async void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        await LoadWidgetsAsync();
    }

    private void OnVersionClick(object sender, RoutedEventArgs e)
    {
        new VersionWindow { Owner = this }.ShowDialog();
    }

    private void OnHelpClick(object sender, RoutedEventArgs e)
    {
        new HelpWindow { Owner = this }.ShowDialog();
    }

    private void OnExitClick(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("确认退出程序？\n所有组件窗口将关闭。", "退出",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        _forceShutdown = true;
        Application.Current.Shutdown();
    }

    // ── Todo ──

    private void OnTodoCreated(TodoInstance instance) => OpenTodoWindow(instance);

    private async void OnTodoDeleteRequested(TodoInstance instance)
    {
        var entry = _todoWindows.FirstOrDefault(x => x.instance.Id == instance.Id);
        if (entry.window != null) { _todoWindows.Remove(entry); entry.window.Close(); }
        _services.GetRequiredService<ITodoDataService>().GroupId = -1;
        await _todoInstanceService.DeleteAsync(instance.Id);
        DeleteDataDir("TODO_WIDGET", instance.Id);
        _todoPage.RemoveInstance(instance);
    }

    private void OpenTodoWindow(TodoInstance instance)
    {
        var vm = _services.GetRequiredService<TodoListViewModel>();
        vm.LoadInstance(instance.Id, instance.Name);
        var window = new TodoWindow(vm) { Title = instance.Name };

        WidgetWindowHelper.RestorePosition(window, instance);
        WidgetWindowHelper.SetupAutoSave(window, instance, () => _todoInstanceService.UpdateAsync(instance));

        vm.PropertyChanged += async (_, e) =>
        {
            if (e.PropertyName == nameof(TodoListViewModel.GroupName))
            {
                instance.Name = vm.GroupName;
                window.Title = vm.GroupName;
                await _todoInstanceService.UpdateAsync(instance);
            }
        };

        _todoWindows.Add((window, instance));
        window.Show();
    }

    // ── Schedule ──

    private async Task OpenScheduleWindowAsync()
    {
        var vm = new ScheduleViewModel(_scheduleService);
        _scheduleWindow = new ScheduleWindow(vm, _scheduleService);
        var cfg = await _scheduleService.GetConfigAsync() ?? new ScheduleConfig { ManagerCreated = true };

        _scheduleWindow.Title = "日程管理";
        WidgetWindowHelper.RestorePosition(_scheduleWindow, cfg);
        WidgetWindowHelper.SetupAutoSave(_scheduleWindow, cfg, () => _scheduleService.SaveConfigAsync(cfg));

        _scheduleWindow.Show();
    }

    private async void OnManagerDeleteRequested()
    {
        if (_scheduleWindow != null)
        {
            _scheduleWindow.Close();
            _scheduleWindow = null;
        }
        var cfg = await _scheduleService.GetConfigAsync();
        if (cfg != null) { cfg.ManagerCreated = false; await _scheduleService.SaveConfigAsync(cfg); }
    }

    private async void OnTodayDeleteRequested()
    {
        if (_todayScheduleWindow != null)
        {
            _todayScheduleWindow.Close();
            _todayScheduleWindow = null;
        }
        var cfg = await _scheduleService.GetConfigAsync();
        if (cfg != null) { cfg.TodayCreated = false; await _scheduleService.SaveConfigAsync(cfg); }
    }

    private async Task OpenTodayScheduleWindowAsync()
    {
        _todayScheduleWindow = new TodayScheduleWindow(_scheduleService);
        var cfg = await _scheduleService.GetConfigAsync() ?? new ScheduleConfig { ManagerCreated = true, TodayCreated = true };

        var pos = new TodayPosition(cfg);
        WidgetWindowHelper.RestorePosition(_todayScheduleWindow, pos);
        WidgetWindowHelper.SetupAutoSave(_todayScheduleWindow, pos, () => _scheduleService.SaveConfigAsync(cfg));

        _todayScheduleWindow.Show();
        await _todayScheduleWindow.LoadDataAsync();
    }

    // ── Notes ──

    private void OnNotesCreated(NotesInstance instance) => OpenNotesWindow(instance);

    private async void OnNotesDeleteRequested(NotesInstance instance)
    {
        var entry = _notesWindows.FirstOrDefault(x => x.instance.Id == instance.Id);
        if (entry.window != null) { _notesWindows.Remove(entry); entry.window.Close(); }
        _services.GetRequiredService<INotesDataService>().GroupId = -1;
        await _notesInstanceService.DeleteAsync(instance.Id);
        DeleteDataDir("NOTES_WIDGET", instance.Id);
        _notesPage.RemoveInstance(instance);
    }

    private void OnTodoRenamed(TodoInstance instance)
    {
        var entry = _todoWindows.FirstOrDefault(x => x.instance.Id == instance.Id);
        if (entry.window != null)
            entry.window.Title = instance.Name;
    }

    private void OnNotesRenamed(NotesInstance instance)
    {
        var entry = _notesWindows.FirstOrDefault(x => x.instance.Id == instance.Id);
        if (entry.window != null)
            entry.window.Title = instance.Name;
    }

    private void OpenNotesWindow(NotesInstance instance)
    {
        var vm = _services.GetRequiredService<NotesViewModel>();
        vm.LoadInstance(instance.Id, instance.Name);
        var window = new NotesWindow(vm) { Title = instance.Name };

        WidgetWindowHelper.RestorePosition(window, instance);
        WidgetWindowHelper.SetupAutoSave(window, instance, () => _notesInstanceService.UpdateAsync(instance));

        vm.PropertyChanged += async (_, e) =>
        {
            if (e.PropertyName == nameof(NotesViewModel.Title))
            {
                instance.Name = vm.Title;
                await _notesInstanceService.UpdateAsync(instance);
                _notesPage.UpdateInstanceName(instance.Id, vm.Title);
            }
        };

        window.Deactivated += async (_, _) =>
        {
            await vm.SaveContentAsync();
        };

        _notesWindows.Add((window, instance));
        window.Show();
    }

    // ── Habit ──

    private async Task OpenHabitWindowAsync()
    {
        _habitWindow = new HabitWindow(_habitService);
        var cfg = await _habitService.GetConfigAsync() ?? new HabitWidgetConfig { Created = true };

        WidgetWindowHelper.RestorePosition(_habitWindow, cfg);
        WidgetWindowHelper.SetupAutoSave(_habitWindow, cfg, () => _habitService.SaveConfigAsync(cfg));

        _habitWindow.Show();
    }

    private async void OnHabitDeleteRequested()
    {
        if (_habitWindow != null)
        {
            _habitWindow.Close();
            _habitWindow = null;
        }
        var cfg = await _habitService.GetConfigAsync();
        if (cfg != null) { cfg.Created = false; await _habitService.SaveConfigAsync(cfg); }
    }

    // ── Plan ──

    private async Task OpenPlanWindowAsync()
    {
        _planWindow = new PlanWindow(_planService);
        var cfg = await _planService.GetConfigAsync() ?? new PlanConfig { Created = true };

        WidgetWindowHelper.RestorePosition(_planWindow, cfg);
        WidgetWindowHelper.SetupAutoSave(_planWindow, cfg, () => _planService.SaveConfigAsync(cfg));

        _planWindow.Show();
    }

    private async void OnPlanDeleteRequested()
    {
        if (_planWindow != null)
        {
            _planWindow.Close();
            _planWindow = null;
        }
        var cfg = await _planService.GetConfigAsync();
        if (cfg != null) { cfg.Created = false; await _planService.SaveConfigAsync(cfg); }
    }

    // ── Plan Lite ──

    private async Task OpenPlanLiteWindowAsync()
    {
        _planLiteWindow = new PlanLiteWindow(_planLiteService);
        var cfg = await _planLiteService.GetConfigAsync() ?? new PlanConfig { Created = true, Width = 280, Height = 500 };

        WidgetWindowHelper.RestorePosition(_planLiteWindow, cfg);
        WidgetWindowHelper.SetupAutoSave(_planLiteWindow, cfg, () => _planLiteService.SaveConfigAsync(cfg));

        _planLiteWindow.Show();
    }

    private async void OnPlanLiteDeleteRequested()
    {
        if (_planLiteWindow != null)
        {
            _planLiteWindow.Close();
            _planLiteWindow = null;
        }
        var cfg = await _planLiteService.GetConfigAsync();
        if (cfg != null) { cfg.Created = false; await _planLiteService.SaveConfigAsync(cfg); }
    }

    // ── Translate ──

    private async Task OpenTranslateWindowAsync()
    {
        _translateWindow = new TranslateWindow();
        var cfg = await _translateService.GetConfigAsync() ?? new TranslateConfig { Created = true };

        WidgetWindowHelper.RestorePosition(_translateWindow, cfg);
        WidgetWindowHelper.SetupAutoSave(_translateWindow, cfg, () => _translateService.SaveConfigAsync(cfg));

        _translateWindow.Show();
    }

    private async void OnTranslateDeleteRequested()
    {
        if (_translateWindow != null)
        {
            _translateWindow.Close();
            _translateWindow = null;
        }
        var cfg = await _translateService.GetConfigAsync();
        if (cfg != null) { cfg.Created = false; await _translateService.SaveConfigAsync(cfg); }
    }

    // ── Common ──

    private UserControl? _previousPage;

    public void NavigateTo(UserControl page)
    {
        _previousPage = ContentArea.Content as UserControl;
        _previousPage = _pages.Contains(ContentArea.Content as UserControl)
            ? (UserControl)ContentArea.Content : _previousPage;
        ContentArea.Content = page;
    }

    public void NavigateBack()
    {
        if (_previousPage != null)
            ContentArea.Content = _previousPage;
    }

    private static void DeleteDataDir(string widget, int id)
    {
        string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IDESK", "DataBase", widget, $"Group_{id}");
        if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
    }

    private void OnNavSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_initialized) return;
        int index = NavList.SelectedIndex;
        if (index >= 0 && index < _pages.Length)
        {
            ContentArea.Content = _pages[index];
            if (_pages[index] == _todoPage)
                _todoPage.Reload();
        }
    }

    private sealed class TodayPosition(ScheduleConfig config) : IWidgetPosition
    {
        public double PositionX { get => config.TodayPositionX; set => config.TodayPositionX = value; }
        public double PositionY { get => config.TodayPositionY; set => config.TodayPositionY = value; }
        public double BookmarkPositionX { get => config.TodayBookmarkPositionX; set => config.TodayBookmarkPositionX = value; }
        public double Width { get => config.TodayWidth; set => config.TodayWidth = value; }
        public double Height { get => config.TodayHeight; set => config.TodayHeight = value; }
        public int BookmarkPresetId { get => config.TodayBookmarkPresetId; set => config.TodayBookmarkPresetId = value; }
        public bool IsBookmarkMode { get => config.TodayIsBookmarkMode; set => config.TodayIsBookmarkMode = value; }
    }
}
