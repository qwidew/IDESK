using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace IDESK.Widgets.Todo.Control;

public partial class CalendarDialog : Window
{
    private bool _closing;

    public event Action<DateTime?>? DateSelected;

    public DateTime? SelectedDate
    {
        get => (DateTime?)GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }
    public static readonly DependencyProperty SelectedDateProperty =
        DependencyProperty.Register(nameof(SelectedDate), typeof(DateTime?),
            typeof(CalendarDialog));

    public CalendarDialog()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            DatePicker.SelectedDatesChanged -= OnSelectedDatesChanged;
            if (SelectedDate.HasValue)
                DatePicker.SelectedDate = SelectedDate;
            DatePicker.SelectedDatesChanged += OnSelectedDatesChanged;
        };
    }

    private void OnDeactivated(object? sender, EventArgs e)
    {
        if (!_closing) { _closing = true; Close(); }
    }

    private void OnTitleMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        _closing = true;
        Close();
    }

    private void OnSelectedDatesChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_closing) return;
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is DateTime date)
        {
            _closing = true;
            SelectedDate = date;
            DateSelected?.Invoke(date);
            Close();
        }
    }

    private void OnClearClick(object sender, RoutedEventArgs e)
    {
        _closing = true;
        SelectedDate = null;
        DateSelected?.Invoke(null);
        Close();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && !_closing)
        {
            _closing = true;
            Close();
        }
    }
}
