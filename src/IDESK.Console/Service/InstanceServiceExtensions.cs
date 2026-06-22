using Microsoft.Extensions.DependencyInjection;

namespace IDESK.Console.Service;

public static class InstanceServiceExtensions
{
    public static IServiceCollection AddInstanceService(this IServiceCollection services)
    {
        services.AddSingleton<IInstanceService, InstanceService>();
        services.AddSingleton<INotesInstanceService, NotesInstanceService>();
        return services;
    }
}
