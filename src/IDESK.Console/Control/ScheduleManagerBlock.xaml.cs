using System.Windows;
using System.Windows.Controls;

namespace IDESK.Console.Control;

public partial class ScheduleManagerBlock : UserControl
{
    public event Action? DeleteClicked;

    public ScheduleManagerBlock()
    {
        InitializeComponent();
    }

    public bool IsDeleteEnabled
    {
        get => DeleteBtn.IsEnabled;
        set => DeleteBtn.IsEnabled = value;
    }

    private void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        DeleteClicked?.Invoke();
    }
}
