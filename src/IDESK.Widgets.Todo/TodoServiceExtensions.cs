using Microsoft.Extensions.DependencyInjection;
using IDESK.Widgets.Todo.Service;

namespace IDESK.Widgets.Todo;

public static class TodoServiceExtensions
{
    public static IServiceCollection AddTodoWidget(this IServiceCollection services)
    {
        services.AddTransient<ITodoDataService, TodoDataService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddTransient<TodoListViewModel>();
        services.AddTransient<TodoWindow>();
        return services;
    }
}
