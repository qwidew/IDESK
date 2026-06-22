using Microsoft.EntityFrameworkCore;
using IDESK.Widgets.Schedule.Data;
using IDESK.Widgets.Schedule.Models;

namespace IDESK.Widgets.Schedule.Service;

public class ScheduleService : IScheduleService
{
    public static Action? DataChanged;

    private readonly AppDbContext _db;

    public ScheduleService()
    {
        _db = new AppDbContext();
        _db.Database.EnsureCreated();
    }

    public async Task<List<ScheduleItem>> GetByDateAsync(DateTime date)
    {
        return await _db.ScheduleItems
            .Where(x => x.Date == date.Date)
            .OrderBy(x => x.Time)
            .ToListAsync();
    }

    public async Task<List<ScheduleItem>> GetByRangeAsync(DateTime start, DateTime end)
    {
        return await _db.ScheduleItems
            .Where(x => x.Date >= start.Date && x.Date <= end.Date)
            .OrderBy(x => x.Date).ThenBy(x => x.Time)
            .ToListAsync();
    }

    public async Task<HashSet<DateTime>> GetMonthDatesAsync(DateTime yearMonth)
    {
        var start = new DateTime(yearMonth.Year, yearMonth.Month, 1);
        var end = start.AddMonths(1);
        return (await _db.ScheduleItems
            .Where(x => x.Date >= start && x.Date < end)
            .Select(x => x.Date)
            .ToListAsync()).ToHashSet();
    }

    public async Task AddAsync(ScheduleItem item)
    {
        _db.ScheduleItems.Add(item);
        await _db.SaveChangesAsync();
        DataChanged?.Invoke();
    }

    public async Task DeleteAsync(int id)
    {
        var item = await _db.ScheduleItems.FindAsync(id);
        if (item != null)
        {
            _db.ScheduleItems.Remove(item);
            await _db.SaveChangesAsync();
            DataChanged?.Invoke();
        }
    }

    public async Task<bool> GetManagerCreatedAsync()
    {
        var cfg = await _db.ScheduleConfigs.FirstOrDefaultAsync();
        return cfg?.ManagerCreated ?? false;
    }

    public async Task SetManagerCreatedAsync()
    {
        var cfg = await _db.ScheduleConfigs.FirstOrDefaultAsync();
        if (cfg == null)
            _db.ScheduleConfigs.Add(new ScheduleConfig { ManagerCreated = true });
        else
            cfg.ManagerCreated = true;
        await _db.SaveChangesAsync();
    }

    public async Task<ScheduleConfig?> GetConfigAsync()
    {
        return await _db.ScheduleConfigs.FirstOrDefaultAsync();
    }

    public async Task SaveConfigAsync(ScheduleConfig config)
    {
        var existing = await _db.ScheduleConfigs.FirstOrDefaultAsync();
        if (existing == null)
        {
            _db.ScheduleConfigs.Add(config);
        }
        else
        {
            existing.PositionX = config.PositionX;
            existing.PositionY = config.PositionY;
            existing.BookmarkPositionX = config.BookmarkPositionX;
            existing.Width = config.Width;
            existing.Height = config.Height;
            existing.BookmarkPresetId = config.BookmarkPresetId;
            existing.IsBookmarkMode = config.IsBookmarkMode;
            existing.TodayCreated = config.TodayCreated;
            existing.TodayPositionX = config.TodayPositionX;
            existing.TodayPositionY = config.TodayPositionY;
            existing.TodayBookmarkPositionX = config.TodayBookmarkPositionX;
            existing.TodayWidth = config.TodayWidth;
            existing.TodayHeight = config.TodayHeight;
            existing.TodayBookmarkPresetId = config.TodayBookmarkPresetId;
            existing.TodayIsBookmarkMode = config.TodayIsBookmarkMode;
        }
        await _db.SaveChangesAsync();
    }
}
