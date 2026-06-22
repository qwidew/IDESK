using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using IDESK.Widgets.Todo.Models;

namespace IDESK.Widgets.Todo.Control;

public partial class TodoItemBlock : UserControl
{
    public string TodoContent
    {
        get => (string)GetValue(TodoContentProperty);
        set => SetValue(TodoContentProperty, value);
    }
    public static readonly DependencyProperty TodoContentProperty =
        DependencyProperty.Register(nameof(TodoContent), typeof(string),
            typeof(TodoItemBlock));

    public bool IsDone
    {
        get => (bool)GetValue(IsDoneProperty);
        set => SetValue(IsDoneProperty, value);
    }
    public static readonly DependencyProperty IsDoneProperty =
        DependencyProperty.Register(nameof(IsDone), typeof(bool),
            typeof(TodoItemBlock));

    public ICommand DeleteCommand
    {
        get => (ICommand)GetValue(DeleteCommandProperty);
        set => SetValue(DeleteCommandProperty, value);
    }
    public static readonly DependencyProperty DeleteCommandProperty =
        DependencyProperty.Register(nameof(DeleteCommand), typeof(ICommand),
            typeof(TodoItemBlock));

    public ICommand CompleteCommand
    {
        get => (ICommand)GetValue(CompleteCommandProperty);
        set => SetValue(CompleteCommandProperty, value);
    }
    public static readonly DependencyProperty CompleteCommandProperty =
        DependencyProperty.Register(nameof(CompleteCommand), typeof(ICommand),
            typeof(TodoItemBlock));

    public TodoItemBlock()
    {
        InitializeComponent();
    }

    private void OnEditName(object sender, MouseButtonEventArgs e)
    {
        DisplayText.Visibility = Visibility.Collapsed;
        EditBox.Visibility = Visibility.Visible;
        EditBox.Focus();
        EditBox.SelectAll();
        e.Handled = true;
    }

    private void OnEditEnd(object sender, RoutedEventArgs e)
    {
        EditBox.Visibility = Visibility.Collapsed;
        DisplayText.Visibility = Visibility.Visible;
    }

    private void OnEditKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var binding = EditBox.GetBindingExpression(TextBox.TextProperty);
            binding?.UpdateSource();
            OnEditEnd(sender, e);
        }
        else if (e.Key == Key.Escape)
        {
            OnEditEnd(sender, e);
        }
    }

    private void OnCalendarClick(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (DataContext is not TodoItem item) return;

        var dialog = new CalendarDialog
        {
            SelectedDate = item.Ddl,
            Topmost = true,
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };

        dialog.DateSelected += date => item.Ddl = date?.Date;
        dialog.Show();
    }
}
