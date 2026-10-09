using HobbyXP.Data;
using HobbyXP.Helpers;
using HobbyXP.Models.Achievements;
using HobbyXP.Models.Enums;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Results;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Services;

public sealed class MedalService : IMedalService
{
    private readonly IDbContextFactory<HobbyXpDbContext> _dbContextFactory;
    private readonly IAchievementEngineService _achievementEngine;

    public MedalService(
        IDbContextFactory<HobbyXpDbContext> dbContextFactory,
        IAchievementEngineService achievementEngine)
    {
        _dbContextFactory = dbContextFactory;
        _achievementEngine = achievementEngine;
    }

    public async Task<IReadOnlyList<MedalShowcaseItem>> GetShowcaseAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var definitions = await db.MedalDefinitions
            .AsNoTracking()
            .OrderBy(m => m.SourceType)
            .ThenBy(m => m.XpThreshold)
            .ThenBy(m => m.Id)
            .ToListAsync(cancellationToken);

        var earned = await db.EarnedMedals
            .AsNoTracking()
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
                    ResolveIconPath(definition),
                    instance is not null,
                    instance?.EarnedAt,
                    definition.SourceType,
                    definition.XpThreshold);
            })
            .ToList();
    }

    public async Task<IReadOnlyList<MedalShowcaseSection>> GetShowcaseSectionsAsync(
        CancellationToken cancellationToken = default)
    {
        var items = await GetShowcaseAsync(cancellationToken);

        return HobbyProgressCatalog.TrackedHobbies
            .Select(source =>
            {
                var medals = items
                    .Where(item => item.SourceType == source)
                    .OrderByDescending(item => item.IsEarned)
                    .ThenByDescending(item => item.EarnedAt)
                    .ThenBy(item => item.XpThreshold)
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

    public async Task<IReadOnlyList<EarnedMedal>> GetEarnedMedalsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.EarnedMedals
            .AsNoTracking()
            .Include(m => m.MedalDefinition)
            .OrderByDescending(m => m.EarnedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MedalDefinition>> GetAllDefinitionsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var definitions = await db.MedalDefinitions
            .AsNoTracking()
            .OrderBy(m => m.SourceType)
            .ThenBy(m => m.XpThreshold)
            .ThenBy(m => m.Id)
            .ToListAsync(cancellationToken);

        foreach (var definition in definitions)
            definition.IconPath = ResolveIconPath(definition);

        return definitions;
    }

    public async Task<MedalDefinition> CreateAsync(
        MilestoneSourceType sourceType,
        int xpThreshold,
        string name,
        string description,
        string unlockHint,
        string? iconPath = null,
        CancellationToken cancellationToken = default)
    {
        if (!HobbyProgressCatalog.IsTrackedHobby(sourceType))
            throw new InvalidOperationException($"El hobby '{sourceType}' no admite medallas.");

        if (xpThreshold < 1)
            throw new ArgumentOutOfRangeException(nameof(xpThreshold), "El umbral de XP debe ser al menos 1.");

        var trimmedName = name.Trim();
        var trimmedDescription = description.Trim();
        var trimmedHint = unlockHint.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
            throw new ArgumentException("El nombre es obligatorio.", nameof(name));
        if (string.IsNullOrWhiteSpace(trimmedDescription))
            throw new ArgumentException("La descripción es obligatoria.", nameof(description));
        if (string.IsNullOrWhiteSpace(trimmedHint))
            throw new ArgumentException("La pista de desbloqueo es obligatoria.", nameof(unlockHint));

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var duplicateThreshold = await db.MedalDefinitions
            .AnyAsync(m => m.SourceType == sourceType && m.XpThreshold == xpThreshold, cancellationToken);
        if (duplicateThreshold)
            throw new InvalidOperationException(
                $"Ya existe una medalla de {HobbyProgressCatalog.GetDisplayName(sourceType)} con umbral {xpThreshold:N0} XP.");

        var entity = new MedalDefinition
        {
            Code = $"m_{Guid.NewGuid():N}",
            SourceType = sourceType,
            XpThreshold = xpThreshold,
            Name = trimmedName,
            Description = trimmedDescription,
            UnlockHint = trimmedHint,
            IconPath = string.IsNullOrWhiteSpace(iconPath) ? MedalIconPaths.ForHobby(sourceType) : iconPath.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        db.MedalDefinitions.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        // Si el XP actual ya supera el umbral, otorgar de inmediato.
        await _achievementEngine.TryAwardHobbyXpMedalsAsync(sourceType, cancellationToken);

        entity.IconPath = ResolveIconPath(entity);
        return entity;
    }

    public async Task<MedalDefinition> UpdateDefinitionAsync(
        MedalDefinition definition,
        CancellationToken cancellationToken = default)
    {
        if (definition.XpThreshold < 1)
            throw new ArgumentOutOfRangeException(nameof(definition), "El umbral de XP debe ser al menos 1.");

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await db.MedalDefinitions.FindAsync([definition.Id], cancellationToken)
            ?? throw new InvalidOperationException($"No se encontró la medalla con Id {definition.Id}.");

        var duplicateThreshold = await db.MedalDefinitions
            .AnyAsync(
                m => m.Id != definition.Id
                     && m.SourceType == definition.SourceType
                     && m.XpThreshold == definition.XpThreshold,
                cancellationToken);
        if (duplicateThreshold)
            throw new InvalidOperationException(
                $"Ya existe una medalla de {HobbyProgressCatalog.GetDisplayName(definition.SourceType)} con umbral {definition.XpThreshold:N0} XP.");

        existing.SourceType = definition.SourceType;
        existing.XpThreshold = definition.XpThreshold;
        existing.Name = definition.Name.Trim();
        existing.Description = definition.Description.Trim();
        existing.UnlockHint = definition.UnlockHint.Trim();
        existing.IconPath = string.IsNullOrWhiteSpace(definition.IconPath)
            ? MedalIconPaths.ForHobby(definition.SourceType)
            : definition.IconPath.Trim();
        existing.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        await _achievementEngine.TryAwardHobbyXpMedalsAsync(existing.SourceType, cancellationToken);

        existing.IconPath = ResolveIconPath(existing);
        return existing;
    }

    public async Task DeleteAsync(int medalDefinitionId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await db.MedalDefinitions.FindAsync([medalDefinitionId], cancellationToken)
            ?? throw new InvalidOperationException($"No se encontró la medalla con Id {medalDefinitionId}.");

        await db.EarnedMedals
            .Where(e => e.MedalDefinitionId == medalDefinitionId)
            .ExecuteDeleteAsync(cancellationToken);

        db.MedalDefinitions.Remove(existing);
        await db.SaveChangesAsync(cancellationToken);
    }

    public IReadOnlyList<string> SuggestNames(MilestoneSourceType sourceType, int xpThreshold) =>
        MedalNameSuggestions.Suggest(sourceType, xpThreshold);

    private static string ResolveIconPath(MedalDefinition definition) =>
        string.IsNullOrWhiteSpace(definition.IconPath)
            ? MedalIconPaths.ForHobby(definition.SourceType)
            : definition.IconPath;
}
