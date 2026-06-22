using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace IDESK.Widgets.Plan.Models;

public class PlanItem : INotifyPropertyChanged
{
    [Key] public int Id { get; set; }
    public DateTime PlannedDate { get; set; }

    private string _content = "";
    public string Content
    {
        get => _content;
        set { _content = value; OnPropertyChanged(nameof(Content)); }
    }

    public string StartTime { get; set; } = "";
    public string EndTime { get; set; } = "";

    private bool _isDone;
    public bool IsDone
    {
        get => _isDone;
        set { _isDone = value; OnPropertyChanged(nameof(IsDone)); }
    }

    public int SortOrder { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string p) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
}
