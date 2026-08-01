using HobbyXP.Helpers;
using HobbyXP.Models.Enums;

namespace HobbyXP.ViewModels.Physical;

public sealed class RunningSessionTypeOption
{
    private RunningSessionTypeOption(RunningSessionType? value, string label, bool matchesUnassignedOnly)
    {
        Value = value;
        Label = label;
        MatchesUnassignedOnly = matchesUnassignedOnly;
    }

    public RunningSessionType? Value { get; }

    public string Label { get; }

    public bool MatchesUnassignedOnly { get; }

    public bool IsAllFilter => Value is null && !MatchesUnassignedOnly;

    public static RunningSessionTypeOption Create(RunningSessionType? value, string label) =>
        new(value, label, matchesUnassignedOnly: false);

    public static RunningSessionTypeOption CreateUnassignedOnly(string label) =>
        new(null, label, matchesUnassignedOnly: true);

    public static IReadOnlyList<RunningSessionTypeOption> CreateCatalogOptions() =>
        Enum.GetValues<RunningSessionType>()
            .Select(t => Create(t, RunningSessionTypeLabels.Get(t)))
            .ToList();

    public static IReadOnlyList<RunningSessionTypeOption> CreateFilterOptions()
    {
        var options = new List<RunningSessionTypeOption>
        {
            Create(null, "Todos los tipos")
        };

        foreach (RunningSessionType type in Enum.GetValues<RunningSessionType>())
            options.Add(Create(type, RunningSessionTypeLabels.Get(type)));

        options.Add(CreateUnassignedOnly("Sin tipo"));
        return options;
    }

    public bool Matches(RunningSessionType? sessionType)
    {
        if (IsAllFilter)
            return true;

        if (MatchesUnassignedOnly)
            return sessionType is null;

        return sessionType == Value;
    }
}
