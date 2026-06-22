using IDESK.Core;
using IDESK.Widgets.Schedule.Service;

namespace IDESK.Widgets.Schedule;

public class TodayScheduleWindow : DeskWidget
{
    public TodayScheduleWindow(IScheduleService service)
    {
        var view = new TodayScheduleView(service);
        NormalContent = view;
        Title = "今日日程";
    }

    public async Task LoadDataAsync()
    {
        if (NormalContent is TodayScheduleView view)
            await view.LoadItemsAsync();
    }
}
