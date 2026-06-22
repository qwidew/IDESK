using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using IDESK.Console.Models;
using IDESK.Console.Service;
using IDESK.Core.Helper;

namespace IDESK.Console.Control;

public partial class TodoPage : UserControl
{
    private readonly IInstanceService _instanceService;
    private readonly ObservableCollection<TodoInstance> _instances = [];

    public ICommand DeleteCommand { get; }
    public event Action<TodoInstance>? InstanceCreated;
    public event Action<TodoInstance>? InstanceDeleteRequested;
    public event Action<TodoInstance>? InstanceRenamed;

    public TodoPage(IInstanceService instanceService)
    {
        _instanceService = instanceService;
        DeleteCommand = new RelayCommand(param =>
        {
            if (param is TodoInstance instance)
                InstanceDeleteRequested?.Invoke(instance);
        });
        DataContext = this;
        InitializeComponent();
        InstanceList.ItemsSource = _instances;
        LoadInstances();
    }

    private void WatchInstance(TodoInstance instance)
    {
        instance.PropertyChanged += async (_, e) =>
        {
            if (e.PropertyName == nameof(TodoInstance.Name))
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

    /// <summary>刷新实例列表（外部添加分组后调用）。</summary>
    public void Reload() => _ = LoadInstancesAsync();

    private async Task LoadInstancesAsync()
    {
        var list = await _instanceService.GetAllAsync();
        _instances.Clear();
        foreach (var item in list)
        {
            WatchInstance(item);
            _instances.Add(item);
        }
    }

    public void RemoveInstance(TodoInstance instance)
    {
        _instances.Remove(instance);
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
            var instance = new TodoInstance { Name = name };
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
