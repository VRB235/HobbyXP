using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace HobbyXP.Helpers;

/// <summary>
/// Abre el desplegable de un ComboBox editable al escribir, para autocompletar.
/// </summary>
public static class ComboBoxAssist
{
    public static readonly DependencyProperty OpenDropDownOnTextChangeProperty =
        DependencyProperty.RegisterAttached(
            "OpenDropDownOnTextChange",
            typeof(bool),
            typeof(ComboBoxAssist),
            new PropertyMetadata(false, OnOpenDropDownOnTextChangeChanged));

    public static bool GetOpenDropDownOnTextChange(DependencyObject obj) =>
        (bool)obj.GetValue(OpenDropDownOnTextChangeProperty);

    public static void SetOpenDropDownOnTextChange(DependencyObject obj, bool value) =>
        obj.SetValue(OpenDropDownOnTextChangeProperty, value);

    private static void OnOpenDropDownOnTextChangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ComboBox combo)
            return;

        combo.RemoveHandler(TextBoxBase.TextChangedEvent, (RoutedEventHandler)OnTextChanged);
        if ((bool)e.NewValue)
            combo.AddHandler(TextBoxBase.TextChangedEvent, (RoutedEventHandler)OnTextChanged, true);
    }

    private static void OnTextChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not ComboBox { IsEditable: true, IsKeyboardFocusWithin: true } combo)
            return;

        if (!combo.IsDropDownOpen)
            combo.IsDropDownOpen = true;
    }
}
