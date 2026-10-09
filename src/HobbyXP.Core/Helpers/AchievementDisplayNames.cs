using HobbyXP.Models.Enums;

namespace HobbyXP.Helpers;

public static class AchievementDisplayNames
{
    public static string ForActionType(AchievementActionType actionType) => actionType switch
    {
        AchievementActionType.RunningKilometer => "Running — kilómetro",
        AchievementActionType.GymWorkoutSaved => "Gimnasio — sesión guardada",
        AchievementActionType.ProgressiveOverload => "Gimnasio — récord por ejercicio",
        AchievementActionType.OfficialRaceCompleted => "Carrera oficial completada",
        AchievementActionType.PuzzleCompleted => "Rompecabezas completado",
        AchievementActionType.MediaCompleted => "Película terminada",
        AchievementActionType.MediaChapterWatched => "Serie — XP repartido en capítulos",
        AchievementActionType.WeeklyQuotaPenalty => "Disciplina — castigo/restauración",
        AchievementActionType.VideoGamePercent => "Videojuego — avance (%)",
        AchievementActionType.VideoGamePlatinum => "Videojuego platinado",
        AchievementActionType.BookPageRead => "Libro — página leída",
        AchievementActionType.BookCompleted => "Libro terminado",
        AchievementActionType.CourseCompleted => "Curso terminado",
        AchievementActionType.RewardRedeemed => "Premio canjeado",
        AchievementActionType.CourseSessionCompleted => "Curso — sesión completada",
        AchievementActionType.HobbyLevelUp => "Bonus global — nivel de hobby",
        AchievementActionType.DietMealOnPlan => "Dieta — comida en plan",
        AchievementActionType.DietPerfectDay => "Dieta — día perfecto",
        AchievementActionType.MedalPrivilegeBonus => "Medalla — bonus de saldo",
        _ => actionType.ToString()
    };
}
