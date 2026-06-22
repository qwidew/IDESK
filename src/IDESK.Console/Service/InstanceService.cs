using Microsoft.EntityFrameworkCore;
using IDESK.Console.Data;
using IDESK.Console.Models;

namespace IDESK.Console.Service;

public class InstanceService : IInstanceService
{
    private readonly AppDbContext _db;

    public InstanceService()
    {
        _db = new AppDbContext();
        _db.Database.EnsureCreated();
    }

    public async Task<List<TodoInstance>> GetAllAsync()
    {
        return await _db.TodoInstances.OrderBy(x => x.Id).ToListAsync();
    }

    public async Task AddAsync(TodoInstance instance)
    {
        _db.TodoInstances.Add(instance);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(TodoInstance instance)
    {
        _db.TodoInstances.Update(instance);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var item = await _db.TodoInstances.FindAsync(id);
        if (item != null)
        {
            _db.TodoInstances.Remove(item);
            await _db.SaveChangesAsync();
        }
    }
}
