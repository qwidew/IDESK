using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using IDESK.Core;
using IDESK.Widgets.Todo.Control;

namespace IDESK.Widgets.Plan;

public partial class PlanLiteView : UserControl
{
    private readonly PlanLiteViewModel _vm;

    public PlanLiteView(PlanLiteViewModel vm)
    {
        _vm = vm;
        InitializeComponent();
        DataContext = vm;
    }

    private void OnDateClick(object sender, MouseButtonEventArgs e)
    {
        var win = Window.GetWindow(this);
        if (win == null) return;
        var dialog = new CalendarDialog
        {
            SelectedDate = _vm.SelectedDate,
            Topmost = true,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
        };
        dialog.DateSelected += date =>
        {
            if (date.HasValue) _vm.SelectedDate = date.Value;
        };
        dialog.Show();
    }
}
