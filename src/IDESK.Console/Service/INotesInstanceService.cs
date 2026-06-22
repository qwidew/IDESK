using IDESK.Console.Models;

namespace IDESK.Console.Service;

public interface INotesInstanceService
{
    Task<List<NotesInstance>> GetAllAsync();
    Task AddAsync(NotesInstance instance);
    Task UpdateAsync(NotesInstance instance);
    Task DeleteAsync(int id);
}
