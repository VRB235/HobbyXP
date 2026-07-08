using HobbyXP.Models.Physical;
using HobbyXP.Services.Results;

namespace HobbyXP.Services.Abstractions;

public interface IRunningService
{
    Task<IReadOnlyList<RunningSession>> GetSessionsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OfficialRace>> GetOfficialRacesAsync(CancellationToken cancellationToken = default);

    Task<OfficialRace?> GetOfficialRaceByIdAsync(int raceId, CancellationToken cancellationToken = default);

    Task<OperationResult<RunningSession>> SaveSessionAsync(
        decimal distanceKm,
        TimeSpan duration,
        int? carreraId = null,
        string? notes = null,
        CancellationToken cancellationToken = default);

    Task<OfficialRace> SaveOfficialRaceAsync(
        OfficialRace race,
        CancellationToken cancellationToken = default);

    Task<OperationResult<OfficialRace>> CompleteOfficialRaceAsync(
        int raceId,
        CancellationToken cancellationToken = default);

    Task<RacePreparationStats> GetRacePreparationStatsAsync(
        int raceId,
        CancellationToken cancellationToken = default);
}
