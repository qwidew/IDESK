using System.IO;
using Microsoft.EntityFrameworkCore;
using IDESK.Widgets.Plan.Models;

namespace IDESK.Widgets.Plan.Data;

public class AppDbContext : DbContext
{
    public DateTime Date { get; set; }

    public DbSet<PlanConfig> PlanConfigs { get; set; }
    public DbSet<PlanItem> PlanItems { get; set; }

    public void Migrate()
    {
        try { Database.ExecuteSqlRaw("ALTER TABLE PlanItems ADD COLUMN StartTime TEXT"); } catch { }
        try { Database.ExecuteSqlRaw("ALTER TABLE PlanItems ADD COLUMN EndTime TEXT"); } catch { }
        try { Database.ExecuteSqlRaw("ALTER TABLE PlanItems ADD COLUMN IsDone INTEGER NOT NULL DEFAULT 0"); } catch { }
        try { Database.ExecuteSqlRaw("ALTER TABLE PlanItems ADD COLUMN SortOrder INTEGER NOT NULL DEFAULT 0"); } catch { }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "IDESK", "DataBase", "PLAN_WIDGET", Date.ToString("yyyy-MM-dd"), "data.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        optionsBuilder.UseSqlite($"Data Source={dbPath};Pooling=False");
    }

    public override void Dispose()
    {
        Database.CloseConnection();
        base.Dispose();
    }
}
