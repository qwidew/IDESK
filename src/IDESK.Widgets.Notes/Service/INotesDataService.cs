using IDESK.Widgets.Notes.Models;

namespace IDESK.Widgets.Notes.Service;

public interface INotesDataService
{
    int GroupId { get; set; }
    Task<string> GetTitleAsync();
    Task SaveTitleAsync(string title);
    Task<string> GetContentAsync();
    Task SaveContentAsync(string content);
}
