using System.Windows;
using System.Windows.Controls;

namespace LyoConsole.Utilities;

public static class TextBoxBehaviors
{
    public static readonly DependencyProperty AutoScrollToEndProperty =
        DependencyProperty.RegisterAttached(
            "AutoScrollToEnd",
            typeof(bool),
            typeof(TextBoxBehaviors),
            new PropertyMetadata(false, OnAutoScrollToEndChanged));

    public static bool GetAutoScrollToEnd(DependencyObject obj) => (bool)obj.GetValue(AutoScrollToEndProperty);
    public static void SetAutoScrollToEnd(DependencyObject obj, bool value) => obj.SetValue(AutoScrollToEndProperty, value);

    private static void OnAutoScrollToEndChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox textBox)
            return;

        if (e.NewValue is true)
            textBox.TextChanged += OnTextChanged;
        else
            textBox.TextChanged -= OnTextChanged;
    }

    private static void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
            textBox.ScrollToEnd();
    }
}

