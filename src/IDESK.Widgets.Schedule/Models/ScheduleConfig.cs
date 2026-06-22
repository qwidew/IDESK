using System.ComponentModel.DataAnnotations;
using IDESK.Core;

namespace IDESK.Widgets.Schedule.Models;

public class ScheduleConfig : IWidgetPosition
{
    [Key]
    public int Id { get; set; }
    public bool ManagerCreated { get; set; }
    public double PositionX { get; set; }
    public double PositionY { get; set; }
    public double BookmarkPositionX { get; set; }
    public double Width { get; set; } = 800;
    public double Height { get; set; } = 600;
    public int BookmarkPresetId { get; set; }
    public bool IsBookmarkMode { get; set; }
    public bool TodayCreated { get; set; }
    public double TodayPositionX { get; set; }
    public double TodayPositionY { get; set; }
    public double TodayBookmarkPositionX { get; set; }
    public double TodayWidth { get; set; } = 350;
    public double TodayHeight { get; set; } = 500;
    public int TodayBookmarkPresetId { get; set; }
    public bool TodayIsBookmarkMode { get; set; }
}
