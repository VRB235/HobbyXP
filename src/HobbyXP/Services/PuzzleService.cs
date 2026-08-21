using HobbyXP.Data;
using HobbyXP.Helpers;
using HobbyXP.Models.Entertainment;
using HobbyXP.Models.Enums;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Results;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Services;

public sealed class PuzzleService : IPuzzleService
{
    private readonly IDbContextFactory<HobbyXpDbContext> _dbContextFactory;
    private readonly IXpService _xpService;
    private readonly IAchievementEngineService _achievementEngine;
    private readonly IWeeklyQuotaService _weeklyQuotaService;

    public PuzzleService(
        IDbContextFactory<HobbyXpDbContext> dbContextFactory,
        IXpService xpService,
        IAchievementEngineService achievementEngine,
        IWeeklyQuotaService weeklyQuotaService)
    {
        _dbContextFactory = dbContextFactory;
        _xpService = xpService;
        _achievementEngine = achievementEngine;
        _weeklyQuotaService = weeklyQuotaService;
    }

    public async Task<IReadOnlyList<Puzzle>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Puzzles
            .AsNoTracking()
            .OrderByDescending(p => p.CompletedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<OperationResult<Puzzle>> RegisterCompletedAsync(
        string name,
        int pieceCount,
        PuzzleCategory category,
        IReadOnlyList<string>? photoPaths = null,
        DateTime? completedAt = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre es obligatorio.", nameof(name));

        if (pieceCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(pieceCount));

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var puzzle = new Puzzle
        {
            Name = name.Trim(),
            PieceCount = pieceCount,
            Category = category,
            CompletedAt = completedAt ?? DateTime.UtcNow
        };

        db.Puzzles.Add(puzzle);
        await db.SaveChangesAsync(cancellationToken);

        if (photoPaths is { Count: > 0 })
        {
            var savedPhotos = PuzzlePhotoStorage.SavePhotos(puzzle.Id, photoPaths);
            puzzle.PhotoPath = PuzzlePhotoStorage.Serialize(savedPhotos);
            await db.SaveChangesAsync(cancellationToken);
        }

        var xpOutcome = await _xpService.AwardXpAsync(
            AchievementActionType.PuzzleCompleted,
            units: 1,
            $"Rompecabezas completado: {puzzle.Name}",
            MilestoneSourceType.Puzzle,
            nameof(Puzzle),
            puzzle.Id,
            $"Rompecabezas: {puzzle.Name}",
            cancellationToken);

        puzzle.XpEarned = xpOutcome.AmountAwarded;
        await db.SaveChangesAsync(cancellationToken);

        var events = new List<AchievementEvent>();
        if (xpOutcome.Milestone is not null)
        {
            events.Add(new AchievementEvent(
                xpOutcome.Milestone.Title,
                xpOutcome.Milestone.Description ?? puzzle.Name,
                xpOutcome.AmountAwarded,
                MilestoneSourceType.Puzzle));
        }

        var medalEvents = await _achievementEngine.TryAwardMilestonesForTrackAsync(
            MedalMilestoneTrack.PuzzlesCompleted,
            MilestoneSourceType.Puzzle,
            nameof(Puzzle),
            puzzle.Id,
            cancellationToken);

        events.AddRange(medalEvents);

        var activityLocal = (completedAt ?? DateTime.UtcNow).ToLocalTime().Date;
        await _weeklyQuotaService.NotifyActivityAsync(MilestoneSourceType.Puzzle, activityLocal, cancellationToken);

        return OperationResult<Puzzle>.WithEvents(puzzle, events.ToArray());
    }

    public async Task<Puzzle> UpdateAsync(
        int puzzleId,
        string name,
        int pieceCount,
        PuzzleCategory category,
        DateTime completedAt,
        IReadOnlyList<string>? photoPaths = null,
        bool replacePhotos = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre es obligatorio.", nameof(name));

        if (pieceCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(pieceCount));

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var puzzle = await db.Puzzles.FindAsync([puzzleId], cancellationToken)
            ?? throw new InvalidOperationException($"No existe el rompecabezas con Id {puzzleId}.");

        puzzle.Name = name.Trim();
        puzzle.PieceCount = pieceCount;
        puzzle.Category = category;
        puzzle.CompletedAt = completedAt;
        puzzle.UpdatedAt = DateTime.UtcNow;

        if (replacePhotos)
        {
            PuzzlePhotoStorage.DeleteStoredPhotos(puzzle.Id, puzzle.PhotoPath);
            puzzle.PhotoPath = null;

            if (photoPaths is { Count: > 0 })
            {
                var savedPhotos = PuzzlePhotoStorage.SavePhotos(puzzle.Id, photoPaths);
                puzzle.PhotoPath = PuzzlePhotoStorage.Serialize(savedPhotos);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return puzzle;
    }

    public async Task<bool> DeleteAsync(int puzzleId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var puzzle = await db.Puzzles.FindAsync([puzzleId], cancellationToken);
        if (puzzle is null)
            return false;

        await _xpService.RevokeXpForSourceAsync(
            MilestoneSourceType.Puzzle,
            nameof(Puzzle),
            puzzleId,
            $"Eliminado del historial: rompecabezas {puzzle.Name}",
            cancellationToken);

        PuzzlePhotoStorage.DeleteStoredPhotos(puzzleId, puzzle.PhotoPath);
        db.Puzzles.Remove(puzzle);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
