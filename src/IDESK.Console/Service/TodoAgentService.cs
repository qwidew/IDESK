using System.IO;
using IDESK.Console.Models;
using IDESK.Widgets.Todo.Models;
using IDESK.Widgets.Todo.Service;

namespace IDESK.Console.Service;

/// <summary>
/// Todo 组件代理接口，供测试页手动调用或后续 AI 调用。
/// </summary>
public class TodoAgentService
{
    public static event Action<TodoInstance>? GroupCreated;
    public static event Action<int>? GroupDeleted;
    /// <summary>获取所有 Todo 分组。</summary>
    public async Task<List<TodoGroupInfo>> GetAllGroupsAsync()
    {
        var svc = new InstanceService();
        var list = await svc.GetAllAsync();
        return list.Select(x => new TodoGroupInfo { Id = x.Id, Name = x.Name }).ToList();
    }

    /// <summary>获取指定组的所有待办。分组不存在时返回空列表。</summary>
    public async Task<List<TodoItemBrief>> GetTodosAsync(int groupId)
    {
        var groups = await GetAllGroupsAsync();
        if (!groups.Any(g => g.Id == groupId)) return [];

        var service = new TodoDataService { GroupId = groupId };
        var items = await service.GetItemsAsync();
        service.Dispose();
        return items.Select(i => new TodoItemBrief
        {
            Id = i.Id,
            Content = i.Content,
            IsDone = i.IsDone,
            Ddl = i.Ddl,
            CompleteDate = i.CompleteDate,
        }).ToList();
    }

    /// <summary>添加分组。返回 (是否成功, 新分组ID, 分组名)。</summary>
    public async Task<(bool Ok, int Id, string Name)> AddGroupAsync(string name)
    {
        try
        {
            var instance = new TodoInstance { Name = name };
            var svc = new InstanceService();
            await svc.AddAsync(instance);
            GroupCreated?.Invoke(instance);
            return (true, instance.Id, instance.Name);
        }
        catch (Exception ex)
        {
            return (false, 0, ex.Message);
        }
    }

    /// <summary>添加待办。</summary>
    public async Task<string> AddTodoAsync(int groupId, string content, DateTime? ddl)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(content))
                return "内容不能为空";

            var groups = await GetAllGroupsAsync();
            if (!groups.Any(g => g.Id == groupId))
                return $"【错误】分组 {groupId} 不存在";

            var service = new TodoDataService { GroupId = groupId };
            var item = new TodoItem(content.Trim());
            if (ddl.HasValue) item.Ddl = ddl.Value;
            await service.AddItemAsync(item);
            TodoDataService.ItemAdded?.Invoke(groupId, item);
            service.Dispose();
            return $"【操作成功】已在分组 {groupId} 中添加待办「{content}」" + (ddl.HasValue ? $"（截止日期：{ddl:yyyy-MM-dd}）" : "");
        }
        catch (Exception ex)
        {
            return $"添加失败：{ex.Message}";
        }
    }

    public async Task<string> DeleteTodoAsync(int groupId, int itemId)
    {
        try
        {
            var groups = await GetAllGroupsAsync();
            if (!groups.Any(g => g.Id == groupId)) return $"【错误】分组 {groupId} 不存在";

            TodoDataService.ItemDeleted?.Invoke(groupId, itemId);
            var service = new TodoDataService { GroupId = groupId };
            await service.RemoveItemAsync(itemId);
            service.Dispose();
            return $"【操作成功】已删除待办 {itemId}";
        }
        catch (Exception ex) { return $"删除失败：{ex.Message}"; }
    }

    public async Task<string> ToggleTodoAsync(int groupId, int itemId)
    {
        try
        {
            var groups = await GetAllGroupsAsync();
            if (!groups.Any(g => g.Id == groupId)) return $"【错误】分组 {groupId} 不存在";

            var items = await GetTodosAsync(groupId);
            var brief = items.FirstOrDefault(i => i.Id == itemId);
            if (brief == null) return $"【错误】待办 {itemId} 不存在";

            bool newDone = !brief.IsDone;
            DateTime? newDate = newDone ? DateTime.Now : null;

            TodoDataService.ItemToggled?.Invoke(groupId, itemId, newDone, newDate);
            var service = new TodoDataService { GroupId = groupId };
            var all = await service.GetItemsAsync();
            var item = all.FirstOrDefault(i => i.Id == itemId);
            if (item != null) { item.IsDone = newDone; item.CompleteDate = newDate; await service.UpdateItemAsync(item); }
            service.Dispose();
            return $"【操作成功】待办 {itemId} 已{(newDone ? "完成" : "取消完成")}";
        }
        catch (Exception ex) { return $"操作失败：{ex.Message}"; }
    }

    public async Task<string> SetDdlAsync(int groupId, int itemId, DateTime? ddl)
    {
        try
        {
            var groups = await GetAllGroupsAsync();
            if (!groups.Any(g => g.Id == groupId)) return $"【错误】分组 {groupId} 不存在";

            TodoDataService.ItemDdlChanged?.Invoke(groupId, itemId, ddl);
            var service = new TodoDataService { GroupId = groupId };
            var all = await service.GetItemsAsync();
            var item = all.FirstOrDefault(i => i.Id == itemId);
            if (item != null) { item.Ddl = ddl; await service.UpdateItemAsync(item); }
            service.Dispose();
            return $"【操作成功】待办 {itemId} 截止日期已设置为 {(ddl?.ToString("yyyy-MM-dd") ?? "无")}";
        }
        catch (Exception ex) { return $"操作失败：{ex.Message}"; }
    }

    public async Task<string> DeleteGroupAsync(int groupId)
    {
        try
        {
            var groups = await GetAllGroupsAsync();
            if (!groups.Any(g => g.Id == groupId)) return $"【错误】分组 {groupId} 不存在";

            GroupDeleted?.Invoke(groupId);
            var svc = new InstanceService();
            await svc.DeleteAsync(groupId);
            DeleteDataDir(groupId);
            return $"【操作成功】已删除分组 {groupId}";
        }
        catch (Exception ex) { return $"删除失败：{ex.Message}"; }
    }

    private static void DeleteDataDir(int groupId)
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "IDESK", "DataBase", "TODO_WIDGET", $"Group_{groupId}");
        if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
    }
}

public class TodoGroupInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class TodoItemBrief
{
    public int Id { get; set; }
    public string Content { get; set; } = "";
    public bool IsDone { get; set; }
    public DateTime? Ddl { get; set; }
    public DateTime? CompleteDate { get; set; }
}
