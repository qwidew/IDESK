using IDESK.Widgets.Habit.Models;

namespace IDESK.Widgets.Habit.Service;

public interface IHabitService
{
    // Widget 窗口存在性
    Task<bool> GetCreatedAsync();
    Task SetCreatedAsync();
    Task<HabitWidgetConfig?> GetConfigAsync();
    Task SaveConfigAsync(HabitWidgetConfig config);

    // 习惯 CRUD
    Task<List<HabitConfig>> GetAllHabitsAsync();
    Task AddHabitAsync(HabitConfig habit);
    Task UpdateHabitAsync(HabitConfig habit);
    Task DeleteHabitAsync(int id);

    // 完成记录
    Task ToggleCompleteAsync(int habitId, DateTime date);
    Task<HashSet<DateTime>> GetCompletedDatesAsync(int habitId, DateTime start, DateTime end);
}
