namespace HobbyXP.Helpers;

/// <summary>
/// Opción de filtro por enum (incluye «Todos»).
/// </summary>
public sealed class EnumFilterOption<TEnum> where TEnum : struct, Enum
{
    private EnumFilterOption(TEnum? value, string label)
    {
        Value = value;
        Label = label;
    }

    public TEnum? Value { get; }

    public string Label { get; }

    public bool IsAll => Value is null;

    public static EnumFilterOption<TEnum> All(string label) => new(null, label);

    public static EnumFilterOption<TEnum> Of(TEnum value, string label) => new(value, label);

    public static IReadOnlyList<EnumFilterOption<TEnum>> Create(
        string allLabel,
        Func<TEnum, string> labelSelector)
    {
        var options = new List<EnumFilterOption<TEnum>> { All(allLabel) };
        foreach (var value in Enum.GetValues<TEnum>())
            options.Add(Of(value, labelSelector(value)));
        return options;
    }

    public bool Matches(TEnum actual) =>
        IsAll || EqualityComparer<TEnum>.Default.Equals(Value!.Value, actual);
}
