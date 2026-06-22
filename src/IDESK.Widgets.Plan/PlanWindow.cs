using IDESK.Core;
using IDESK.Widgets.Plan.Service;

namespace IDESK.Widgets.Plan;

public class PlanWindow : DeskWidget
{
    public PlanWindow(IPlanService service)
    {
        var vm = new PlanViewModel(service);
        NormalContent = new PlanView(vm);
        Title = "计划";
    }
}
