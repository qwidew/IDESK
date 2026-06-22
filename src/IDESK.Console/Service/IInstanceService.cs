using IDESK.Console.Models;

namespace IDESK.Console.Service;

public interface IInstanceService
{
    Task<List<TodoInstance>> GetAllAsync();
    Task AddAsync(TodoInstance instance);
    Task UpdateAsync(TodoInstance instance);
    Task DeleteAsync(int id);
}
