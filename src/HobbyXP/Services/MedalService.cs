using HobbyXP.Data;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Results;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Services;

public sealed class MedalService : IMedalService
{
    private readonly IDbContextFactory<HobbyXpDbContext> _dbContextFactory;

    public MedalService(IDbContextFactory<HobbyXpDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<IReadOnlyList<MedalShowcaseItem>> GetShowcaseAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var definitions = await db.MedalDefinitions
            .AsNoTracking()
            .OrderBy(m => m.Id)
            .ToListAsync(cancellationToken);

        var earned = await db.EarnedMedals
            .AsNoTracking()
            .Include(m => m.MedalDefinition)
            .ToListAsync(cancellationToken);

        var earnedLookup = earned
            .GroupBy(m => m.MedalDefinitionId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.EarnedAt).First());

        return definitions
            .Select(definition =>
            {
                earnedLookup.TryGetValue(definition.Id, out var instance);
                return new MedalShowcaseItem(
                    definition.Id,
                    definition.Code,
                    definition.Name,
                    definition.Description,
                    definition.UnlockHint,
                    definition.IconPath,
                    instance is not null,
                    instance?.EarnedAt);
            })
            .ToList();
    }

    public async Task<IReadOnlyList<Models.Achievements.EarnedMedal>> GetEarnedMedalsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.EarnedMedals
            .AsNoTracking()
            .Include(m => m.MedalDefinition)
            .OrderByDescending(m => m.EarnedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Models.Achievements.MedalDefinition>> GetAllDefinitionsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.MedalDefinitions
            .AsNoTracking()
            .OrderBy(m => m.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<Models.Achievements.MedalDefinition> UpdateDefinitionAsync(
        Models.Achievements.MedalDefinition definition,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await db.MedalDefinitions.FindAsync([definition.Id], cancellationToken)
            ?? throw new InvalidOperationException($"No se encontró la medalla con Id {definition.Id}.");

        existing.Name = definition.Name;
        existing.Description = definition.Description;
        existing.UnlockHint = definition.UnlockHint;
        existing.IconPath = definition.IconPath;
        existing.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return existing;
    }
}
