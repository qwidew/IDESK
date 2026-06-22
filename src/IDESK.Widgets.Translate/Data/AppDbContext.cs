using System.IO;
using Microsoft.EntityFrameworkCore;
using IDESK.Widgets.Translate.Models;

namespace IDESK.Widgets.Translate.Data;

public class AppDbContext : DbContext
{
    public DbSet<TranslateConfig> TranslateConfigs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "IDESK", "DataBase", "TRANSLATE_WIDGET", "data.db");
        if (!File.Exists(dbPath))
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath) ?? throw new InvalidOperationException());
        optionsBuilder.UseSqlite($"Data Source={dbPath};Pooling=False");
    }

    public override void Dispose()
    {
        Database.CloseConnection();
        base.Dispose();
    }
}
