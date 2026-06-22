using System.ComponentModel.DataAnnotations;
using IDESK.Core;

namespace IDESK.Widgets.Plan.Models;

public class PlanConfig : IWidgetPosition
{
    [Key]
    public int Id { get; set; }
    public bool Created { get; set; }
    public double PositionX { get; set; }
    public double PositionY { get; set; }
    public double BookmarkPositionX { get; set; }
    public double Width { get; set; } = 800;
    public double Height { get; set; } = 500;
    public int BookmarkPresetId { get; set; }
    public bool IsBookmarkMode { get; set; }
}
