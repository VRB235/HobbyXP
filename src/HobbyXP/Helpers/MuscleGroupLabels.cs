using System.Globalization;
using HobbyXP.Models.Enums;

namespace HobbyXP.Helpers;

public static class MuscleGroupLabels
{
    private static readonly StringComparer LabelComparer =
        StringComparer.Create(CultureInfo.GetCultureInfo("es"), ignoreCase: true);

    /// <summary>
    /// Grupos ordenados alfabéticamente por etiqueta en español (p. ej. para ComboBox).
    /// </summary>
    public static IReadOnlyList<MuscleGroup> OrderedAlphabetically { get; } =
        Enum.GetValues<MuscleGroup>()
            .OrderBy(Get, LabelComparer)
            .ToArray();

    private static readonly IReadOnlyDictionary<MuscleGroup, int> SortOrders =
        OrderedAlphabetically
            .Select((group, index) => (group, order: index + 1))
            .ToDictionary(x => x.group, x => x.order);

    public static string Get(MuscleGroup group) => group switch
    {
        MuscleGroup.Pecho => "Pecho",
        MuscleGroup.Triceps => "Tríceps",
        MuscleGroup.Biceps => "Bíceps",
        MuscleGroup.Hombros => "Hombros",
        MuscleGroup.Core => "Core",
        MuscleGroup.Espalda => "Espalda",
        MuscleGroup.Cuadriceps => "Cuádriceps",
        MuscleGroup.Gemelos => "Gemelos",
        MuscleGroup.Abductores => "Abductores",
        MuscleGroup.Aductores => "Aductores",
        MuscleGroup.Isquiotibiales => "Isquiotibiales",
        MuscleGroup.Gluteos => "Glúteos",
        _ => group.ToString()
    };

    public static string GetOrUnassigned(MuscleGroup? group) =>
        group is null ? "Sin grupo" : Get(group.Value);

    /// <summary>
    /// Orden de presentación: «Sin grupo» primero (0), luego etiquetas A–Z.
    /// </summary>
    public static int GetSortOrder(MuscleGroup? group) =>
        group is null ? 0 : SortOrders[group.Value];
}
