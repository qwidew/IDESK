using IDESK.Widgets.Plan.Models;
using IDESK.Widgets.Plan.Service;

namespace IDESK.Console.Service;

public class PlanAgentService
{
    public async Task<string> GetByDateAsync(DateTime date)
    {
        var svc = new PlanService();
        var items = await svc.GetByDateAsync(date);
        if (items.Count == 0) return $"【查询结果】{date:MM/dd} 暂无计划。";
        var lines = items.Select(i =>
            $"  - 计划ID={i.Id} {(string.IsNullOrEmpty(i.StartTime) ? "" : $"[{i.StartTime}-{i.EndTime}]")} " +
            $"{(i.IsDone ? "[已完成]" : "[未完成]")} {i.Content}");
        return $"【查询结果】{date:MM/dd} 共有 {items.Count} 项计划：\n" + string.Join("\n", lines);
    }

    public async Task<string> AddAsync(DateTime date, string content, string startTime, string endTime)
    {
        if (string.IsNullOrWhiteSpace(content)) return "【错误】内容不能为空";
        var svc = new PlanService();
        var item = new PlanItem
        {
            PlannedDate = date.Date,
            Content = content.Trim(),
            StartTime = startTime?.Trim() ?? "",
            EndTime = endTime?.Trim() ?? "",
        };
        await svc.AddAsync(item);
        return $"【操作成功】已添加计划：{date:MM/dd} {item.StartTime}-{item.EndTime}「{content}」";
    }

    public async Task<string> ToggleAsync(int itemId, DateTime date)
    {
        try
        {
            var svc = new PlanService();
            var items = await svc.GetByDateAsync(date);
            var item = items.FirstOrDefault(i => i.Id == itemId);
            if (item == null) return $"【错误】未找到计划 ID={itemId}";
            item.IsDone = !item.IsDone;
            await svc.UpdateAsync(item);
            return $"【操作成功】已{(item.IsDone ? "完成" : "取消完成")}计划「{item.Content}」";
        }
        catch (Exception ex) { return $"操作失败：{ex.Message}"; }
    }

    public async Task<string> DeleteAsync(int itemId, DateTime date)
    {
        try
        {
            var svc = new PlanService();
            var item = (await svc.GetByDateAsync(date)).FirstOrDefault(i => i.Id == itemId);
            if (item == null) return $"【错误】未找到计划 ID={itemId}";
            await svc.DeleteAsync(item);
            return $"【操作成功】已删除计划「{item.Content}」";
        }
        catch (Exception ex) { return $"删除失败：{ex.Message}"; }
    }
}
