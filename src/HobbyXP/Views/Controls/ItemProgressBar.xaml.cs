using System.Windows;
using System.Windows.Controls;

namespace HobbyXP.Views.Controls;

public partial class ItemProgressBar : UserControl
{
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(ItemProgressBar),
            new PropertyMetadata(0d, null, CoerceValue));

    public ItemProgressBar() => InitializeComponent();

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    private static object CoerceValue(DependencyObject d, object? baseValue)
    {
        var value = baseValue is double number ? number : 0d;
        if (double.IsNaN(value) || double.IsInfinity(value))
            return 0d;

        return Math.Clamp(value, 0d, 100d);
    }
}
