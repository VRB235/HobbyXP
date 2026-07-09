using System.ComponentModel.DataAnnotations.Schema;
using HobbyXP.Models.Common;

namespace HobbyXP.Models.Physical;

public class GymWorkout : EntityBase
{
    public DateTime WorkoutDate { get; set; } = DateTime.UtcNow;

    public string? Notes { get; set; }

    public int XpEarned { get; set; }

    public bool TriggeredProgressiveOverload { get; set; }

    public ICollection<GymWorkoutEntry> Entries { get; set; } = [];

    [NotMapped]
    public int ExerciseCount => Entries.Count;

    [NotMapped]
    public string ExerciseSummary
    {
        get
        {
            if (Entries.Count == 0)
                return "(Sin ejercicios)";

            var names = Entries
                .OrderBy(e => e.SortOrder)
                .Select(e => e.Exercise?.Name ?? "?")
                .ToList();

            if (names.Count <= 3)
                return string.Join(", ", names);

            return string.Join(", ", names.Take(3)) + $" (+{names.Count - 3})";
        }
    }

    [NotMapped]
    public string OverloadLabel => TriggeredProgressiveOverload ? "¡Récord!" : string.Empty;
}
