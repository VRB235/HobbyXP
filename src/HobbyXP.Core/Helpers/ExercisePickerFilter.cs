using HobbyXP.Models.Physical;

namespace HobbyXP.Helpers;

/// <summary>
/// Filtro de ejercicios para el ComboBox de entrenamiento (nombre, grupo o texto del picker).
/// </summary>
public static class ExercisePickerFilter
{
    public static string? EffectiveQuery(string? query, Exercise? selected)
    {
        if (string.IsNullOrWhiteSpace(query) || selected is null)
            return query;

        var trimmed = query.Trim();
        if (string.Equals(trimmed, selected.PickerDisplayName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, selected.Name, StringComparison.OrdinalIgnoreCase))
            return null;

        return query;
    }

    public static bool MatchesText(Exercise exercise, string? query) =>
        TextSearchFilter.MatchesAny(query, exercise.Name, exercise.PickerDisplayName, exercise.MuscleGroupLabel);

    public static IEnumerable<Exercise> Filter(
        IEnumerable<Exercise> catalog,
        string? query,
        int? selectedId)
    {
        var selected = selectedId is int id
            ? catalog.FirstOrDefault(e => e.Id == id)
            : null;
        var effective = EffectiveQuery(query, selected);

        foreach (var exercise in catalog)
        {
            if (selectedId == exercise.Id || MatchesText(exercise, effective))
                yield return exercise;
        }
    }
}
