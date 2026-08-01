using HobbyXP.Data;
using HobbyXP.Models.Enums;
using HobbyXP.Models.Physical;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Results;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Services;

public sealed class RunningService : IRunningService
{
    private readonly IDbContextFactory<HobbyXpDbContext> _dbContextFactory;
    private readonly IXpService _xpService;
    private readonly IAchievementEngineService _achievementEngine;

    public RunningService(
        IDbContextFactory<HobbyXpDbContext> dbContextFactory,
        IXpService xpService,
        IAchievementEngineService achievementEngine)
    {
        _dbContextFactory = dbContextFactory;
        _xpService = xpService;
        _achievementEngine = achievementEngine;
    }

    public async Task<IReadOnlyList<RunningSession>> GetSessionsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.RunningSessions
            .AsNoTracking()
            .Include(s => s.Carrera)
            .OrderByDescending(s => s.RecordedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OfficialRace>> GetOfficialRacesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.OfficialRaces
            .AsNoTracking()
            .OrderBy(r => r.IsCompleted)
            .ThenByDescending(r => r.EventDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<OfficialRace?> GetOfficialRaceByIdAsync(int raceId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.OfficialRaces
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == raceId, cancellationToken);
    }

    public async Task<OperationResult<RunningSession>> SaveSessionAsync(
        decimal distanceKm,
        TimeSpan duration,
        RunningSessionType sessionType,
        int? carreraId = null,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        if (distanceKm <= 0)
            throw new ArgumentOutOfRangeException(nameof(distanceKm), "La distancia debe ser mayor que cero.");

        if (duration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(duration), "El tiempo debe ser mayor que cero.");

        if (!Enum.IsDefined(sessionType))
            throw new ArgumentOutOfRangeException(nameof(sessionType), "Tipo de sesión inválido.");

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        if (carreraId.HasValue &&
            !await db.OfficialRaces.AnyAsync(r => r.Id == carreraId.Value, cancellationToken))
        {
            throw new InvalidOperationException($"No existe la carrera oficial con Id {carreraId.Value}.");
        }

        var pace = duration.TotalMinutes / (double)distanceKm;
        var session = new RunningSession
        {
            DistanceKm = distanceKm,
            Duration = duration,
            PaceMinPerKm = pace,
            SessionType = sessionType,
            CarreraId = carreraId,
            Notes = notes,
            RecordedAt = DateTime.UtcNow
        };

        db.RunningSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);

        var xpOutcome = await _xpService.AwardXpAsync(
            AchievementActionType.RunningKilometer,
            distanceKm,
            $"Running: {distanceKm:0.##} km",
            MilestoneSourceType.Running,
            nameof(RunningSession),
            session.Id,
            $"Sesión de running ({distanceKm:0.##} km)",
            cancellationToken);

        session.XpEarned = xpOutcome.AmountAwarded;
        session.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        if (carreraId.HasValue)
        {
            session.Carrera = await db.OfficialRaces
                .AsNoTracking()
                .FirstAsync(r => r.Id == carreraId.Value, cancellationToken);
        }

        var events = new List<AchievementEvent>();
        if (xpOutcome.Milestone is not null)
        {
            events.Add(new AchievementEvent(
                xpOutcome.Milestone.Title,
                xpOutcome.Milestone.Description ?? xpOutcome.Milestone.Title,
                xpOutcome.AmountAwarded,
                MilestoneSourceType.Running));
        }

        if (xpOutcome.LeveledUp && xpOutcome.NewLevel.HasValue)
        {
            events.Add(new AchievementEvent(
                $"¡Nivel {xpOutcome.NewLevel.Value}!",
                "Subiste de nivel por tu actividad de running.",
                0,
                MilestoneSourceType.System));
        }

        events.AddRange(await _achievementEngine.TryAwardMilestonesForTrackAsync(
            MedalMilestoneTrack.RunningSessions,
            MilestoneSourceType.Running,
            nameof(RunningSession),
            session.Id,
            cancellationToken));

        events.AddRange(await _achievementEngine.TryAwardMilestonesForTrackAsync(
            MedalMilestoneTrack.RunningKilometers,
            MilestoneSourceType.Running,
            nameof(RunningSession),
            session.Id,
            cancellationToken));

        return OperationResult<RunningSession>.WithEvents(session, events.ToArray());
    }

    public async Task<OfficialRace> SaveOfficialRaceAsync(
        OfficialRace race,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        if (race.Id == 0)
        {
            db.OfficialRaces.Add(race);
        }
        else
        {
            var existing = await db.OfficialRaces.FindAsync([race.Id], cancellationToken)
                ?? throw new InvalidOperationException($"No existe la carrera con Id {race.Id}.");

            existing.Name = race.Name;
            existing.DistanceKm = race.DistanceKm;
            existing.EventDate = race.EventDate;
            existing.Location = race.Location;
            existing.Description = race.Description;
            existing.UpdatedAt = DateTime.UtcNow;
            race = existing;
        }

        await db.SaveChangesAsync(cancellationToken);
        return race;
    }

    public async Task<OperationResult<OfficialRace>> CompleteOfficialRaceAsync(
        int raceId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var race = await db.OfficialRaces.FindAsync([raceId], cancellationToken)
            ?? throw new InvalidOperationException($"No existe la carrera con Id {raceId}.");

        if (race.IsCompleted)
            return OperationResult<OfficialRace>.Empty(race);

        race.IsCompleted = true;
        race.CompletedAt = DateTime.UtcNow;
        race.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        var events = new List<AchievementEvent>();
        var xpOutcome = await _xpService.AwardFlatBonusAsync(
            AchievementActionType.OfficialRaceCompleted,
            await _xpService.CalculatePointsAsync(AchievementActionType.OfficialRaceCompleted, 1, cancellationToken),
            $"Carrera oficial completada: {race.Name}",
            MilestoneSourceType.OfficialRace,
            nameof(OfficialRace),
            race.Id,
            $"Carrera completada: {race.Name}",
            cancellationToken);

        race.BonusXpAwarded = xpOutcome.AmountAwarded;
        race.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        if (xpOutcome.Milestone is not null)
        {
            events.Add(new AchievementEvent(
                xpOutcome.Milestone.Title,
                xpOutcome.Milestone.Description ?? race.Name,
                xpOutcome.AmountAwarded,
                MilestoneSourceType.OfficialRace,
                RequiresCelebration: true));
        }

        var medalEvents = await _achievementEngine.TryAwardMilestonesForTrackAsync(
            MedalMilestoneTrack.OfficialRacesCompleted,
            MilestoneSourceType.OfficialRace,
            nameof(OfficialRace),
            race.Id,
            cancellationToken);

        events.AddRange(medalEvents);

        return OperationResult<OfficialRace>.WithEvents(race, events.ToArray());
    }

    public async Task<RacePreparationStats> GetRacePreparationStatsAsync(
        int raceId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var race = await db.OfficialRaces
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == raceId, cancellationToken)
            ?? throw new InvalidOperationException($"No existe la carrera con Id {raceId}.");

        var sessions = await db.RunningSessions
            .AsNoTracking()
            .Where(s => s.CarreraId == raceId)
            .ToListAsync(cancellationToken);

        return new RacePreparationStats(
            race.Id,
            race.Name,
            sessions.Count,
            sessions.Sum(s => s.DistanceKm),
            sessions.Count == 0 ? null : sessions.Min(s => s.PaceMinPerKm));
    }

    public async Task<RunningSession?> UpdateSessionTypeAsync(
        int sessionId,
        RunningSessionType sessionType,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(sessionType))
            throw new ArgumentOutOfRangeException(nameof(sessionType), "Tipo de sesión inválido.");

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var session = await db.RunningSessions
            .Include(s => s.Carrera)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
        if (session is null)
            return null;

        session.SessionType = sessionType;
        session.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        // Detach-friendly copy for UI (AsNoTracking-style consumers).
        db.Entry(session).State = EntityState.Detached;
        return session;
    }

    public async Task<bool> DeleteSessionAsync(int sessionId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var session = await db.RunningSessions.FindAsync([sessionId], cancellationToken);
        if (session is null)
            return false;

        await _xpService.RevokeXpForSourceAsync(
            MilestoneSourceType.Running,
            nameof(RunningSession),
            sessionId,
            $"Eliminado del historial: sesión de running ({session.DistanceKm:0.##} km)",
            cancellationToken);

        db.RunningSessions.Remove(session);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteOfficialRaceAsync(int raceId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var race = await db.OfficialRaces.FindAsync([raceId], cancellationToken);
        if (race is null)
            return false;

        await _xpService.RevokeXpForSourceAsync(
            MilestoneSourceType.OfficialRace,
            nameof(OfficialRace),
            raceId,
            $"Eliminado del historial: carrera oficial {race.Name}",
            cancellationToken);

        db.OfficialRaces.Remove(race);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
