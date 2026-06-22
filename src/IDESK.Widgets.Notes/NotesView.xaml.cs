using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace IDESK.Widgets.Notes;

public partial class NotesView : UserControl
{
    public NotesView()
    {
        InitializeComponent();
    }

    private void OnEditTitle(object sender, MouseButtonEventArgs e)
    {
        TitleText.Visibility = Visibility.Collapsed;
        TitleBox.Visibility = Visibility.Visible;
        TitleBox.Focus();
        TitleBox.SelectAll();
        e.Handled = true;
    }

    private void OnEditTitleEnd(object sender, RoutedEventArgs e)
    {
        TitleBox.Visibility = Visibility.Collapsed;
        TitleText.Visibility = Visibility.Visible;
    }

    private void OnEditTitleKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var binding = TitleBox.GetBindingExpression(TextBox.TextProperty);
            binding?.UpdateSource();
            OnEditTitleEnd(sender, e);
        }
        else if (e.Key == Key.Escape)
        {
            OnEditTitleEnd(sender, e);
        }
    }
}
