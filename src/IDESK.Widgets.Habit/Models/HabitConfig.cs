using System.ComponentModel.DataAnnotations;
using IDESK.Core.Models;

namespace IDESK.Widgets.Habit.Models;

public class HabitConfig
{
    [Key]
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public int Frequency { get; set; } = 1;
    public Importance Importance { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Today;
}
