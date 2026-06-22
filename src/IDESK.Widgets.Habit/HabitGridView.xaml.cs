using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using IDESK.Widgets.Habit.Service;

namespace IDESK.Widgets.Habit;

public partial class HabitGridView : UserControl
{
    private readonly HabitGridViewModel _vm;

    public HabitGridView(HabitGridViewModel vm)
    {
        _vm = vm;
        InitializeComponent();
        DataContext = vm;
        Loaded += async (_, _) => await BuildGridAsync();
        HabitService.DataChanged += async () => await Dispatcher.InvokeAsync(BuildGridAsync);
    }

    private async Task BuildGridAsync()
    {
        await _vm.LoadAsync();

        var grid = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };
        grid.SetResourceReference(Grid.BackgroundProperty, "CardBackgroundBrush");

        int colCount = 9;
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
        for (int i = 0; i < 7; i++)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });

        // Header
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(36) });
        var headerBg = new Border();
        headerBg.SetResourceReference(Border.BackgroundProperty, "SectionBgBrush");
        AddToGrid(grid, headerBg, 0, 0, colCount, 1);

        var headerText = new TextBlock
        {
            Text = "习惯", FontSize = 12, FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        headerText.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondaryBrush");
        AddToGrid(grid, headerText, 0, 0);

        for (int c = 1; c <= 7; c++)
        {
            var day = _vm.Days[c - 1];
            var dayText = new TextBlock
            {
                Text = day.Label, FontSize = 11,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            dayText.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondaryBrush");
            AddToGrid(grid, dayText, c, 0);
        }

        var fadedStyle = (Style)FindResource("FadedButtonStyle");
        var closeIcon = FindResource("IconClose") as StreamGeometry;
        var addIcon = FindResource("IconAdd") as StreamGeometry;

        // Data rows
        foreach (var row in _vm.Rows)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });
            int r = grid.RowDefinitions.Count - 1;

            if (r % 2 == 1)
            {
                var rowBg = new Border();
                rowBg.SetResourceReference(Border.BackgroundProperty, "ItemBgBrush");
                AddToGrid(grid, rowBg, 0, r, colCount, 1);
            }

            int hid = row.Habit.Id;
            var cell = BuildNameCell(row.Habit.Title, hid, _vm, "TextPrimaryBrush");
            AddToGrid(grid, cell, 0, r);

            for (int d = 0; d < 7; d++)
            {
                int di = d;
                var cb = new CheckBox
                {
                    IsChecked = row.Completed[d],
                    Style = (Style)FindResource("HabitCheckBoxStyle"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                cb.Checked += (_, _) => _ = _vm.ToggleAsync(row.Habit.Id, di);
                cb.Unchecked += (_, _) => _ = _vm.ToggleAsync(row.Habit.Id, di);
                AddToGrid(grid, cb, d + 1, r);
            }

            // Delete button
            var delBtn = new Button { Style = fadedStyle, Width = 24, Height = 24, ToolTip = "删除习惯" };
            var delIcon = new Path { Data = closeIcon, Width = 10, Height = 10, Stretch = Stretch.Uniform };
            delIcon.SetResourceReference(Path.FillProperty, "AccentBrush");
            delBtn.Content = delIcon;
            delBtn.Click += async (_, _) => { await _vm.DeleteHabitAsync(hid); await BuildGridAsync(); };
            AddToGrid(grid, delBtn, 8, r);
        }

        // Add button (+)
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });
        int lastRow = grid.RowDefinitions.Count - 1;

        var addBtn = new Button
        {
            Style = fadedStyle,
            Width = 28, Height = 28,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            ToolTip = "添加习惯",
        };
        var addIconPath = new Path { Data = addIcon, Width = 18, Height = 18, Stretch = Stretch.Uniform };
        addIconPath.SetResourceReference(Path.FillProperty, "AccentBrush");
        addBtn.Content = addIconPath;
        addBtn.Click += OnAddHabitClick;
        AddToGrid(grid, addBtn, 0, lastRow, colCount, 1);

        TableContainer.Content = grid;
    }

    private static UIElement BuildNameCell(string title, int habitId, HabitGridViewModel vm, string fgResourceKey)
    {
        var tb = new Label
        {
            Content = title, FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Cursor = Cursors.Hand,
            Padding = new Thickness(0),
        };
        tb.SetResourceReference(Label.ForegroundProperty, fgResourceKey);

        var box = new TextBox
        {
            Text = title, FontSize = 13,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            Visibility = Visibility.Collapsed,
        };
        box.SetResourceReference(TextBox.CaretBrushProperty, "TextPrimaryBrush");

        tb.MouseDown += (_, e) =>
        {
            if (box.Visibility == Visibility.Visible) return;
            tb.Visibility = Visibility.Collapsed;
            box.Visibility = Visibility.Visible;
            box.Focus();
            box.SelectAll();
            e.Handled = true;
        };

        void Finish()
        {
            string text = box.Text.Trim();
            if (string.IsNullOrEmpty(text)) text = "新习惯";
            tb.Content = text;
            box.Text = text;
            box.Visibility = Visibility.Collapsed;
            tb.Visibility = Visibility.Visible;
            _ = vm.UpdateTitleAsync(habitId, text);
        }

        box.LostFocus += (_, _) => Finish();
        box.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter) Finish();
            if (e.Key == Key.Escape) { box.Text = (string)tb.Content; Finish(); }
        };

        var cell = new Grid { Background = Brushes.Transparent };
        cell.Children.Add(tb);
        cell.Children.Add(box);
        return cell;
    }

    private async void OnAddHabitClick(object sender, RoutedEventArgs e)
    {
        await _vm.AddEmptyHabitAsync();
        await BuildGridAsync();
    }

    private async void OnPrevWeek(object sender, RoutedEventArgs e)
    {
        _vm.MoveWeek(-1);
        await BuildGridAsync();
    }

    private async void OnNextWeek(object sender, RoutedEventArgs e)
    {
        _vm.MoveWeek(1);
        await BuildGridAsync();
    }

    private static void AddToGrid(Grid grid, UIElement el, int col, int row, int colSpan = 1, int rowSpan = 1)
    {
        Grid.SetColumn(el, col);
        Grid.SetRow(el, row);
        if (colSpan > 1) Grid.SetColumnSpan(el, colSpan);
        if (rowSpan > 1) Grid.SetRowSpan(el, rowSpan);
        grid.Children.Add(el);
    }
}
