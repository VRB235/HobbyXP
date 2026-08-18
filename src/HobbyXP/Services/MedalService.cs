using HobbyXP.Data;
using HobbyXP.Helpers;
using HobbyXP.Models.Enums;
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

        var catalogByCode = MedalCatalog.Entries.ToDictionary(e => e.Code);

        return definitions
            .Select(definition =>
            {
                earnedLookup.TryGetValue(definition.Id, out var instance);
                catalogByCode.TryGetValue(definition.Code, out var catalog);
                var source = catalog is null
                    ? MilestoneSourceType.System
                    : MedalTrackMap.SourceFor(catalog.Track) ?? MilestoneSourceType.System;
                return new MedalShowcaseItem(
                    definition.Id,
                    definition.Code,
                    definition.Name,
                    definition.Description,
                    definition.UnlockHint,
                    ResolveIconPath(definition),
                    instance is not null,
                    instance?.EarnedAt,
                    source);
            })
            .ToList();
    }

    public async Task<IReadOnlyList<MedalShowcaseSection>> GetShowcaseSectionsAsync(
        CancellationToken cancellationToken = default)
    {
        var items = await GetShowcaseAsync(cancellationToken);
        var catalogByCode = MedalCatalog.Entries.ToDictionary(e => e.Code);

        return HobbyProgressCatalog.TrackedHobbies
            .Select(source =>
            {
                var medals = items
                    .Where(item => item.SourceType == source)
                    .OrderByDescending(item => item.IsEarned)
                    .ThenByDescending(item => item.EarnedAt)
                    .ThenBy(item => catalogByCode.TryGetValue(item.Code, out var entry) ? entry.Threshold : int.MaxValue)
                    .ThenBy(item => item.MedalDefinitionId)
                    .ToList();

                return new MedalShowcaseSection(
                    source,
                    HobbyProgressCatalog.GetDisplayName(source),
                    medals);
            })
            .Where(section => section.Medals.Count > 0)
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
        var definitions = await db.MedalDefinitions
            .AsNoTracking()
            .OrderBy(m => m.Id)
            .ToListAsync(cancellationToken);

        foreach (var definition in definitions)
            definition.IconPath = ResolveIconPath(definition);

        return definitions;
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
        existing.IconPath = ResolveIconPath(existing);
        return existing;
    }

    private static string ResolveIconPath(Models.Achievements.MedalDefinition definition) =>
        string.IsNullOrWhiteSpace(definition.IconPath)
            ? MedalIconPaths.ForMedalCode(definition.Code)
            : definition.IconPath;
}
