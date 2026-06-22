using System.IO;
using Microsoft.EntityFrameworkCore;
using IDESK.Widgets.Notes.Models;

namespace IDESK.Widgets.Notes.Data;

public class AppDbContext : DbContext
{
    public int GroupId { get; set; }

    public DbSet<NoteItem> Notes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "IDESK", "DataBase", "NOTES_WIDGET", $"Group_{GroupId}", "data.db");
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
