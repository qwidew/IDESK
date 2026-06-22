using Microsoft.EntityFrameworkCore;
using IDESK.Widgets.Plan.Data;
using IDESK.Widgets.Plan.Models;

namespace IDESK.Widgets.Plan.Service;

public class PlanLiteService : IPlanLiteService
{
    public static Action? DataChanged;

    private PlanLiteDbContext? _db;
    private DateTime _currentDate;

    public DateTime CurrentDate
    {
        get => _currentDate;
        set
        {
            _currentDate = value.Date;
            _db?.Dispose();
            _db = new PlanLiteDbContext { Date = _currentDate };
            _db.Database.EnsureCreated();
            _db.Migrate();
        }
    }

    private PlanLiteDbContext Db => _db ?? throw new InvalidOperationException("CurrentDate not set");

    // ── Widget 配置 ──

    public async Task<bool> GetCreatedAsync()
    {
        EnsureDb();
        var cfg = await _db!.PlanConfigs.FirstOrDefaultAsync();
        return cfg?.Created ?? false;
    }

    public async Task SetCreatedAsync()
    {
        EnsureDb();
        var cfg = await _db!.PlanConfigs.FirstOrDefaultAsync();
        if (cfg == null) _db.PlanConfigs.Add(new PlanConfig { Created = true });
        else cfg.Created = true;
        await _db.SaveChangesAsync();
    }

    public async Task<PlanConfig?> GetConfigAsync()
    {
        EnsureDb();
        return await _db!.PlanConfigs.FirstOrDefaultAsync();
    }

    public async Task SaveConfigAsync(PlanConfig config)
    {
        EnsureDb();
        var existing = await _db!.PlanConfigs.FirstOrDefaultAsync();
        if (existing == null) _db.PlanConfigs.Add(config);
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

    // ── PlanItem CRUD ──

    public async Task<List<PlanItem>> GetByDateAsync(DateTime date)
    {
        CurrentDate = date;
        return await Db.PlanItems
            .OrderBy(x => x.SortOrder).ThenBy(x => x.StartTime)
            .ToListAsync();
    }

    public async Task<PlanItem> AddAsync(PlanItem item)
    {
        CurrentDate = item.PlannedDate;
        Db.PlanItems.Add(item);
        await Db.SaveChangesAsync();
        DataChanged?.Invoke();
        return item;
    }

    public async Task UpdateAsync(PlanItem item)
    {
        CurrentDate = item.PlannedDate;
        Db.PlanItems.Update(item);
        await Db.SaveChangesAsync();
        DataChanged?.Invoke();
    }

    public async Task DeleteAsync(PlanItem item)
    {
        CurrentDate = item.PlannedDate;
        var existing = await Db.PlanItems.FindAsync(item.Id);
        if (existing != null)
        {
            Db.PlanItems.Remove(existing);
            await Db.SaveChangesAsync();
            DataChanged?.Invoke();
        }
    }

    private void EnsureDb()
    {
        if (_db == null) CurrentDate = DateTime.Today;
    }
}
