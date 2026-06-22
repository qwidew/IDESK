using System.Windows;

namespace IDESK.Widgets.Todo.Service;

public class DialogService : IDialogService
{
    public bool Confirm(string itemName)
    {
        return MessageBox.Show(
            $"确定要删除「{itemName}」吗？",
            "确认",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question
        ) == MessageBoxResult.Yes;
    }

    public void ShowError(string message)
    {
        MessageBox.Show(message, "error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}