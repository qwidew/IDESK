using Microsoft.EntityFrameworkCore;
using IDESK.Widgets.Habit.Data;
using IDESK.Widgets.Habit.Models;

namespace IDESK.Widgets.Habit.Service;

public class HabitService : IHabitService
{
    public static Action? DataChanged;

    private readonly AppDbContext _db;

    public HabitService()
    {
        _db = new AppDbContext();
        _db.Database.EnsureCreated();
    }

    // ── Widget 窗口存在性 ──

    public async Task<bool> GetCreatedAsync()
    {
        var cfg = await _db.HabitWidgetConfigs.FirstOrDefaultAsync();
        return cfg?.Created ?? false;
    }

    public async Task SetCreatedAsync()
    {
        var cfg = await _db.HabitWidgetConfigs.FirstOrDefaultAsync();
        if (cfg == null)
            _db.HabitWidgetConfigs.Add(new HabitWidgetConfig { Created = true });
        else
            cfg.Created = true;
        await _db.SaveChangesAsync();
    }

    public async Task<HabitWidgetConfig?> GetConfigAsync()
    {
        return await _db.HabitWidgetConfigs.FirstOrDefaultAsync();
    }

    public async Task SaveConfigAsync(HabitWidgetConfig config)
    {
        var existing = await _db.HabitWidgetConfigs.FirstOrDefaultAsync();
        if (existing == null)
        {
            _db.HabitWidgetConfigs.Add(config);
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
        }
        await _db.SaveChangesAsync();
    }

    // ── 习惯 CRUD ──

    public async Task<List<HabitConfig>> GetAllHabitsAsync()
    {
        return await _db.HabitConfigs.ToListAsync();
    }

    public async Task AddHabitAsync(HabitConfig habit)
    {
        _db.HabitConfigs.Add(habit);
        await _db.SaveChangesAsync();
        DataChanged?.Invoke();
    }

    public async Task UpdateHabitAsync(HabitConfig habit)
    {
        _db.HabitConfigs.Update(habit);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteHabitAsync(int id)
    {
        var item = await _db.HabitConfigs.FindAsync(id);
        if (item != null)
        {
            _db.HabitConfigs.Remove(item);
            await _db.SaveChangesAsync();
            DataChanged?.Invoke();
        }
    }

    // ── 完成记录 ──

    public async Task ToggleCompleteAsync(int habitId, DateTime date)
    {
        var existing = await _db.HabitRecords
            .FirstOrDefaultAsync(r => r.HabitId == habitId && r.CompletedDate == date.Date);

        if (existing != null)
            _db.HabitRecords.Remove(existing);
        else
            _db.HabitRecords.Add(new HabitRecord { HabitId = habitId, CompletedDate = date.Date });

        await _db.SaveChangesAsync();
        DataChanged?.Invoke();
    }

    public async Task<HashSet<DateTime>> GetCompletedDatesAsync(int habitId, DateTime start, DateTime end)
    {
        var dates = await _db.HabitRecords
            .Where(r => r.HabitId == habitId && r.CompletedDate >= start.Date && r.CompletedDate <= end.Date)
            .Select(r => r.CompletedDate)
            .ToListAsync();
        return dates.ToHashSet();
    }
}
