using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LyoConsole.Utilities;

public static class TabReorderBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(TabReorderBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    private static Point _dragStartPoint;

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TabControl tabControl)
            return;

        if (e.NewValue is true)
        {
            tabControl.AllowDrop = true;
            tabControl.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            tabControl.PreviewMouseMove += OnPreviewMouseMove;
            tabControl.Drop += OnDrop;
        }
        else
        {
            tabControl.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
            tabControl.PreviewMouseMove -= OnPreviewMouseMove;
            tabControl.Drop -= OnDrop;
        }
    }

    private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        => _dragStartPoint = e.GetPosition(null);

    private static void OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
            return;

        var position = e.GetPosition(null);
        var deltaX = Math.Abs(position.X - _dragStartPoint.X);
        var deltaY = Math.Abs(position.Y - _dragStartPoint.Y);
        if (deltaX < SystemParameters.MinimumHorizontalDragDistance &&
            deltaY < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        if (sender is not TabControl tabControl)
            return;

        if (FindAncestor<TabItem>((DependencyObject)e.OriginalSource) is not { } tabItem)
            return;

        DragDrop.DoDragDrop(tabItem, tabItem.DataContext, DragDropEffects.Move);
    }

    private static void OnDrop(object sender, DragEventArgs e)
    {
        if (sender is not TabControl tabControl)
            return;

        if (tabControl.ItemsSource is not IList itemsSource)
            return;

        var draggedItem = e.Data.GetData(typeof(object));
        if (draggedItem is null)
            return;

        var targetTabItem = FindAncestor<TabItem>((DependencyObject)e.OriginalSource);
        var targetItem = targetTabItem?.DataContext;

        var sourceIndex = itemsSource.IndexOf(draggedItem);
        if (sourceIndex < 0)
            return;

        var targetIndex = targetItem is null ? itemsSource.Count - 1 : itemsSource.IndexOf(targetItem);
        if (targetIndex < 0)
            targetIndex = itemsSource.Count - 1;

        if (sourceIndex == targetIndex)
            return;

        itemsSource.RemoveAt(sourceIndex);
        itemsSource.Insert(targetIndex, draggedItem);
        tabControl.SelectedItem = draggedItem;
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T t)
                return t;
            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }
}

