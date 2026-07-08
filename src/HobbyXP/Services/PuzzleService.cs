using HobbyXP.Data;
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

    public PuzzleService(IDbContextFactory<HobbyXpDbContext> dbContextFactory, IXpService xpService)
    {
        _dbContextFactory = dbContextFactory;
        _xpService = xpService;
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
        string? photoPath = null,
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
            PhotoPath = photoPath,
            CompletedAt = DateTime.UtcNow
        };

        db.Puzzles.Add(puzzle);
        await db.SaveChangesAsync(cancellationToken);

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

        return OperationResult<Puzzle>.WithEvents(puzzle, events.ToArray());
    }
}
