using System.ComponentModel.DataAnnotations;

namespace IDESK.Widgets.Habit.Models;

public class HabitRecord
{
    [Key]
    public int Id { get; set; }
    public int HabitId { get; set; }
    public DateTime CompletedDate { get; set; }
}
