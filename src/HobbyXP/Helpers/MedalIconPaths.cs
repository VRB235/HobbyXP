using System.IO;
using HobbyXP.Data;
using HobbyXP.Models.Enums;

namespace HobbyXP.Helpers;

/// <summary>
/// Rutas embebidas de iconos por track de medalla (copiados al output en Assets/Medals).
/// </summary>
public static class MedalIconPaths
{
    private const string Root = "Assets/Medals";

    public static string ForTrack(MedalMilestoneTrack track) => track switch
    {
        MedalMilestoneTrack.OfficialRacesCompleted => $"{Root}/official-race.png",
        MedalMilestoneTrack.RunningSessions => $"{Root}/running-session.png",
        MedalMilestoneTrack.RunningKilometers => $"{Root}/running-km.png",
        MedalMilestoneTrack.ProgressiveOverloadPrs => $"{Root}/progressive-overload.png",
        MedalMilestoneTrack.GymWorkouts => $"{Root}/gym-workout.png",
        MedalMilestoneTrack.VideoGamesPlatinum => $"{Root}/platinum-game.png",
        MedalMilestoneTrack.BooksCompleted => $"{Root}/book-completed.png",
        MedalMilestoneTrack.BookPagesRead => $"{Root}/book-pages.png",
        MedalMilestoneTrack.CoursesCompleted => $"{Root}/course-completed.png",
        MedalMilestoneTrack.CourseSessions => $"{Root}/course-sessions.png",
        MedalMilestoneTrack.PuzzlesCompleted => $"{Root}/puzzle.png",
        MedalMilestoneTrack.MediaCompleted => $"{Root}/media.png",
        MedalMilestoneTrack.DietGoodDays => $"{Root}/gym-workout.png",
        MedalMilestoneTrack.DietPerfectDays => $"{Root}/progressive-overload.png",
        _ => $"{Root}/official-race.png"
    };

    public static string ForMedalCode(MedalCode code)
    {
        var entry = MedalCatalog.Entries.FirstOrDefault(e => e.Code == code);
        return entry?.IconPath ?? ForTrack(MedalMilestoneTrack.OfficialRacesCompleted);
    }

    public static string? ResolveAbsolutePath(string? storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
            return null;

        if (Path.IsPathRooted(storedPath))
            return File.Exists(storedPath) ? storedPath : null;

        var normalized = storedPath.Replace('/', Path.DirectorySeparatorChar);
        var fromOutput = Path.Combine(AppContext.BaseDirectory, normalized);
        if (File.Exists(fromOutput))
            return fromOutput;

        return File.Exists(storedPath) ? storedPath : null;
    }
}
