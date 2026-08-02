namespace HobbyXP.Models.Enums;

/// <summary>
/// Acciones interceptables por el motor de reglas de logros.
/// </summary>
public enum AchievementActionType
{
    RunningKilometer = 0,
    GymWorkoutSaved = 1,
    ProgressiveOverload = 2,
    OfficialRaceCompleted = 3,
    PuzzleCompleted = 4,
    MediaCompleted = 5,
    VideoGamePercent = 6,
    VideoGamePlatinum = 7,
    BookPageRead = 8,
    BookCompleted = 9,
    CourseCompleted = 10,
    RewardRedeemed = 11,
    CourseSessionCompleted = 12,
    /// <summary>Bonus al XP global al subir de nivel un hobby.</summary>
    HobbyLevelUp = 13
}
