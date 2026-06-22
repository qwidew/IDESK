using System.ComponentModel;
using IDESK.Widgets.Notes.Service;

namespace IDESK.Widgets.Notes;

public class NotesViewModel : INotifyPropertyChanged
{
    private readonly INotesDataService _dataService;
    private string _title = "笔记";
    private string _content = "";

    public string Title
    {
        get => _title;
        set
        {
            if (_title == value) return;
            _title = value;
            OnPropertyChanged(nameof(Title));
            _ = _dataService.SaveTitleAsync(value);
        }
    }

    public string Content
    {
        get => _content;
        set
        {
            if (_content == value) return;
            _content = value;
            OnPropertyChanged(nameof(Content));
        }
    }

    public NotesViewModel(INotesDataService dataService)
    {
        _dataService = dataService;
    }

    public void LoadInstance(int groupId, string defaultTitle = "笔记")
    {
        _dataService.GroupId = groupId;
        _ = LoadTitleAsync(defaultTitle);
        _ = LoadContentAsync();
    }

    private async Task LoadTitleAsync(string defaultTitle)
    {
        var title = await _dataService.GetTitleAsync();
        if (!string.IsNullOrEmpty(title))
        {
            _title = title;
        }
        else
        {
            _title = defaultTitle;
            await _dataService.SaveTitleAsync(defaultTitle);
        }
        OnPropertyChanged(nameof(Title));
    }

    private async Task LoadContentAsync()
    {
        Content = await _dataService.GetContentAsync();
    }

    public async Task SaveContentAsync() => await _dataService.SaveContentAsync(_content);

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
