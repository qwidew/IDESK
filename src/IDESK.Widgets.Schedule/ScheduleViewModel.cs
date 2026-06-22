using System.Collections.ObjectModel;
using IDESK.Widgets.Schedule.Models;
using IDESK.Widgets.Schedule.Service;

namespace IDESK.Widgets.Schedule;

public class ScheduleViewModel
{
    private readonly IScheduleService _service;
    public ObservableCollection<ScheduleItem> Items { get; } = [];

    public ScheduleViewModel(IScheduleService service)
    {
        _service = service;
        ScheduleService.DataChanged += async () =>
        {
            if (_currentDate.HasValue) await LoadDateAsync(_currentDate.Value);
        };
    }

    private DateTime? _currentDate;

    public async Task LoadDateAsync(DateTime date)
    {
        _currentDate = date;
        var list = await _service.GetByDateAsync(date);
        Items.Clear();
        foreach (var item in list)
            Items.Add(item);
    }

    public async Task AddItemAsync(DateTime date, string content, string time)
    {
        if (string.IsNullOrWhiteSpace(content)) return;
        var item = new ScheduleItem { Date = date.Date, Content = content, Time = time };
        await _service.AddAsync(item);
        // DataChanged 事件会触发 LoadDateAsync 重新加载，无需手动 Add
    }

    public async Task DeleteItemAsync(int id)
    {
        var item = Items.FirstOrDefault(x => x.Id == id);
        if (item != null)
        {
            await _service.DeleteAsync(id);
            Items.Remove(item);
        }
    }
}
