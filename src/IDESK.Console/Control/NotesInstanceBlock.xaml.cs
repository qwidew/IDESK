using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace IDESK.Console.Control;

public partial class NotesInstanceBlock : UserControl
{
    public static readonly DependencyProperty DeleteCommandProperty =
        DependencyProperty.Register(nameof(DeleteCommand), typeof(ICommand), typeof(NotesInstanceBlock));

    public ICommand? DeleteCommand
    {
        get => (ICommand?)GetValue(DeleteCommandProperty);
        set => SetValue(DeleteCommandProperty, value);
    }

    public NotesInstanceBlock()
    {
        InitializeComponent();
    }
}
