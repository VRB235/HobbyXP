namespace HobbyXP.Services.Results;

public sealed record RacePreparationStats(
    int OfficialRaceId,
    string RaceName,
    int TrainingSessionCount,
    decimal TotalTrainingKm,
    double? BestPaceMinPerKm);
