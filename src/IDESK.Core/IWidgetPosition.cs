namespace IDESK.Core;

public interface IWidgetPosition
{
    double PositionX { get; set; }
    double PositionY { get; set; }
    double BookmarkPositionX { get; set; }
    double Width { get; set; }
    double Height { get; set; }
    int BookmarkPresetId { get; set; }
    bool IsBookmarkMode { get; set; }
}
