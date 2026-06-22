using System.Collections.ObjectModel;
using System.Windows.Controls;
using IDESK.Widgets.Schedule.Models;
using IDESK.Widgets.Schedule.Service;

namespace IDESK.Widgets.Schedule;

public partial class TodayScheduleView : UserControl
{
    private readonly IScheduleService _service;
    private readonly ObservableCollection<ScheduleItem> _items = [];

    public TodayScheduleView(IScheduleService service)
    {
        _service = service;
        InitializeComponent();
        ItemList.ItemsSource = _items;
    }

    public async Task LoadItemsAsync()
    {
        var items = await _service.GetByDateAsync(DateTime.Today);
        _items.Clear();
        foreach (var item in items.OrderBy(x => x.Time))
            _items.Add(item);
    }
}
