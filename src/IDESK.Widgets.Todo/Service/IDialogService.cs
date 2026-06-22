namespace IDESK.Widgets.Todo.Service;

public interface IDialogService
{
    bool Confirm(string message);
    void ShowError(string message);
}