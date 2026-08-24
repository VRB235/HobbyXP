using HobbyXP.Data;
using HobbyXP.Helpers;
using HobbyXP.Models.Core;
using HobbyXP.Models.Enums;
using HobbyXP.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Services;

public sealed class ModuleDisciplineService : IModuleDisciplineService
{
    private readonly IDbContextFactory<HobbyXpDbContext> _dbContextFactory;

    public ModuleDisciplineService(IDbContextFactory<HobbyXpDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<bool> IsPausedAsync(
        MilestoneSourceType sourceType,
        CancellationToken cancellationToken = default)
    {
        if (!WeeklyQuotaRules.TrackedSources.Contains(sourceType))
            return false;

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var row = await db.ModuleDisciplinePauses
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.SourceType == sourceType, cancellationToken);

        return row?.IsPaused == true;
    }

    public async Task<IReadOnlyDictionary<MilestoneSourceType, bool>> GetPauseStatesAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var stored = await db.ModuleDisciplinePauses
            .AsNoTracking()
            .ToDictionaryAsync(p => p.SourceType, p => p.IsPaused, cancellationToken);

        var result = new Dictionary<MilestoneSourceType, bool>();
        foreach (var source in WeeklyQuotaRules.TrackedSources)
            result[source] = stored.GetValueOrDefault(source, false);

        return result;
    }

    public async Task SetPausedAsync(
        MilestoneSourceType sourceType,
        bool isPaused,
        CancellationToken cancellationToken = default)
    {
        if (!WeeklyQuotaRules.TrackedSources.Contains(sourceType))
            throw new ArgumentOutOfRangeException(nameof(sourceType), "Ese módulo no tiene disciplina configurable.");

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var row = await db.ModuleDisciplinePauses
            .FirstOrDefaultAsync(p => p.SourceType == sourceType, cancellationToken);

        if (row is null)
        {
            row = new ModuleDisciplinePause { SourceType = sourceType };
            db.ModuleDisciplinePauses.Add(row);
        }

        row.IsPaused = isPaused;
        row.PausedAtUtc = isPaused ? DateTime.UtcNow : null;
        row.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }
}
