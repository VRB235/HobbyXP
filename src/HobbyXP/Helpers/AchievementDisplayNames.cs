using HobbyXP.Data;
using HobbyXP.Models.Enums;

namespace HobbyXP.Helpers;

public static class AchievementDisplayNames
{
    public static string ForActionType(AchievementActionType actionType) => actionType switch
    {
        AchievementActionType.RunningKilometer => "Running — kilómetro",
        AchievementActionType.GymWorkoutSaved => "Gimnasio — sesión guardada",
        AchievementActionType.ProgressiveOverload => "Gimnasio — sobrecarga progresiva",
        AchievementActionType.OfficialRaceCompleted => "Carrera oficial completada",
        AchievementActionType.PuzzleCompleted => "Rompecabezas completado",
        AchievementActionType.MediaCompleted => "Serie o película terminada",
        AchievementActionType.MediaChapterWatched => "Serie — capítulo visto",
        AchievementActionType.WeeklyQuotaPenalty => "Disciplina semanal — castigo/restauración",
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

    public static string ForMedalTrack(MedalMilestoneTrack track) => track switch
    {
        MedalMilestoneTrack.BooksCompleted => "Libros terminados",
        MedalMilestoneTrack.BookPagesRead => "Páginas leídas",
        MedalMilestoneTrack.MediaCompleted => "Series y películas",
        MedalMilestoneTrack.PuzzlesCompleted => "Rompecabezas",
        MedalMilestoneTrack.CoursesCompleted => "Cursos terminados",
        MedalMilestoneTrack.CourseSessions => "Sesiones de curso",
        MedalMilestoneTrack.OfficialRacesCompleted => "Carreras oficiales",
        MedalMilestoneTrack.RunningSessions => "Sesiones de running",
        MedalMilestoneTrack.RunningKilometers => "Kilómetros corridos",
        MedalMilestoneTrack.GymWorkouts => "Entrenamientos de gym",
        MedalMilestoneTrack.ProgressiveOverloadPrs => "Sobrecarga progresiva",
        MedalMilestoneTrack.VideoGamesPlatinum => "Videojuegos platinados",
        MedalMilestoneTrack.DietGoodDays => "Días buenos de dieta",
        MedalMilestoneTrack.DietPerfectDays => "Días perfectos de dieta",
        _ => track.ToString()
    };

    public static string ForMedalCode(MedalCode code)
    {
        var entry = MedalCatalog.Entries.FirstOrDefault(e => e.Code == code);
        return entry is null ? code.ToString() : ForMedalTrack(entry.Track);
    }
}
