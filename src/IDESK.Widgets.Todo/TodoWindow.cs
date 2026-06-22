using IDESK.Core;

namespace IDESK.Widgets.Todo;

public class TodoWindow : DeskWidget
{
    public TodoWindow(TodoListViewModel vm)
    {
        var view = new TodoListView();
        view.DataContext = vm;
        NormalContent = view;
    }
}
