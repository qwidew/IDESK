using IDESK.Widgets.Translate.Models;

namespace IDESK.Widgets.Translate.Service;

public interface ITranslateService
{
    Task<bool> GetCreatedAsync();
    Task SetCreatedAsync();
    Task<TranslateConfig?> GetConfigAsync();
    Task SaveConfigAsync(TranslateConfig config);
}
