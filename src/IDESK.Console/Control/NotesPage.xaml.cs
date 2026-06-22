using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using IDESK.Console.Models;
using IDESK.Console.Service;
using IDESK.Core.Helper;

namespace IDESK.Console.Control;

public partial class NotesPage : UserControl
{
    private readonly INotesInstanceService _instanceService;
    private readonly ObservableCollection<NotesInstance> _instances = [];

    public ICommand DeleteCommand { get; }
    public ICommand RenameCommand { get; }

    public event Action<NotesInstance>? InstanceCreated;
    public event Action<NotesInstance>? InstanceDeleteRequested;
    public event Action<NotesInstance>? InstanceRenamed;

    public NotesPage(INotesInstanceService instanceService)
    {
        _instanceService = instanceService;
        DeleteCommand = new RelayCommand(param =>
        {
            if (param is NotesInstance instance)
                InstanceDeleteRequested?.Invoke(instance);
        });
        RenameCommand = new RelayCommand(async param =>
        {
            if (param is NotesInstance instance)
            {
                await _instanceService.UpdateAsync(instance);
                InstanceRenamed?.Invoke(instance);
            }
        });
        DataContext = this;
        InitializeComponent();
        InstanceList.ItemsSource = _instances;
        LoadInstances();
    }

    private void WatchInstance(NotesInstance instance)
    {
        instance.PropertyChanged += async (_, e) =>
        {
            if (e.PropertyName == nameof(NotesInstance.Name))
            {
                await _instanceService.UpdateAsync(instance);
                InstanceRenamed?.Invoke(instance);
            }
        };
    }

    private async void LoadInstances()
    {
        var list = await _instanceService.GetAllAsync();
        _instances.Clear();
        foreach (var item in list)
        {
            WatchInstance(item);
            _instances.Add(item);
        }
    }

    public void RemoveInstance(NotesInstance instance)
    {
        _instances.Remove(instance);
    }

    public void UpdateInstanceName(int id, string name)
    {
        var item = _instances.FirstOrDefault(x => x.Id == id);
        if (item != null)
            item.Name = name;
    }

    private void OnCreateClick(object sender, RoutedEventArgs e)
    {
        InstanceNameBox.Clear();
        Overlay.Visibility = Visibility.Visible;
        InstanceNameBox.Focus();
    }

    private async void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        string name = InstanceNameBox.Text.Trim();
        if (!string.IsNullOrEmpty(name))
        {
            var instance = new NotesInstance { Name = name };
            await _instanceService.AddAsync(instance);
            WatchInstance(instance);
            _instances.Add(instance);
            InstanceCreated?.Invoke(instance);
        }
        Overlay.Visibility = Visibility.Collapsed;
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Overlay.Visibility = Visibility.Collapsed;
    }
}
