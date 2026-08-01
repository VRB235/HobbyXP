using HobbyXP.Models.Enums;
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
        RunningSessionType sessionType,
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

    Task<RunningSession?> UpdateSessionTypeAsync(
        int sessionId,
        RunningSessionType sessionType,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteSessionAsync(int sessionId, CancellationToken cancellationToken = default);

    Task<bool> DeleteOfficialRaceAsync(int raceId, CancellationToken cancellationToken = default);
}
