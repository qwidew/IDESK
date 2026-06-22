using System.ComponentModel.DataAnnotations;

namespace IDESK.Widgets.Schedule.Models;

public class ScheduleItem
{
    [Key]
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Content { get; set; } = "";
    public string Time { get; set; } = "";
}
