using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace HobbyXP.Views.Controls;

public partial class NumericStepper : UserControl
{
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            nameof(Value),
            typeof(string),
            typeof(NumericStepper),
            new FrameworkPropertyMetadata("0", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty StepProperty =
        DependencyProperty.Register(nameof(Step), typeof(decimal), typeof(NumericStepper), new PropertyMetadata(1m));

    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register(nameof(Minimum), typeof(decimal), typeof(NumericStepper), new PropertyMetadata(0m));

    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register(nameof(Maximum), typeof(decimal), typeof(NumericStepper), new PropertyMetadata(9999m));

    public static readonly DependencyProperty DecimalPlacesProperty =
        DependencyProperty.Register(nameof(DecimalPlaces), typeof(int), typeof(NumericStepper), new PropertyMetadata(0));

    public NumericStepper()
    {
        InitializeComponent();
    }

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public decimal Step
    {
        get => (decimal)GetValue(StepProperty);
        set => SetValue(StepProperty, value);
    }

    public decimal Minimum
    {
        get => (decimal)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public decimal Maximum
    {
        get => (decimal)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public int DecimalPlaces
    {
        get => (int)GetValue(DecimalPlacesProperty);
        set => SetValue(DecimalPlacesProperty, value);
    }

    private void OnIncrement(object sender, RoutedEventArgs e) => Adjust(Step);

    private void OnDecrement(object sender, RoutedEventArgs e) => Adjust(-Step);

    private void Adjust(decimal delta)
    {
        var current = TryParse(Value, out var parsed) ? parsed : Minimum;
        var next = Math.Clamp(current + delta, Minimum, Maximum);
        Value = Format(next);
    }

    private static bool TryParse(string? text, out decimal value) =>
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value)
        || decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value);

    private string Format(decimal value) =>
        DecimalPlaces > 0
            ? value.ToString($"F{DecimalPlaces}", CultureInfo.InvariantCulture)
            : ((int)value).ToString(CultureInfo.InvariantCulture);
}
