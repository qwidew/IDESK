using System.Windows;
using System.Windows.Controls;
using IDESK.Widgets.Schedule.Models;
using IDESK.Widgets.Schedule.Service;

namespace IDESK.Console.Control;

public partial class SchedulePage : UserControl
{
    private readonly IScheduleService _scheduleService;
    private readonly TodayScheduleBlock _todayCard;
    public event Action? ManagerCreated;
    public event Action? ManagerDeleteRequested;
    public event Action? TodayCreated;
    public event Action? TodayDeleteRequested;

    public SchedulePage(IScheduleService scheduleService)
    {
        _scheduleService = scheduleService;
        _todayCard = new TodayScheduleBlock();
        InitializeComponent();
        TodayCardContainer.Content = _todayCard;

        ManagerCard.DeleteClicked += () =>
        {
            ManagerCard.Visibility = Visibility.Collapsed;
            CreateManagerBtn.Visibility = Visibility.Visible;
            CreateTodayBtn.Visibility = Visibility.Collapsed;
            TodayCardContainer.Visibility = Visibility.Collapsed;
            ManagerDeleteRequested?.Invoke();
        };

        _todayCard.DeleteClicked += () =>
        {
            TodayCardContainer.Visibility = Visibility.Collapsed;
            CreateTodayBtn.Visibility = Visibility.Visible;
            TodayDeleteRequested?.Invoke();
        };

        Loaded += async (_, _) =>
        {
            var cfg = await _scheduleService.GetConfigAsync();
            if (cfg?.ManagerCreated == true)
            {
                CreateManagerBtn.Visibility = Visibility.Collapsed;
                ManagerCard.Visibility = Visibility.Visible;
                if (cfg.TodayCreated != true)
                    CreateTodayBtn.Visibility = Visibility.Visible;
                else
                    TodayCardContainer.Visibility = Visibility.Visible;
            }
        };
    }

    private async void OnCreateManagerClick(object sender, RoutedEventArgs e)
    {
        CreateManagerBtn.Visibility = Visibility.Collapsed;
        ManagerCard.Visibility = Visibility.Visible;
        CreateTodayBtn.Visibility = Visibility.Visible;
        await _scheduleService.SetManagerCreatedAsync();
        ManagerCreated?.Invoke();
    }

    private async void OnCreateTodayClick(object sender, RoutedEventArgs e)
    {
        CreateTodayBtn.Visibility = Visibility.Collapsed;
        TodayCardContainer.Visibility = Visibility.Visible;
        var cfg = await _scheduleService.GetConfigAsync() ?? new ScheduleConfig { ManagerCreated = true };
        cfg.TodayCreated = true;
        await _scheduleService.SaveConfigAsync(cfg);
        TodayCreated?.Invoke();
    }
}
