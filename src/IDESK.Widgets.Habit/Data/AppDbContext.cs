using System.IO;
using Microsoft.EntityFrameworkCore;
using IDESK.Widgets.Habit.Models;

namespace IDESK.Widgets.Habit.Data;

public class AppDbContext : DbContext
{
    public DbSet<HabitWidgetConfig> HabitWidgetConfigs { get; set; }
    public DbSet<HabitConfig> HabitConfigs { get; set; }
    public DbSet<HabitRecord> HabitRecords { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "IDESK", "DataBase", "HABIT_WIDGET", "data.db");
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
