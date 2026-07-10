using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using HobbyXP.Helpers;
using HobbyXP.Models.Enums;

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

public sealed class InverseNullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is null or "" ? Visibility.Visible : Visibility.Collapsed;

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

/// <summary>
/// Convierte propiedades numéricas (int, decimal, nullable) a texto para NumericStepper y viceversa.
/// </summary>
public sealed class PuzzleCategoryDisplayConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            PuzzleCategory.TwoD => "2D",
            PuzzleCategory.ThreeD => "3D",
            _ => string.Empty
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            "2D" => PuzzleCategory.TwoD,
            "3D" => PuzzleCategory.ThreeD,
            _ => Binding.DoNothing
        };
}

public sealed class PathToImageSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var path = value as string;
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var local = LocalImageLoader.TryLoad(path);
        if (local is not null)
            return local;

        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class UtcToLocalDateTimeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is DateTime utc
            ? utc.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime()
                : utc.ToLocalTime()
            : value;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class PhotoPathsToImagesConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var paths = PuzzlePhotoStorage.Deserialize(value as string);
        return paths
            .Select(LocalImageLoader.TryLoad)
            .Where(image => image is not null)
            .ToList();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class FlexibleNumberConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
            return string.Empty;

        return value switch
        {
            decimal d => d.ToString(CultureInfo.InvariantCulture),
            int i => i.ToString(CultureInfo.InvariantCulture),
            double d => d.ToString(CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = value as string ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
            return Nullable.GetUnderlyingType(targetType) is not null ? null : Activator.CreateInstance(targetType);

        if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            && !decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out parsed))
        {
            return Binding.DoNothing;
        }

        if (targetType == typeof(int))
            return (int)parsed;

        if (targetType == typeof(int?))
            return (int)parsed;

        if (targetType == typeof(decimal))
            return parsed;

        if (targetType == typeof(decimal?))
            return parsed;

        return parsed;
    }
}

