using System.Windows;
using System.Windows.Controls;
using IDESK.Widgets.Schedule.Service;

namespace IDESK.Widgets.Schedule;

public partial class ScheduleView : UserControl
{
    private readonly ScheduleViewModel _vm;
    private readonly IScheduleService _service;

    public ScheduleView(ScheduleViewModel vm, IScheduleService service)
    {
        _vm = vm;
        _service = service;
        InitializeComponent();
        ItemList.ItemsSource = _vm.Items;

        RefreshMarkedDates(_service);

        Calendar.DateSelected += async (date) =>
        {
            DateLabel.Content = $"{date.Year}/{date.Month}/{date.Day}";
            await _vm.LoadDateAsync(date);
        };
        Calendar.MonthChanged += () => RefreshMarkedDates(_service);
    }

    private async void RefreshMarkedDates(IScheduleService service)
    {
        var m = Calendar.CurrentMonth;
        Calendar.MarkedDates = await service.GetMonthDatesAsync(m);
        Calendar.RenderMonth(m);
    }

    private async void OnAddClick(object sender, RoutedEventArgs e)
    {
        string content = ContentBox.Text.Trim();
        string time = TimeBox.Text.Trim();
        if (string.IsNullOrEmpty(content) || !Calendar.SelectedDate.HasValue) return;
        await _vm.AddItemAsync(Calendar.SelectedDate.Value, content, time);
        ContentBox.Clear();
        TimeBox.Clear();
        ContentBox.Focus();
        RefreshMarkedDates(_service);
    }

    private async void OnDeleteItem(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int id)
        {
            await _vm.DeleteItemAsync(id);
            RefreshMarkedDates(_service);
        }
    }
}
