using IDESK.Core;

namespace IDESK.Widgets.Notes;

public class NotesWindow : DeskWidget
{
    public NotesWindow(NotesViewModel vm)
    {
        var view = new NotesView();
        view.DataContext = vm;
        NormalContent = view;
        Title = "Notes";
    }
}
