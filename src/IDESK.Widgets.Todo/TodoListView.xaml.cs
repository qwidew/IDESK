using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using IDESK.Widgets.Todo.Models;

namespace IDESK.Widgets.Todo;

public partial class TodoListView : UserControl
{
    public TodoListView()
    {
        InitializeComponent();
    }

    private void OnEditGroupName(object sender, MouseButtonEventArgs e)
    {
        GroupNameText.Visibility = Visibility.Collapsed;
        GroupNameBox.Visibility = Visibility.Visible;
        GroupNameBox.Focus();
        GroupNameBox.SelectAll();
        e.Handled = true;
    }

    private void OnEditGroupEnd(object sender, RoutedEventArgs e)
    {
        GroupNameBox.Visibility = Visibility.Collapsed;
        GroupNameText.Visibility = Visibility.Visible;
    }

    private void OnEditGroupKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var binding = GroupNameBox.GetBindingExpression(TextBox.TextProperty);
            binding?.UpdateSource();
            OnEditGroupEnd(sender, e);
        }
        else if (e.Key == Key.Escape)
        {
            OnEditGroupEnd(sender, e);
        }
    }

    private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var source = e.OriginalSource as DependencyObject;
        var button = FindAncestor<Button>(source);
        if (button == null || button.Command != null || button.Content?.ToString() != "⠿") return;

        var listBoxItem = FindAncestor<ListBoxItem>(source);
        if (listBoxItem == null) return;

        var todoItem = listBoxItem.DataContext as TodoItem;
        if (todoItem == null) return;

        DragDrop.DoDragDrop(listBoxItem, todoItem, DragDropEffects.Move);
    }

    private void OnGiveFeedback(object? sender, GiveFeedbackEventArgs e)
    {
        e.UseDefaultCursors = false;
        e.Handled = true;
    }

    private void OnDragLeave(object sender, DragEventArgs e)
    {
        HideInsertionLine();
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(TodoItem)))
        {
            e.Effects = DragDropEffects.None;
            HideInsertionLine();
            return;
        }
        e.Effects = DragDropEffects.Move;
        e.Handled = true;

        UpdateInsertionLine((ListBox)sender, e.GetPosition((ListBox)sender));
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        var listBox = sender as ListBox;
        var draggedItem = e.Data.GetData(typeof(TodoItem)) as TodoItem;
        if (listBox == null || draggedItem == null) return;

        var vm = DataContext as TodoListViewModel;
        if (vm == null) return;

        HideInsertionLine();

        int targetIndex = GetDropIndex(listBox, e.GetPosition(listBox));
        if (targetIndex < 0) return;

        int oldIndex = vm.TodoList.IndexOf(draggedItem);
        if (oldIndex < 0) return;

        int newIndex = targetIndex > oldIndex ? targetIndex - 1 : targetIndex;
        if (oldIndex == newIndex) return;

        vm.TodoList.Move(oldIndex, newIndex);

        for (int i = 0; i < vm.TodoList.Count; i++)
        {
            vm.TodoList[i].SortOrder = i;
        }
        _ = vm.SaveOrderAsync();
    }

    private void HideInsertionLine()
    {
        InsertionLine.Visibility = Visibility.Collapsed;
    }

    private void UpdateInsertionLine(ListBox listBox, Point pos)
    {
        double lineY = -1;

        if (listBox.Items.Count == 0) return;

        if (pos.Y < 0)
        {
            lineY = 0;
        }
        else
        {
            for (int i = 0; i < listBox.Items.Count; i++)
            {
                var item = listBox.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                if (item == null) continue;

                var bounds = item.TransformToAncestor(listBox)
                               .TransformBounds(new Rect(0, 0, item.ActualWidth, item.ActualHeight));

                if (pos.Y >= bounds.Top && pos.Y <= bounds.Bottom)
                {
                    double midY = bounds.Top + bounds.Height / 2;
                    lineY = pos.Y < midY ? bounds.Top : bounds.Bottom;
                    break;
                }
            }

            if (lineY < 0)
            {
                var lastItem = listBox.ItemContainerGenerator
                    .ContainerFromIndex(listBox.Items.Count - 1) as ListBoxItem;
                if (lastItem != null)
                {
                    var lastBounds = lastItem.TransformToAncestor(listBox)
                        .TransformBounds(new Rect(0, 0, lastItem.ActualWidth, lastItem.ActualHeight));
                    if (pos.Y > lastBounds.Bottom)
                        lineY = lastBounds.Bottom;
                }
            }
        }

        if (lineY >= 0)
        {
            InsertionLine.Width = listBox.ActualWidth;
            Canvas.SetTop(InsertionLine, lineY);
            Canvas.SetLeft(InsertionLine, 0);
            InsertionLine.Visibility = Visibility.Visible;
        }
        else
        {
            HideInsertionLine();
        }
    }

    private int GetDropIndex(ListBox listBox, Point pos)
    {
        if (pos.Y < 0 || listBox.Items.Count == 0) return 0;

        for (int i = 0; i < listBox.Items.Count; i++)
        {
            var item = listBox.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
            if (item == null) continue;

            var bounds = item.TransformToAncestor(listBox)
                           .TransformBounds(new Rect(0, 0, item.ActualWidth, item.ActualHeight));

            if (pos.Y >= bounds.Top && pos.Y <= bounds.Bottom)
            {
                double midY = bounds.Top + bounds.Height / 2;
                return pos.Y < midY ? i : i + 1;
            }
        }

        return listBox.Items.Count;
    }

    private static T? FindAncestor<T>(DependencyObject? element) where T : DependencyObject
    {
        while (element != null)
        {
            if (element is T t) return t;
            element = VisualTreeHelper.GetParent(element);
        }
        return null;
    }
}
