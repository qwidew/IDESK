using System.Collections.ObjectModel;
using System.ComponentModel;
using IDESK.Widgets.Habit.Models;
using IDESK.Widgets.Habit.Service;

namespace IDESK.Widgets.Habit;

public class HabitGridViewModel : INotifyPropertyChanged
{
    private readonly IHabitService _service;

    public ObservableCollection<HabitRowViewModel> Rows { get; } = [];
    public List<DayHeader> Days { get; } = [];
    public DateTime WeekStart { get; set; }

    public string WeekLabel => $"{WeekStart:MM/dd} - {WeekStart.AddDays(6):MM/dd}";

    public HabitGridViewModel(IHabitService service)
    {
        _service = service;
        InitWeek(DateTime.Today);
    }

    public void InitWeek(DateTime date)
    {
        int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        WeekStart = date.AddDays(-diff);
        RebuildDays();
    }

    public void MoveWeek(int offset)
    {
        WeekStart = WeekStart.AddDays(offset * 7);
        RebuildDays();
        Notify(nameof(WeekLabel));
    }

    private void RebuildDays()
    {
        Days.Clear();
        for (int i = 0; i < 7; i++)
        {
            var d = WeekStart.AddDays(i);
            Days.Add(new DayHeader { Date = d, Label = $"{d:ddd}\n{d:MM/dd}" });
        }
    }

    public async Task LoadAsync()
    {
        Rows.Clear();
        var habits = await _service.GetAllHabitsAsync();
        foreach (var h in habits)
        {
            var completed = await _service.GetCompletedDatesAsync(h.Id, WeekStart, WeekStart.AddDays(6));
            Rows.Add(new HabitRowViewModel
            {
                Habit = h,
                Completed = Enumerable.Range(0, 7).Select(i => completed.Contains(WeekStart.AddDays(i))).ToArray(),
            });
        }
    }

    public async Task<HabitConfig> AddEmptyHabitAsync()
    {
        var habit = new HabitConfig { Title = "新习惯" };
        await _service.AddHabitAsync(habit);
        return habit;
    }

    public async Task UpdateTitleAsync(int id, string title)
    {
        var habit = Rows.FirstOrDefault(r => r.Habit.Id == id)?.Habit;
        if (habit != null)
        {
            habit.Title = title;
            await _service.UpdateHabitAsync(habit);
        }
    }

    public async Task DeleteHabitAsync(int id)
    {
        await _service.DeleteHabitAsync(id);
    }

    public async Task ToggleAsync(int habitId, int dayIndex)
    {
        var date = WeekStart.AddDays(dayIndex);
        await _service.ToggleCompleteAsync(habitId, date);
        var row = Rows.FirstOrDefault(r => r.Habit.Id == habitId);
        if (row != null)
            row.Completed[dayIndex] = !row.Completed[dayIndex];
    }

    private void Notify(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

    public event PropertyChangedEventHandler? PropertyChanged;
}

public class HabitRowViewModel
{
    public HabitConfig Habit { get; set; } = null!;
    public bool[] Completed { get; set; } = [];
}

public class DayHeader
{
    public DateTime Date { get; set; }
    public string Label { get; set; } = "";
}
