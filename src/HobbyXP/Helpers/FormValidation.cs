using System.Globalization;

namespace HobbyXP.Helpers;

public readonly record struct ValidationResult(bool IsValid, string? Message)
{
    public static ValidationResult Ok() => new(true, null);

    public static ValidationResult Fail(string message) => new(false, message);

    public static ValidationResult FirstFailure(params ValidationResult[] results)
    {
        foreach (var result in results)
        {
            if (!result.IsValid)
                return result;
        }

        return Ok();
    }
}

public static class FormValidation
{
    public static ValidationResult RequireText(string? value, string fieldLabel) =>
        string.IsNullOrWhiteSpace(value) ? ValidationResult.Fail($"Indique {fieldLabel}.") : ValidationResult.Ok();

    public static ValidationResult RequireValue<T>(T? value, string fieldLabel)
        where T : class =>
        value is null ? ValidationResult.Fail($"Indique {fieldLabel}.") : ValidationResult.Ok();

    public static ValidationResult RequirePositiveInt(string? text, string fieldLabel, out int parsed)
    {
        parsed = 0;
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out parsed)
            && !int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
        {
            return ValidationResult.Fail($"{fieldLabel} debe ser un número entero.");
        }

        return parsed > 0
            ? ValidationResult.Ok()
            : ValidationResult.Fail($"{fieldLabel} debe ser mayor que cero.");
    }

    public static ValidationResult RequireNonNegativeInt(string? text, string fieldLabel, out int parsed)
    {
        parsed = 0;
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out parsed)
            && !int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
        {
            return ValidationResult.Fail($"{fieldLabel} debe ser un número entero.");
        }

        return parsed >= 0
            ? ValidationResult.Ok()
            : ValidationResult.Fail($"{fieldLabel} no puede ser negativo.");
    }

    public static ValidationResult RequirePositiveDecimal(string? text, string fieldLabel, out decimal parsed)
    {
        parsed = 0m;
        if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out parsed)
            && !decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))
        {
            return ValidationResult.Fail($"{fieldLabel} debe ser un número.");
        }

        return parsed > 0m
            ? ValidationResult.Ok()
            : ValidationResult.Fail($"{fieldLabel} debe ser mayor que cero.");
    }

    public static ValidationResult RequireNonNegativeDecimal(string? text, string fieldLabel, out decimal parsed)
    {
        parsed = 0m;
        if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out parsed)
            && !decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))
        {
            return ValidationResult.Fail($"{fieldLabel} debe ser un número.");
        }

        return parsed >= 0m
            ? ValidationResult.Ok()
            : ValidationResult.Fail($"{fieldLabel} no puede ser negativo.");
    }

    public static ValidationResult RequireIntInRange(string? text, string fieldLabel, int min, int max, out int parsed)
    {
        var number = RequireNonNegativeInt(text, fieldLabel, out parsed);
        if (!number.IsValid)
            return number;

        return parsed >= min && parsed <= max
            ? ValidationResult.Ok()
            : ValidationResult.Fail($"{fieldLabel} debe estar entre {min} y {max}.");
    }

    public static ValidationResult RequireNotAbove(int value, int maximum, string fieldLabel, string unitLabel) =>
        value <= maximum
            ? ValidationResult.Ok()
            : ValidationResult.Fail($"{fieldLabel} ({value} {unitLabel}) no puede superar el total ({maximum} {unitLabel}).");

    public static ValidationResult FirstFailure(params ValidationResult[] results) =>
        ValidationResult.FirstFailure(results);
}
