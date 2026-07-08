using HobbyXP.Data;
using HobbyXP.Models.Entertainment;
using HobbyXP.Models.Enums;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Results;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Services;

public sealed class VideoGameService : IVideoGameService
{
    private readonly IDbContextFactory<HobbyXpDbContext> _dbContextFactory;
    private readonly IXpService _xpService;
    private readonly IAchievementEngineService _achievementEngine;

    public VideoGameService(
        IDbContextFactory<HobbyXpDbContext> dbContextFactory,
        IXpService xpService,
        IAchievementEngineService achievementEngine)
    {
        _dbContextFactory = dbContextFactory;
        _xpService = xpService;
        _achievementEngine = achievementEngine;
    }

    public async Task<IReadOnlyList<VideoGame>> GetInProgressAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.VideoGames
            .AsNoTracking()
            .Where(g => g.Status == VideoGameStatus.InProgress)
            .OrderByDescending(g => g.UpdatedAt ?? g.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VideoGame>> GetPlatinumAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.VideoGames
            .AsNoTracking()
            .Where(g => g.Status == VideoGameStatus.Platinum)
            .OrderByDescending(g => g.PlatinumUnlockedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<OperationResult<VideoGame>> RegisterAsync(
        string title,
        VideoGamePlatform platform,
        int initialCompletionPercentage = 0,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("El título es obligatorio.", nameof(title));

        var percentage = Math.Clamp(initialCompletionPercentage, 0, 100);

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var game = new VideoGame
        {
            Title = title.Trim(),
            Platform = platform,
            CompletionPercentage = percentage,
            Status = percentage >= 100 ? VideoGameStatus.Platinum : VideoGameStatus.InProgress,
            StartedAt = DateTime.UtcNow,
            PlatinumUnlockedAt = percentage >= 100 ? DateTime.UtcNow : null
        };

        db.VideoGames.Add(game);
        await db.SaveChangesAsync(cancellationToken);

        return await ApplyCompletionDeltaAsync(game.Id, previousPercentage: 0, percentage, cancellationToken);
    }

    public Task<OperationResult<VideoGame>> UpdateCompletionAsync(
        int videoGameId,
        int newCompletionPercentage,
        CancellationToken cancellationToken = default)
    {
        var clamped = Math.Clamp(newCompletionPercentage, 0, 100);
        return UpdateCompletionInternalAsync(videoGameId, clamped, cancellationToken);
    }

    public async Task<OperationResult<VideoGame>> IncrementCompletionAsync(
        int videoGameId,
        int increment = 1,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var game = await db.VideoGames.FindAsync([videoGameId], cancellationToken)
            ?? throw new InvalidOperationException($"No existe el videojuego con Id {videoGameId}.");

        var target = Math.Clamp(game.CompletionPercentage + increment, 0, 100);
        return await UpdateCompletionInternalAsync(videoGameId, target, cancellationToken);
    }

    private async Task<OperationResult<VideoGame>> UpdateCompletionInternalAsync(
        int videoGameId,
        int newPercentage,
        CancellationToken cancellationToken)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var game = await db.VideoGames.FindAsync([videoGameId], cancellationToken)
            ?? throw new InvalidOperationException($"No existe el videojuego con Id {videoGameId}.");

        if (game.Status == VideoGameStatus.Platinum)
            return OperationResult<VideoGame>.Empty(game);

        var previous = game.CompletionPercentage;
        if (newPercentage == previous)
            return OperationResult<VideoGame>.Empty(game);

        game.CompletionPercentage = newPercentage;
        game.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return await ApplyCompletionDeltaAsync(game.Id, previous, newPercentage, cancellationToken);
    }

    private async Task<OperationResult<VideoGame>> ApplyCompletionDeltaAsync(
        int videoGameId,
        int previousPercentage,
        int newPercentage,
        CancellationToken cancellationToken)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var game = await db.VideoGames.FindAsync([videoGameId], cancellationToken)
            ?? throw new InvalidOperationException($"No existe el videojuego con Id {videoGameId}.");

        var events = new List<AchievementEvent>();
        var delta = Math.Max(0, newPercentage - previousPercentage);

        if (delta > 0 && newPercentage < 100)
        {
            var xpOutcome = await _xpService.AwardXpAsync(
                AchievementActionType.VideoGamePercent,
                delta,
                $"Avance en {game.Title}: +{delta}%",
                MilestoneSourceType.VideoGame,
                nameof(VideoGame),
                game.Id,
                $"Avance: {game.Title} ({newPercentage}%)",
                cancellationToken);

            game.XpEarned += xpOutcome.AmountAwarded;

            if (xpOutcome.Milestone is not null)
            {
                events.Add(new AchievementEvent(
                    xpOutcome.Milestone.Title,
                    xpOutcome.Milestone.Description ?? game.Title,
                    xpOutcome.AmountAwarded,
                    MilestoneSourceType.VideoGame));
            }
        }

        if (newPercentage >= 100 && game.Status != VideoGameStatus.Platinum)
        {
            game.Status = VideoGameStatus.Platinum;
            game.PlatinumUnlockedAt = DateTime.UtcNow;
            game.CompletionPercentage = 100;

            var remainingDelta = Math.Max(0, 100 - previousPercentage);
            if (remainingDelta > 0)
            {
                var percentXp = await _xpService.AwardXpAsync(
                    AchievementActionType.VideoGamePercent,
                    remainingDelta,
                    $"Avance final en {game.Title}",
                    MilestoneSourceType.VideoGame,
                    nameof(VideoGame),
                    game.Id,
                    milestoneTitle: null,
                    cancellationToken);

                game.XpEarned += percentXp.AmountAwarded;
            }

            var platinumXp = await _xpService.AwardFlatBonusAsync(
                AchievementActionType.VideoGamePlatinum,
                await _xpService.CalculatePointsAsync(AchievementActionType.VideoGamePlatinum, 1, cancellationToken),
                $"Videojuego platinado: {game.Title}",
                MilestoneSourceType.VideoGame,
                nameof(VideoGame),
                game.Id,
                $"¡Platino! {game.Title}",
                cancellationToken);

            game.PlatinumBonusXp = platinumXp.AmountAwarded;
            game.XpEarned += platinumXp.AmountAwarded;

            events.Add(new AchievementEvent(
                $"¡Platino! {game.Title}",
                "Completaste el videojuego al 100%.",
                platinumXp.AmountAwarded,
                MilestoneSourceType.VideoGame,
                MedalCode.PlatinumGame,
                RequiresCelebration: true));

            var medalEvent = await _achievementEngine.TryAwardMedalAsync(
                MedalCode.PlatinumGame,
                MilestoneSourceType.VideoGame,
                nameof(VideoGame),
                game.Id,
                cancellationToken);

            if (medalEvent is not null)
                events.Add(medalEvent);
        }

        game.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return OperationResult<VideoGame>.WithEvents(game, events.ToArray());
    }
}
