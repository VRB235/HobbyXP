using HobbyXP.Models.Enums;

namespace HobbyXP.Helpers;

public static class HobbyProgressCatalog
{
    public static readonly MilestoneSourceType[] TrackedHobbies =
    [
        MilestoneSourceType.Running,
        MilestoneSourceType.Gym,
        MilestoneSourceType.OfficialRace,
        MilestoneSourceType.Puzzle,
        MilestoneSourceType.Media,
        MilestoneSourceType.VideoGame,
        MilestoneSourceType.Book,
        MilestoneSourceType.Course
    ];

    public static bool IsTrackedHobby(MilestoneSourceType sourceType) =>
        TrackedHobbies.Contains(sourceType);

    public static string GetDisplayName(MilestoneSourceType sourceType) => sourceType switch
    {
        MilestoneSourceType.Running => "Running",
        MilestoneSourceType.Gym => "Gimnasio",
        MilestoneSourceType.OfficialRace => "Carrera oficial",
        MilestoneSourceType.Puzzle => "Rompecabezas",
        MilestoneSourceType.Media => "Series y películas",
        MilestoneSourceType.VideoGame => "Videojuegos",
        MilestoneSourceType.Book => "Libros",
        MilestoneSourceType.Course => "Cursos",
        _ => sourceType.ToString()
    };

    public static MilestoneSourceType? MapActionToHobby(AchievementActionType actionType) => actionType switch
    {
        AchievementActionType.RunningKilometer => MilestoneSourceType.Running,
        AchievementActionType.GymWorkoutSaved => MilestoneSourceType.Gym,
        AchievementActionType.ProgressiveOverload => MilestoneSourceType.Gym,
        AchievementActionType.OfficialRaceCompleted => MilestoneSourceType.OfficialRace,
        AchievementActionType.PuzzleCompleted => MilestoneSourceType.Puzzle,
        AchievementActionType.MediaCompleted => MilestoneSourceType.Media,
        AchievementActionType.MediaChapterWatched => MilestoneSourceType.Media,
        AchievementActionType.VideoGamePercent => MilestoneSourceType.VideoGame,
        AchievementActionType.VideoGamePlatinum => MilestoneSourceType.VideoGame,
        AchievementActionType.BookPageRead => MilestoneSourceType.Book,
        AchievementActionType.BookCompleted => MilestoneSourceType.Book,
        AchievementActionType.CourseSessionCompleted => MilestoneSourceType.Course,
        AchievementActionType.CourseCompleted => MilestoneSourceType.Course,
        _ => null
    };
}
