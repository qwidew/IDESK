using System.Windows;
using System.Windows.Controls;

namespace IDESK.Console.Control;

public partial class TodayScheduleBlock : UserControl
{
    public event Action? DeleteClicked;

    public TodayScheduleBlock()
    {
        InitializeComponent();
    }

    private void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        DeleteClicked?.Invoke();
    }
}
