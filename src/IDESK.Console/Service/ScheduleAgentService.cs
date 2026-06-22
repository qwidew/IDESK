using IDESK.Widgets.Schedule.Models;
using IDESK.Widgets.Schedule.Service;

namespace IDESK.Console.Service;

public class ScheduleAgentService
{
    /// <summary>获取指定日期的所有日程。</summary>
    public async Task<string> GetByDateAsync(DateTime date)
    {
        var svc = new ScheduleService();
        var items = await svc.GetByDateAsync(date);
        if (items.Count == 0)
            return $"【查询结果】{date:MM/dd} 暂无日程。";
        var lines = items.Select(i =>
            $"  - 日程ID={i.Id} {(string.IsNullOrEmpty(i.Time) ? "" : $"[{i.Time}]")} {i.Content}");
        return $"【查询结果】{date:MM/dd} 共有 {items.Count} 项日程：\n" + string.Join("\n", lines);
    }

    /// <summary>获取指定日期范围的所有日程。</summary>
    public async Task<string> GetByRangeAsync(DateTime start, DateTime end)
    {
        var svc = new ScheduleService();
        var items = await svc.GetByRangeAsync(start, end);
        if (items.Count == 0)
            return $"【查询结果】{start:MM/dd} ~ {end:MM/dd} 暂无日程。";
        var lines = items.Select(i =>
            $"  - 日程ID={i.Id} {i.Date:MM/dd} {(string.IsNullOrEmpty(i.Time) ? "" : $"[{i.Time}]")} {i.Content}");
        return $"【查询结果】{start:MM/dd} ~ {end:MM/dd} 共有 {items.Count} 项日程：\n" + string.Join("\n", lines);
    }

    /// <summary>添加日程。</summary>
    public async Task<string> AddAsync(DateTime date, string content, string? time)
    {
        if (string.IsNullOrWhiteSpace(content)) return "【错误】内容不能为空";
        var svc = new ScheduleService();
        var item = new ScheduleItem { Date = date.Date, Content = content.Trim(), Time = time?.Trim() ?? "" };
        await svc.AddAsync(item);
        var timeStr = string.IsNullOrEmpty(item.Time) ? "" : $" {item.Time}";
        return $"【操作成功】已添加日程：{date:MM/dd}{timeStr}「{content}」";
    }

    /// <summary>删除日程。</summary>
    public async Task<string> DeleteAsync(int id)
    {
        try
        {
            var svc = new ScheduleService();
            await svc.DeleteAsync(id);
            return $"【操作成功】已删除日程 ID={id}";
        }
        catch (Exception ex) { return $"删除失败：{ex.Message}"; }
    }
}
