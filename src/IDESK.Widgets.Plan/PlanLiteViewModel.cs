using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using IDESK.Core.Helper;
using IDESK.Widgets.Plan.Models;
using IDESK.Widgets.Plan.Service;

namespace IDESK.Widgets.Plan;

public class PlanLiteViewModel : INotifyPropertyChanged
{
    private readonly IPlanLiteService _service;

    public ObservableCollection<PlanItem> Items { get; } = [];

    private DateTime _selectedDate = DateTime.Today;
    public DateTime SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (_selectedDate == value) return;
            _selectedDate = value;
            Notify(nameof(SelectedDate));
            Notify(nameof(DateLabel));
            _ = LoadAsync();
        }
    }

    public string DateLabel => $"{_selectedDate:yyyy/MM/dd}  {DayOfWeekCn(_selectedDate.DayOfWeek)}";

    public ICommand PrevDayCommand { get; }
    public ICommand NextDayCommand { get; }
    public ICommand AddCommand { get; }
    public ICommand ToggleCommand { get; }
    public ICommand DeleteCommand { get; }

    private string _newContent = "";
    public string NewContent
    {
        get => _newContent;
        set { _newContent = value; Notify(nameof(NewContent)); }
    }

    private string _newStartTime = "";
    public string NewStartTime
    {
        get => _newStartTime;
        set { _newStartTime = value; Notify(nameof(NewStartTime)); }
    }

    private string _newEndTime = "";
    public string NewEndTime
    {
        get => _newEndTime;
        set { _newEndTime = value; Notify(nameof(NewEndTime)); }
    }

    public PlanLiteViewModel(IPlanLiteService service)
    {
        _service = service;
        PrevDayCommand = new RelayCommand(_ => SelectedDate = SelectedDate.AddDays(-1));
        NextDayCommand = new RelayCommand(_ => SelectedDate = SelectedDate.AddDays(1));
        AddCommand = new RelayCommand(async _ => await AddAsync(), _ => true);
        ToggleCommand = new RelayCommand(async param =>
        {
            if (param is PlanItem item)
            {
                item.IsDone = !item.IsDone;
                await _service.UpdateAsync(item);
            }
        });
        DeleteCommand = new RelayCommand(async param =>
        {
            if (param is PlanItem item) await _service.DeleteAsync(item);
        });

        PlanLiteService.DataChanged += () => _ = LoadAsync();
        _ = LoadAsync();
    }

    public async Task LoadAsync()
    {
        var list = await _service.GetByDateAsync(_selectedDate);
        Items.Clear();
        foreach (var item in list) Items.Add(item);
    }

    private async Task AddAsync()
    {
        var content = NewContent.Trim();
        if (string.IsNullOrEmpty(content)) return;
        var item = new PlanItem
        {
            PlannedDate = _selectedDate.Date,
            Content = content,
            StartTime = NewStartTime.Trim(),
            EndTime = NewEndTime.Trim(),
            SortOrder = Items.Count,
        };
        await _service.AddAsync(item);
        NewContent = "";
        NewStartTime = "";
        NewEndTime = "";
    }

    private static string DayOfWeekCn(DayOfWeek dow) => dow switch
    {
        DayOfWeek.Monday => "星期一",
        DayOfWeek.Tuesday => "星期二",
        DayOfWeek.Wednesday => "星期三",
        DayOfWeek.Thursday => "星期四",
        DayOfWeek.Friday => "星期五",
        DayOfWeek.Saturday => "星期六",
        DayOfWeek.Sunday => "星期日",
        _ => ""
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify(string p) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
}
