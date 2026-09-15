using HobbyXP.Models.Physical;

namespace HobbyXP.Helpers;

public static class GymLastPerformance
{
    public static GymWorkoutEntry? FindLatest(IEnumerable<GymWorkout> workouts, int exerciseId) =>
        workouts
            .SelectMany(workout => workout.Entries.Select(entry => (workout, entry)))
            .Where(x => x.entry.ExerciseId == exerciseId)
            .OrderByDescending(x => x.workout.WorkoutDate)
            .ThenByDescending(x => x.workout.Id)
            .ThenByDescending(x => x.entry.Id)
            .Select(x => x.entry)
            .FirstOrDefault();
}
