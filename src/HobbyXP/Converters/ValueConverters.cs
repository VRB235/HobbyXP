using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HobbyXP.Converters;

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Visibility.Visible;
}

public sealed class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is null or "" ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class ProgressRatioConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length < 2 ||
            !double.TryParse(values[0]?.ToString(), out var current) ||
            !double.TryParse(values[1]?.ToString(), out var total) ||
            total <= 0)
        {
            return 0d;
        }

        return Math.Clamp(current / total * 100d, 0d, 100d);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Evita valores NaN/Infinity o fuera de rango en ProgressBar y Slider (0–100 por defecto).
/// </summary>
public sealed class ProgressValueClampConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var max = 100d;
        if (parameter is string param && double.TryParse(param, out var parsedMax))
            max = parsedMax;

        var number = value switch
        {
            double d => d,
            float f => f,
            int i => i,
            long l => l,
            decimal m => (double)m,
            _ when value is not null && double.TryParse(value.ToString(), out var parsed) => parsed,
            _ => 0d
        };

        if (double.IsNaN(number) || double.IsInfinity(number))
            return 0d;

        return Math.Clamp(number, 0d, max);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value ?? 0d;
}

/// <summary>
/// Calcula el ancho del indicador de una ProgressBar personalizada.
/// </summary>
public sealed class ProgressBarWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length < 4)
            return 0d;

        var value = values[0] is double v ? v : 0d;
        var minimum = values[1] is double min ? min : 0d;
        var maximum = values[2] is double max ? max : 100d;
        var trackWidth = values[3] is double width ? width : 0d;

        if (double.IsNaN(value) || double.IsInfinity(value) || trackWidth <= 0 || maximum <= minimum)
            return 0d;

        var ratio = (value - minimum) / (maximum - minimum);
        ratio = Math.Clamp(ratio, 0d, 1d);
        return trackWidth * ratio;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Limita un valor numérico al máximo indicado por el segundo binding (p. ej. páginas leídas vs total).
/// </summary>
public sealed class ProgressValueToMaxConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length < 2)
            return 0d;

        var current = values[0] switch
        {
            int i => (double)i,
            double d => d,
            _ when values[0] is not null && double.TryParse(values[0].ToString(), out var parsed) => parsed,
            _ => 0d
        };

        var maximum = values[1] switch
        {
            int i => Math.Max(1, i),
            double d => Math.Max(1d, d),
            _ when values[1] is not null && double.TryParse(values[1].ToString(), out var parsed) => Math.Max(1d, parsed),
            _ => 1d
        };

        if (double.IsNaN(current) || double.IsInfinity(current))
            return 0d;

        return Math.Clamp(current, 0d, maximum);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Garantiza un máximo &gt;= 1 para barras cuyo Maximum depende de TotalPages.
/// </summary>
public sealed class MinIntConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var floor = 1;
        if (parameter is string param && int.TryParse(param, out var parsedFloor))
            floor = parsedFloor;

        if (value is int intValue)
            return Math.Max(floor, intValue);

        if (value is not null && int.TryParse(value.ToString(), out var parsed))
            return Math.Max(floor, parsed);

        return floor;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value ?? 1;
}
