using IDESK.Widgets.Schedule.Models;

namespace IDESK.Widgets.Schedule.Service;

public interface IScheduleService
{
    Task<List<ScheduleItem>> GetByDateAsync(DateTime date);
    Task<List<ScheduleItem>> GetByRangeAsync(DateTime start, DateTime end);
    Task<HashSet<DateTime>> GetMonthDatesAsync(DateTime yearMonth);
    Task AddAsync(ScheduleItem item);
    Task DeleteAsync(int id);
    Task<bool> GetManagerCreatedAsync();
    Task SetManagerCreatedAsync();
    Task<ScheduleConfig?> GetConfigAsync();
    Task SaveConfigAsync(ScheduleConfig config);
}
