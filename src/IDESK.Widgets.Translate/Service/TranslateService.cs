using Microsoft.EntityFrameworkCore;
using IDESK.Widgets.Translate.Data;
using IDESK.Widgets.Translate.Models;

namespace IDESK.Widgets.Translate.Service;

public class TranslateService : ITranslateService
{
    private readonly AppDbContext _db;

    public TranslateService()
    {
        _db = new AppDbContext();
        _db.Database.EnsureCreated();
    }

    public async Task<bool> GetCreatedAsync()
    {
        var cfg = await _db.TranslateConfigs.FirstOrDefaultAsync();
        return cfg?.Created ?? false;
    }

    public async Task SetCreatedAsync()
    {
        var cfg = await _db.TranslateConfigs.FirstOrDefaultAsync();
        if (cfg == null)
            _db.TranslateConfigs.Add(new TranslateConfig { Created = true });
        else
            cfg.Created = true;
        await _db.SaveChangesAsync();
    }

    public async Task<TranslateConfig?> GetConfigAsync()
    {
        return await _db.TranslateConfigs.FirstOrDefaultAsync();
    }

    public async Task SaveConfigAsync(TranslateConfig config)
    {
        var existing = await _db.TranslateConfigs.FirstOrDefaultAsync();
        if (existing == null)
        {
            _db.TranslateConfigs.Add(config);
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
}
