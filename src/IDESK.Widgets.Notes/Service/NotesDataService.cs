using Microsoft.EntityFrameworkCore;
using IDESK.Widgets.Notes.Data;
using IDESK.Widgets.Notes.Models;

namespace IDESK.Widgets.Notes.Service;

public class NotesDataService : INotesDataService
{
    private AppDbContext? _db;
    private int _groupId;

    public int GroupId
    {
        get => _groupId;
        set
        {
            if (_groupId == value) return;
            _groupId = value;
            _db?.Dispose();
            _db = null;
        }
    }

    private AppDbContext Db
    {
        get
        {
            if (_db == null)
            {
                _db = new AppDbContext { GroupId = _groupId };
                _db.Database.EnsureCreated();
            }
            return _db;
        }
    }

    public async Task<string> GetTitleAsync()
    {
        var note = await Db.Notes.FirstOrDefaultAsync();
        return note?.Title ?? "";
    }

    public async Task SaveTitleAsync(string title)
    {
        var note = await Db.Notes.FirstOrDefaultAsync();
        if (note == null)
            Db.Notes.Add(new NoteItem { Title = title });
        else
            note.Title = title;
        await Db.SaveChangesAsync();
    }

    public async Task<string> GetContentAsync()
    {
        var note = await Db.Notes.FirstOrDefaultAsync();
        return note?.Content ?? "";
    }

    public async Task SaveContentAsync(string content)
    {
        var note = await Db.Notes.FirstOrDefaultAsync();
        if (note == null)
        {
            Db.Notes.Add(new NoteItem { Content = content });
        }
        else
        {
            note.Content = content;
            note.UpdatedDate = DateTime.Now;
        }
        await Db.SaveChangesAsync();
    }
}
