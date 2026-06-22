using System.IO;
using Microsoft.EntityFrameworkCore;
using IDESK.Console.Models;

namespace IDESK.Console.Data;

public class AppDbContext : DbContext
{
    public DbSet<TodoInstance> TodoInstances { get; set; }
    public DbSet<NotesInstance> NotesInstances { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "IDESK", "DataBase", "instances.db");
        if (!File.Exists(dbPath))
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath) ?? throw new InvalidOperationException());
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }
}
