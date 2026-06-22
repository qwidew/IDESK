using IDESK.Widgets.Plan.Models;

namespace IDESK.Widgets.Plan.Service;

public interface IPlanLiteService
{
    Task<bool> GetCreatedAsync();
    Task SetCreatedAsync();
    Task<PlanConfig?> GetConfigAsync();
    Task SaveConfigAsync(PlanConfig config);

    Task<List<PlanItem>> GetByDateAsync(DateTime date);
    Task<PlanItem> AddAsync(PlanItem item);
    Task UpdateAsync(PlanItem item);
    Task DeleteAsync(PlanItem item);
}
