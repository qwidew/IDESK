using Microsoft.Extensions.DependencyInjection;
using IDESK.Widgets.Notes.Service;

namespace IDESK.Widgets.Notes;

public static class NotesServiceExtensions
{
    public static IServiceCollection AddNotesWidget(this IServiceCollection services)
    {
        services.AddSingleton<INotesDataService, NotesDataService>();
        services.AddTransient<NotesViewModel>();
        services.AddTransient<NotesWindow>();
        return services;
    }
}
