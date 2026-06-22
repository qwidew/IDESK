using IDESK.Widgets.Todo.Models;

namespace IDESK.Widgets.Todo.Service;

public interface ITodoDataService
{
    int GroupId { get; set; }
    Task<List<TodoItem>> GetItemsAsync();
    Task AddItemAsync(TodoItem item);
    Task UpdateItemAsync(TodoItem item);
    Task UpdateOrderAsync(List<TodoItem> items);
    Task RemoveItemAsync(int id);
    Task<string> GetGroupNameAsync();
    Task SaveGroupNameAsync(string name);
}