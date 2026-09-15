using HobbyXP.Helpers;
using HobbyXP.Models.Enums;
using HobbyXP.Models.Physical;

namespace HobbyXP.Tests.Helpers;

public sealed class TextSearchFilterTests
{
    [Theory]
    [InlineData("Press banca", "press", true)]
    [InlineData("Press banca", "BANCA", true)]
    [InlineData("Press banca", "sentadilla", false)]
    [InlineData("Bíceps curl", "biceps", true)]
    [InlineData("Bíceps curl", "bíceps", true)]
    [InlineData("Pecho", "", true)]
    [InlineData("Pecho", "   ", true)]
    public void Matches_IgnoresCaseAndDiacritics(string haystack, string needle, bool expected) =>
        Assert.Equal(expected, TextSearchFilter.Matches(haystack, needle));
}

public sealed class ExercisePickerFilterTests
{
    [Fact]
    public void Filter_MatchesNameMuscleAndKeepsSelected()
    {
        var press = CreateExercise(1, "Press banca", MuscleGroup.Pecho);
        var curl = CreateExercise(2, "Curl", MuscleGroup.Biceps);
        var squat = CreateExercise(3, "Sentadilla", MuscleGroup.Cuadriceps);

        var filtered = ExercisePickerFilter.Filter([press, curl, squat], "pecho", selectedId: 3).ToList();

        Assert.Contains(press, filtered);
        Assert.Contains(squat, filtered);
        Assert.DoesNotContain(curl, filtered);
    }

    [Fact]
    public void EffectiveQuery_IgnoresSelectedDisplayName()
    {
        var press = CreateExercise(1, "Press banca", MuscleGroup.Pecho);

        var effective = ExercisePickerFilter.EffectiveQuery(press.PickerDisplayName, press);

        Assert.Null(effective);
    }

    private static Exercise CreateExercise(int id, string name, MuscleGroup group) =>
        new()
        {
            Id = id,
            Name = name,
            MuscleGroup = group,
            ExerciseType = ExerciseType.TraditionalWeight
        };
}

public sealed class GymLastPerformanceTests
{
    [Fact]
    public void FindLatest_PrefersNewerWorkoutThenHigherId()
    {
        var older = CreateWorkout(1, new DateTime(2026, 9, 1), CreateEntry(10, exerciseId: 5, sets: 3));
        var newerFirst = CreateWorkout(2, new DateTime(2026, 9, 10), CreateEntry(11, exerciseId: 5, sets: 4));
        var newerLater = CreateWorkout(3, new DateTime(2026, 9, 10), CreateEntry(12, exerciseId: 5, sets: 5));
        var other = CreateWorkout(4, new DateTime(2026, 9, 12), CreateEntry(13, exerciseId: 9, sets: 8));

        var latest = GymLastPerformance.FindLatest([older, newerLater, newerFirst, other], exerciseId: 5);

        Assert.NotNull(latest);
        Assert.Equal(12, latest.Id);
        Assert.Equal(5, latest.Sets);
    }

    [Fact]
    public void FindLatest_ReturnsNullWhenExerciseNeverLogged()
    {
        var workout = CreateWorkout(1, DateTime.UtcNow, CreateEntry(1, exerciseId: 2, sets: 3));

        Assert.Null(GymLastPerformance.FindLatest([workout], exerciseId: 99));
    }

    private static GymWorkout CreateWorkout(int id, DateTime date, GymWorkoutEntry entry) =>
        new()
        {
            Id = id,
            WorkoutDate = date,
            Entries = [entry]
        };

    private static GymWorkoutEntry CreateEntry(int id, int exerciseId, int sets) =>
        new()
        {
            Id = id,
            ExerciseId = exerciseId,
            Sets = sets,
            ExerciseType = ExerciseType.TraditionalWeight,
            Repetitions = 10,
            WeightKg = 40
        };
}
