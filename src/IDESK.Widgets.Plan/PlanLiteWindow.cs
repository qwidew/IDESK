using IDESK.Core;
using IDESK.Widgets.Plan.Service;

namespace IDESK.Widgets.Plan;

public class PlanLiteWindow : DeskWidget
{
    public PlanLiteWindow(IPlanLiteService service)
    {
        var vm = new PlanLiteViewModel(service);
        NormalContent = new PlanLiteView(vm);
        Title = "计划 Lite";
    }
}
