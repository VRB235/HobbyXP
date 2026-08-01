using HobbyXP.Helpers;
using HobbyXP.Models.Enums;

namespace HobbyXP.ViewModels.Physical;

public sealed class ExerciseTypeOption
{
    public ExerciseTypeOption(ExerciseType value, string label)
    {
        Value = value;
        Label = label;
    }

    public ExerciseType Value { get; }

    public string Label { get; }

    public static IReadOnlyList<ExerciseTypeOption> All { get; } =
        Enum.GetValues<ExerciseType>()
            .Select(t => new ExerciseTypeOption(t, ExerciseTypeLabels.Get(t)))
            .ToList();
}
