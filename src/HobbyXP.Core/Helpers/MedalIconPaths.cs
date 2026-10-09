using System.IO;
using HobbyXP.Models.Enums;

namespace HobbyXP.Helpers;

/// <summary>
/// Iconos por defecto por hobby y resolución de rutas de medallas.
/// </summary>
public static class MedalIconPaths
{
    private const string Root = "Assets/Medals";

    public static string ForHobby(MilestoneSourceType sourceType) => sourceType switch
    {
        MilestoneSourceType.Running => $"{Root}/running-session.png",
        MilestoneSourceType.Gym => $"{Root}/gym-workout.png",
        MilestoneSourceType.OfficialRace => $"{Root}/official-race.png",
        MilestoneSourceType.Puzzle => $"{Root}/puzzle.png",
        MilestoneSourceType.Media => $"{Root}/media.png",
        MilestoneSourceType.VideoGame => $"{Root}/platinum-game.png",
        MilestoneSourceType.Book => $"{Root}/book-completed.png",
        MilestoneSourceType.Course => $"{Root}/course-completed.png",
        MilestoneSourceType.Diet => $"{Root}/progressive-overload.png",
        _ => $"{Root}/official-race.png"
    };

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
