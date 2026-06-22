using Microsoft.EntityFrameworkCore;
using IDESK.Console.Data;
using IDESK.Console.Models;

namespace IDESK.Console.Service;

public class NotesInstanceService : INotesInstanceService
{
    private readonly AppDbContext _db;

    public NotesInstanceService()
    {
        _db = new AppDbContext();
        _db.Database.EnsureCreated();
    }

    public async Task<List<NotesInstance>> GetAllAsync()
    {
        return await _db.NotesInstances.OrderBy(x => x.Id).ToListAsync();
    }

    public async Task AddAsync(NotesInstance instance)
    {
        _db.NotesInstances.Add(instance);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(NotesInstance instance)
    {
        _db.NotesInstances.Update(instance);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var item = await _db.NotesInstances.FindAsync(id);
        if (item != null)
        {
            _db.NotesInstances.Remove(item);
            await _db.SaveChangesAsync();
        }
    }
}
