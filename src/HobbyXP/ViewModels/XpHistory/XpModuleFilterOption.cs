using HobbyXP.Helpers;
using HobbyXP.Models.Enums;

namespace HobbyXP.ViewModels.XpHistory;

/// <summary>
/// Filtro de módulo del historial XP (Todos / Global / hobby).
/// </summary>
public sealed class XpModuleFilterOption
{
    private XpModuleFilterOption(string label, MilestoneSourceType? sourceType, bool isGlobalOnly)
    {
        Label = label;
        SourceType = sourceType;
        IsGlobalOnly = isGlobalOnly;
    }

    public string Label { get; }

    public MilestoneSourceType? SourceType { get; }

    public bool IsGlobalOnly { get; }

    public bool IsAll => SourceType is null && !IsGlobalOnly;

    public static XpModuleFilterOption All() => new("Todos los módulos", null, false);

    public static XpModuleFilterOption Global() => new("Global", null, true);

    public static XpModuleFilterOption Hobby(MilestoneSourceType sourceType) =>
        new(HobbyProgressCatalog.GetDisplayName(sourceType), sourceType, false);

    public static IReadOnlyList<XpModuleFilterOption> CreateDefault()
    {
        var options = new List<XpModuleFilterOption> { All(), Global() };
        options.AddRange(HobbyProgressCatalog.TrackedHobbies.Select(Hobby));
        return options;
    }

    public bool Matches(XpHistoryRowViewModel row)
    {
        if (IsAll)
            return true;

        if (IsGlobalOnly)
            return row.IsGlobal;

        return !row.IsGlobal && SourceType is { } hobby && row.SourceType == hobby;
    }

    public override string ToString() => Label;
}
