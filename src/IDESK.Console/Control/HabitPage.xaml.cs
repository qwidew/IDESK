using System.Windows;
using System.Windows.Controls;
using IDESK.Widgets.Habit.Service;

namespace IDESK.Console.Control;

public partial class HabitPage : UserControl
{
    private readonly IHabitService _habitService;
    public event Action? Created;
    public event Action? DeleteRequested;

    public HabitPage(IHabitService habitService)
    {
        _habitService = habitService;
        InitializeComponent();

        Loaded += async (_, _) =>
        {
            if (await _habitService.GetCreatedAsync())
            {
                CreateBtn.Visibility = Visibility.Collapsed;
                Card.Visibility = Visibility.Visible;
            }
        };
    }

    private async void OnCreateClick(object sender, RoutedEventArgs e)
    {
        CreateBtn.Visibility = Visibility.Collapsed;
        Card.Visibility = Visibility.Visible;
        await _habitService.SetCreatedAsync();
        Created?.Invoke();
    }

    private async void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        Card.Visibility = Visibility.Collapsed;
        CreateBtn.Visibility = Visibility.Visible;
        var cfg = await _habitService.GetConfigAsync();
        if (cfg != null)
        {
            cfg.Created = false;
            await _habitService.SaveConfigAsync(cfg);
        }
        DeleteRequested?.Invoke();
    }
}
