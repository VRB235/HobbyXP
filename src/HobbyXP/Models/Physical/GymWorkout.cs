using HobbyXP.Models.Common;

namespace HobbyXP.Models.Physical;

public class GymWorkout : EntityBase
{
    public DateTime WorkoutDate { get; set; } = DateTime.UtcNow;

    public string? Notes { get; set; }

    public int XpEarned { get; set; }

    public bool TriggeredProgressiveOverload { get; set; }

    public ICollection<GymWorkoutEntry> Entries { get; set; } = [];
}
