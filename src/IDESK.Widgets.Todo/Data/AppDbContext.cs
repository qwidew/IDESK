using System.IO;
using Microsoft.EntityFrameworkCore;
using IDESK.Widgets.Todo.Models;

namespace IDESK.Widgets.Todo.Data;

public class AppDbContext : DbContext
{
    public int GroupId { get; set; }

    public DbSet<TodoItem> Todos { get; set; }
    public DbSet<GroupConfig> GroupConfigs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "IDESK", "DataBase", "TODO_WIDGET", $"Group_{GroupId}", "data.db");
        if (!File.Exists(dbPath)) Directory.CreateDirectory(Path.GetDirectoryName(dbPath) ?? throw new InvalidOperationException());
        optionsBuilder.UseSqlite($"Data Source={dbPath};Pooling=False");
    }

    public override void Dispose()
    {
        Database.CloseConnection();
        base.Dispose();
    }
}