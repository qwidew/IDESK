using Microsoft.EntityFrameworkCore;
using IDESK.Widgets.Todo.Data;
using IDESK.Widgets.Todo.Models;

namespace IDESK.Widgets.Todo.Service;

public class TodoDataService : ITodoDataService
{
    /// <summary>外部添加待办后触发。item 是已落库的完整对象，包含正确 Id。</summary>
    public static Action<int, TodoItem>? ItemAdded;
    /// <summary>外部删除待办。</summary>
    public static Action<int, int>? ItemDeleted;
    /// <summary>外部切换完成状态。</summary>
    public static Action<int, int, bool, DateTime?>? ItemToggled;
    /// <summary>外部修改截止日期。</summary>
    public static Action<int, int, DateTime?>? ItemDdlChanged;

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

    public async Task<List<TodoItem>> GetItemsAsync()
    {
        return await Db.Todos.OrderBy(x => x.SortOrder).ToListAsync();
    }

    public async Task AddItemAsync(TodoItem item)
    {
        Db.Todos.Add(item);
        await Db.SaveChangesAsync();
    }

    public async Task UpdateItemAsync(TodoItem item)
    {
        Db.Todos.Update(item);
        await Db.SaveChangesAsync();
    }

    public async Task RemoveItemAsync(int id)
    {
        var item = await Db.Todos.FindAsync(id);
        if (item != null)
        {
            Db.Todos.Remove(item);
            await Db.SaveChangesAsync();
        }
    }

    public async Task UpdateOrderAsync(List<TodoItem> items)
    {
        Db.Todos.UpdateRange(items);
        await Db.SaveChangesAsync();
    }

    public async Task<string> GetGroupNameAsync()
    {
        var config = await Db.GroupConfigs.FirstOrDefaultAsync();
        return config?.GroupName ?? "TODOLIST";
    }

    public async Task SaveGroupNameAsync(string name)
    {
        var config = await Db.GroupConfigs.FirstOrDefaultAsync();
        if (config == null)
        {
            Db.GroupConfigs.Add(new GroupConfig { GroupName = name });
        }
        else
        {
            config.GroupName = name;
        }
        await Db.SaveChangesAsync();
    }

    public void Dispose()
    {
        _db?.Dispose();
    }
}
