using System.ComponentModel;

namespace IDESK.Widgets.Todo.Models;

public class TodoItem : INotifyPropertyChanged
{
    public TodoItem(string content)
    {
        Content = content;
    }

    public int Id { get; set; }
    public int SortOrder { get; set; }

    private string _content = string.Empty;
    public string Content {
        get => _content;
        set { _content = value; OnPropertyChanged(nameof(Content)); }
    }

    private bool _isDone = false;
    public bool IsDone {
        get => _isDone;
        set{ _isDone = value; OnPropertyChanged(nameof(IsDone)); OnPropertyChanged(nameof(UrgencyBrushKey)); }
    }

    public DateTime CreateDate { get; set; } = DateTime.Now;

    private DateTime? _ddl;
    public DateTime? Ddl {
        get => _ddl;
        set { _ddl = value; OnPropertyChanged(nameof(Ddl)); OnPropertyChanged(nameof(UrgencyBrushKey)); }
    }

    private DateTime? _completeDate;
    public DateTime? CompleteDate {
        get => _completeDate;
        set { _completeDate = value; OnPropertyChanged(nameof(CompleteDate)); }
    }

    /// <summary>根据 DDL 和完成状态返回主题画笔资源名称。</summary>
    public string UrgencyBrushKey
    {
        get
        {
            if (IsDone) return "Green6Brush";
            if (Ddl == null) return "SeparatorBrush";
            var days = (Ddl.Value.Date - DateTime.Now.Date).Days;
            if (days < 0) return "DangerBrush";
            if (days == 0) return "DangerBrush";
            if (days <= 3) return "Orange5Brush";
            if (days <= 5) return "Amber5Brush";
            if (days <= 7) return "Blue5Brush";
            return "Green6Brush";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
