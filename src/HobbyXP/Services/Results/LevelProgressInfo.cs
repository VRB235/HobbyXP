namespace HobbyXP.Services.Results;

/// <summary>
/// Progreso RPG del jugador para la barra del dashboard.
/// </summary>
public sealed record LevelProgressInfo(
    int CurrentLevel,
    int TotalXp,
    int XpIntoCurrentLevel,
    int XpRequiredForNextLevel,
    double ProgressPercentage);
