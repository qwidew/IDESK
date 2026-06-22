using IDESK.Core;
using IDESK.Widgets.Habit.Service;

namespace IDESK.Widgets.Habit;

public class HabitWindow : DeskWidget
{
    public HabitWindow(IHabitService service)
    {
        var vm = new HabitGridViewModel(service);
        NormalContent = new HabitGridView(vm);
        Title = "习惯";
    }
}
