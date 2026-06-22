using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace IDESK.Widgets.Schedule;

public partial class MonthlyCalendar : UserControl
{
    private DateTime _currentMonth = new(DateTime.Now.Year, DateTime.Now.Month, 1);
    private Border? _selectedCell;

    public DateTime? SelectedDate { get; private set; }
    public DateTime CurrentMonth => _currentMonth;
    public HashSet<DateTime> MarkedDates { get; set; } = [];
    public event Action<DateTime>? DateSelected;
    public event Action? MonthChanged;

    public MonthlyCalendar()
    {
        InitializeComponent();
        RenderMonth(_currentMonth);
        SelectToday();
    }

    public void SelectToday()
    {
        var today = DateTime.Now;
        _currentMonth = new DateTime(today.Year, today.Month, 1);
        SelectedDate = today;
        RenderMonth(_currentMonth);
        DateSelected?.Invoke(today);
    }

    private void OnPrevMonth(object sender, RoutedEventArgs e)
    {
        _currentMonth = _currentMonth.AddMonths(-1);
        RenderMonth(_currentMonth);
        MonthChanged?.Invoke();
    }

    private void OnNextMonth(object sender, RoutedEventArgs e)
    {
        _currentMonth = _currentMonth.AddMonths(1);
        RenderMonth(_currentMonth);
        MonthChanged?.Invoke();
    }

    public void RenderMonth(DateTime month)
    {
        MonthLabel.Content = $"{month.Year} 年 {month.Month} 月";
        DayGrid.Children.Clear();
        _selectedCell = null;

        int daysInMonth = DateTime.DaysInMonth(month.Year, month.Month);
        int startDow = (int)new DateTime(month.Year, month.Month, 1).DayOfWeek;
        int totalCells = startDow + daysInMonth;
        int rows = (totalCells + 6) / 7;

        for (int i = 0; i < rows * 7; i++)
        {
            int day = i - startDow + 1;
            var cell = new Border
            {
                Style = (Style)FindResource("CalendarDayCellStyle"),
                Background = Brushes.Transparent,
                BorderBrush = (Brush)FindResource("CardBorderBrush"),
                BorderThickness = new Thickness(0.5),
                Cursor = Cursors.Hand
            };

            if (day >= 1 && day <= daysInMonth)
            {
                DateTime cellDate = new(month.Year, month.Month, day);
                var sp = new StackPanel { VerticalAlignment = VerticalAlignment.Top };
                sp.Children.Add(new TextBlock
                {
                    Text = day.ToString(),
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Center
                });

                bool hasItem = MarkedDates.Contains(cellDate.Date);
                Brush fg = (Brush)FindResource("TextPrimaryBrush");

                if (cellDate.Date == DateTime.Now.Date)
                {
                    cell.Background = (Brush)FindResource("ItemBgBrush");
                    fg = (Brush)FindResource("AccentBrush");
                }

                if (SelectedDate.HasValue && cellDate.Date == SelectedDate.Value.Date)
                {
                    cell.Background = (Brush)FindResource("AccentBrush");
                    fg = Brushes.White;
                    _selectedCell = cell;
                }

                foreach (TextBlock t in sp.Children.OfType<TextBlock>()) t.Foreground = fg;
                if (hasItem)
                    sp.Children.Add(new Ellipse
                    {
                        Width = 5, Height = 5, Fill = fg,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 1, 0, 0)
                    });
                cell.Child = sp;
                var captured = cellDate;
                cell.MouseLeftButtonDown += (_, _) => SelectDay(captured, cell);
            }

            DayGrid.Children.Add(cell);
        }
    }

    private void SelectDay(DateTime date, Border cell)
    {
        SelectedDate = date;
        DateSelected?.Invoke(date);
        RenderMonth(_currentMonth);
    }
}
