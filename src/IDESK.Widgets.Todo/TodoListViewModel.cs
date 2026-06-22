using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using IDESK.Core.Helper;
using IDESK.Core.Logging;
using IDESK.Widgets.Todo.Models;
using IDESK.Widgets.Todo.Service;

namespace IDESK.Widgets.Todo;

public class TodoListViewModel : INotifyPropertyChanged
{
    private readonly ITodoDataService _dataService;
    private readonly ILogger _logger;
    private readonly IDialogService _dialogService;
    public ObservableCollection<TodoItem> TodoList { get; } = [];

    private string _groupName = "TODOLIST";
    public string GroupName
    {
        get => _groupName;
        set
        {
            if (_groupName == value) return;
            _groupName = value;
            OnPropertyChanged(nameof(GroupName));
            _ = _dataService.SaveGroupNameAsync(value);
        }
    }
    public int GroupID { get; set; }

    private string _newContent= string.Empty;
    public string NewContent {
        get => _newContent;
        set{ _newContent = value; OnPropertyChanged(nameof(NewContent)); }
    }

    public TodoListViewModel(ITodoDataService dataService, ILogger logger, IDialogService dialogService)
    {
        _dataService = dataService;
        _logger = logger;
        _dialogService = dialogService;

        TodoDataService.ItemAdded += (gid, item) =>
        {
            if (gid != GroupID) return;
            WatchItem(item);
            TodoList.Add(item);
        };
        TodoDataService.ItemDeleted += (gid, itemId) =>
        {
            if (gid != GroupID) return;
            var item = TodoList.FirstOrDefault(i => i.Id == itemId);
            if (item != null) TodoList.Remove(item);
        };
        TodoDataService.ItemToggled += (gid, itemId, done, date) =>
        {
            if (gid != GroupID) return;
            var item = TodoList.FirstOrDefault(i => i.Id == itemId);
            if (item != null) { item.IsDone = done; item.CompleteDate = date; }
        };
        TodoDataService.ItemDdlChanged += (gid, itemId, ddl) =>
        {
            if (gid != GroupID) return;
            var item = TodoList.FirstOrDefault(i => i.Id == itemId);
            if (item != null) item.Ddl = ddl;
        };

        LoadCommand = new RelayCommand(async _ => await LoadAsync());

        AddCommand = new RelayCommand(async _ =>
        {
            var todoItem = new TodoItem(NewContent);
            WatchItem(todoItem);
            TodoList.Add(todoItem);
            await _dataService.AddItemAsync(todoItem);
        });
        ClearCommand = new RelayCommand(async _ =>
        {
            var doneList = TodoList.Where(x => x.IsDone).ToList();
            foreach (var item in doneList)
            {
                TodoList.Remove(item);
                await _dataService.RemoveItemAsync(item.Id);
            }
        });
        CompleteItemCommand = new RelayCommand(async param =>
        {
            if (param is TodoItem item)
            {
                item.IsDone = !item.IsDone;
                item.CompleteDate = item.IsDone ? DateTime.Now : null;
                await _dataService.UpdateItemAsync(item);
                FilteredView?.Refresh();
            }
        });
        DeleteItemCommand = new RelayCommand(async param =>
        {
            if (param is TodoItem item)
            {
                TodoList.Remove(item);
                await _dataService.RemoveItemAsync(item.Id);
            }
        });

        ToggleFilterCommand = new RelayCommand(_ => ShowCompleted = !ShowCompleted);
        SortCommand = new RelayCommand(async _ =>
        {
            var sorted = TodoList
                .OrderBy(i => i.IsDone)
                .ThenBy(i => i.Ddl ?? DateTime.MaxValue)
                .ToList();
            TodoList.Clear();
            for (int j = 0; j < sorted.Count; j++)
            {
                sorted[j].SortOrder = j;
                TodoList.Add(sorted[j]);
            }
            await _dataService.UpdateOrderAsync(sorted);
        });

        FilteredView = CollectionViewSource.GetDefaultView(TodoList);
        FilteredView.Filter = o => ShowCompleted || !((TodoItem)o).IsDone;

        LoadAsync();
        _ = LoadGroupNameAsync();
        FilteredView.Refresh();
    }

    public void LoadInstance(int groupId, string groupName)
    {
        GroupID = groupId;
        _groupName = groupName;
        _dataService.GroupId = groupId;
        OnPropertyChanged(nameof(GroupName));
        _dataService.SaveGroupNameAsync(groupName).GetAwaiter().GetResult();
        _ = LoadGroupNameAsync();
        _ = LoadAsync();
    }

    public ICommand LoadCommand { get; }
    public ICommand AddCommand { get; }
    public ICommand ClearCommand { get; }
    public ICommand CompleteItemCommand { get; }
    public ICommand DeleteItemCommand { get; }
    public ICommand ToggleFilterCommand { get; }
    public ICommand SortCommand { get; }
    public ICollectionView FilteredView { get; }

    private bool _showCompleted = true;
    public bool ShowCompleted
    {
        get => _showCompleted;
        set
        {
            _showCompleted = value;
            OnPropertyChanged(nameof(ShowCompleted));
            FilteredView.Refresh();
        }
    }

    private async Task LoadGroupNameAsync()
    {
        var name = await _dataService.GetGroupNameAsync();
        if (!string.IsNullOrEmpty(name))
            _groupName = name;
        OnPropertyChanged(nameof(GroupName));
    }

    private async Task LoadAsync()
    {
        var items = await _dataService.GetItemsAsync();
        TodoList.Clear();
        foreach (var item in items)
        {
            WatchItem(item);
            TodoList.Add(item);
        }
    }

    public async Task SaveOrderAsync()
    {
        await _dataService.UpdateOrderAsync(TodoList.ToList());
    }

    private void WatchItem(TodoItem item)
    {
        item.PropertyChanged += async (_, e) =>
        {
            if (e.PropertyName is nameof(TodoItem.Ddl) or nameof(TodoItem.Content))
            {
                await _dataService.UpdateItemAsync(item);
            }
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
