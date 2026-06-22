using IDESK.Core;
using IDESK.Widgets.Schedule.Service;

namespace IDESK.Widgets.Schedule;

public class ScheduleWindow : DeskWidget
{
    public ScheduleWindow(ScheduleViewModel vm, IScheduleService service)
    {
        var view = new ScheduleView(vm, service);
        NormalContent = view;
        Title = "日程管理";
    }
}
