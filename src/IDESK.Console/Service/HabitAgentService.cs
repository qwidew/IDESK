using IDESK.Widgets.Habit.Models;
using IDESK.Widgets.Habit.Service;

namespace IDESK.Console.Service;

public class HabitAgentService
{
    /// <summary>获取所有习惯及本周完成情况。</summary>
    public async Task<string> GetAllHabitsAsync()
    {
        var svc = new HabitService();
        var habits = await svc.GetAllHabitsAsync();
        if (habits.Count == 0) return "【查询结果】暂无习惯。";

        var weekStart = GetWeekStart();
        var weekEnd = weekStart.AddDays(6);
        var lines = new List<string>();

        foreach (var h in habits)
        {
            var dates = await svc.GetCompletedDatesAsync(h.Id, weekStart, weekEnd);
            var done = dates.Count;
            var pct = new string('■', done) + new string('□', 7 - done);
            lines.Add($"  - 习惯ID={h.Id}「{h.Title}」 本周 {done}/7 {pct}");
        }
        return $"【查询结果】共有 {habits.Count} 个习惯：\n" + string.Join("\n", lines);
    }

    /// <summary>添加新习惯。</summary>
    public async Task<string> AddHabitAsync(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return "【错误】名称不能为空";
        var svc = new HabitService();
        var habit = new HabitConfig { Title = title.Trim() };
        await svc.AddHabitAsync(habit);
        return $"【操作成功】已创建新习惯 ID={habit.Id}「{habit.Title}」";
    }

    /// <summary>删除习惯。</summary>
    public async Task<string> DeleteHabitAsync(int habitId)
    {
        try
        {
            var svc = new HabitService();
            var habits = await svc.GetAllHabitsAsync();
            var h = habits.FirstOrDefault(x => x.Id == habitId);
            await svc.DeleteHabitAsync(habitId);
            return $"【操作成功】已删除习惯「{h?.Title ?? $"ID={habitId}"}」";
        }
        catch (Exception ex) { return $"删除失败：{ex.Message}"; }
    }

    /// <summary>切换某天的完成状态。</summary>
    public async Task<string> ToggleDayAsync(int habitId, DateTime date)
    {
        try
        {
            var svc = new HabitService();
            var habits = await svc.GetAllHabitsAsync();
            var h = habits.FirstOrDefault(x => x.Id == habitId);
            var dates = await svc.GetCompletedDatesAsync(habitId, date.Date, date.Date);
            bool wasDone = dates.Contains(date.Date);
            await svc.ToggleCompleteAsync(habitId, date);
            return wasDone
                ? $"【操作成功】已取消习惯「{h?.Title ?? $"ID={habitId}"}」的 {date:MM/dd} 打卡"
                : $"【操作成功】已标记习惯「{h?.Title ?? $"ID={habitId}"}」在 {date:MM/dd} 完成";
        }
        catch (Exception ex) { return $"操作失败：{ex.Message}"; }
    }

    private static DateTime GetWeekStart() =>
        DateTime.Now.Date.AddDays(-(int)DateTime.Now.DayOfWeek + (int)DayOfWeek.Monday);
}
