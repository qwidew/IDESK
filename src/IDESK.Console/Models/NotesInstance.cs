using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using IDESK.Core;

namespace IDESK.Console.Models;

public class NotesInstance : INotifyPropertyChanged, IWidgetPosition
{
    [Key]
    public int Id { get; set; }

    private string _name = "";
    public string Name
    {
        get => _name;
        set { _name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name))); }
    }
    public double PositionX { get; set; }
    public double PositionY { get; set; }
    public double BookmarkPositionX { get; set; }
    public double Width { get; set; } = 400;
    public double Height { get; set; } = 500;
    public int BookmarkPresetId { get; set; }
    public bool IsBookmarkMode { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
}
