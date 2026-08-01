using HobbyXP.Helpers;
using HobbyXP.Models.Enums;
using HobbyXP.Models.Physical;

namespace HobbyXP.ViewModels.Physical;

/// <summary>
/// Opción de ComboBox para grupo muscular (catálogo y filtro).
/// </summary>
public sealed class MuscleGroupOption
{
    private MuscleGroupOption(MuscleGroup? value, string label, bool matchesUnassignedOnly)
    {
        Value = value;
        Label = label;
        MatchesUnassignedOnly = matchesUnassignedOnly;
    }

    public MuscleGroup? Value { get; }

    public string Label { get; }

    /// <summary>
    /// Filtro especial: solo ejercicios sin grupo.
    /// </summary>
    public bool MatchesUnassignedOnly { get; }

    public bool IsAllFilter => Value is null && !MatchesUnassignedOnly;

    public static MuscleGroupOption Create(MuscleGroup? value, string label) =>
        new(value, label, matchesUnassignedOnly: false);

    public static MuscleGroupOption CreateUnassignedOnly(string label) =>
        new(null, label, matchesUnassignedOnly: true);

    public static IReadOnlyList<MuscleGroupOption> CreateCatalogOptions()
    {
        var options = new List<MuscleGroupOption> { Create(null, "Sin grupo") };

        foreach (MuscleGroup group in Enum.GetValues<MuscleGroup>())
            options.Add(Create(group, MuscleGroupLabels.Get(group)));

        return options;
    }

    public static IReadOnlyList<MuscleGroupOption> CreateFilterOptions()
    {
        var options = new List<MuscleGroupOption> { Create(null, "Todos los grupos") };

        foreach (MuscleGroup group in Enum.GetValues<MuscleGroup>())
            options.Add(Create(group, MuscleGroupLabels.Get(group)));

        options.Add(CreateUnassignedOnly("Sin grupo"));
        return options;
    }

    public bool Matches(MuscleGroup? muscleGroup)
    {
        if (IsAllFilter)
            return true;

        if (MatchesUnassignedOnly)
            return muscleGroup is null;

        return muscleGroup == Value;
    }

    /// <summary>
    /// True si el entrenamiento incluye al menos un ejercicio del grupo filtrado.
    /// </summary>
    public bool MatchesWorkout(GymWorkout workout)
    {
        if (IsAllFilter)
            return true;

        var groups = workout.Entries
            .Select(e => e.Exercise?.MuscleGroup)
            .Distinct()
            .ToList();

        if (MatchesUnassignedOnly)
            return groups.Count == 0 || groups.Any(g => g is null);

        return groups.Any(g => g == Value);
    }
}
