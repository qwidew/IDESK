using System.IO;
using Microsoft.EntityFrameworkCore;
using IDESK.Widgets.Schedule.Models;

namespace IDESK.Widgets.Schedule.Data;

public class AppDbContext : DbContext
{
    public DbSet<ScheduleItem> ScheduleItems { get; set; }
    public DbSet<ScheduleConfig> ScheduleConfigs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "IDESK", "DataBase", "SCHEDULE_WIDGET", "data.db");
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
