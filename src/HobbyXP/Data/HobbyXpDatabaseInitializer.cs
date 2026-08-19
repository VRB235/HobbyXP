using HobbyXP.Helpers;
using HobbyXP.Models.Core;
using HobbyXP.Models.Enums;
using HobbyXP.Services.Internal;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Data;

internal static class HobbyXpDatabaseInitializer
{
    public static async Task EnsurePlayerProfileAsync(
        HobbyXpDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        if (await dbContext.PlayerProfiles.AnyAsync(cancellationToken))
            return;

        dbContext.PlayerProfiles.Add(new PlayerProfile
        {
            CurrentLevel = 1,
            TotalXp = 0,
            SpendableXp = 0,
            SpendableLedgerInitialized = true,
            SpendableProgressBaselineApplied = true,
            HobbySpendableLedgerInitialized = true,
            BaseXpPerLevel = 1000
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Resincroniza <see cref="PlayerProfile.CurrentLevel"/> desde <see cref="PlayerProfile.TotalXp"/>
    /// con la escala geométrica vigente. No modifica el XP total. Idempotente.
    /// </summary>
    public static async Task EnsureGeometricLevelScaleAsync(
        HobbyXpDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var profiles = await dbContext.PlayerProfiles.ToListAsync(cancellationToken);
        var changed = false;

        foreach (var profile in profiles)
        {
            var previousLevel = profile.CurrentLevel;
            XpLevelCalculator.RecalculateLevel(profile);

            if (profile.CurrentLevel == previousLevel)
                continue;

            profile.UpdatedAt = DateTime.UtcNow;
            changed = true;
        }

        if (changed)
            await dbContext.SaveChangesAsync(cancellationToken);
    }

    public static async Task EnsureHobbyProgressRowsAsync(
        HobbyXpDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var profiles = await dbContext.PlayerProfiles
            .Include(p => p.HobbyProgresses)
            .ToListAsync(cancellationToken);

        var changed = false;
        foreach (var profile in profiles)
        {
            foreach (var source in HobbyProgressCatalog.TrackedHobbies)
            {
                if (profile.HobbyProgresses.Any(h => h.SourceType == source))
                    continue;

                profile.HobbyProgresses.Add(new HobbyProgress
                {
                    PlayerProfileId = profile.Id,
                    SourceType = source,
                    CurrentLevel = 1,
                    TotalXp = 0,
                    SpendableXp = 0
                });
                changed = true;
            }
        }

        if (changed)
            await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Migra XP histórico de actividades a pools de hobby y reconstruye el global meta.
    /// Solo corre si el ledger de saldo aún no está activo, no hay XP en hobbies y existen txs de actividad.
    /// Tras el prestige (<see cref="PlayerProfile.SpendableLedgerInitialized"/>) no debe reconstruir historial.
    /// </summary>
    public static async Task EnsureHobbyXpBackfillAsync(
        HobbyXpDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        await EnsureHobbyProgressRowsAsync(dbContext, cancellationToken);

        var ledgerReady = await dbContext.PlayerProfiles
            .AnyAsync(p => p.SpendableLedgerInitialized, cancellationToken);
        if (ledgerReady)
            return;

        var hasHobbyXp = await dbContext.HobbyProgresses.AnyAsync(h => h.TotalXp > 0, cancellationToken);
        if (hasHobbyXp)
            return;

        var activityTransactions = await dbContext.XpTransactions
            .Where(t => t.Amount > 0 &&
                        t.ActionType != AchievementActionType.RewardRedeemed &&
                        t.ActionType != AchievementActionType.HobbyLevelUp)
            .ToListAsync(cancellationToken);

        if (activityTransactions.Count == 0)
            return;

        var profiles = await dbContext.PlayerProfiles
            .Include(p => p.HobbyProgresses)
            .ToListAsync(cancellationToken);

        foreach (var profile in profiles)
        {
            foreach (var hobby in profile.HobbyProgresses)
            {
                hobby.TotalXp = 0;
                hobby.CurrentLevel = 1;
            }

            foreach (var tx in activityTransactions.Where(t => t.PlayerProfileId == profile.Id))
            {
                var hobbySource = tx.SourceType ?? HobbyProgressCatalog.MapActionToHobby(tx.ActionType);
                if (hobbySource is null || !HobbyProgressCatalog.IsTrackedHobby(hobbySource.Value))
                    continue;

                var hobby = profile.HobbyProgresses.First(h => h.SourceType == hobbySource.Value);
                hobby.TotalXp += tx.Amount;
                tx.SourceType = hobbySource;
                tx.IsGlobal = false;
            }

            foreach (var hobby in profile.HobbyProgresses)
                XpLevelCalculator.RecalculateLevel(hobby, profile.BaseXpPerLevel);

            var redeemed = await dbContext.XpTransactions
                .Where(t => t.PlayerProfileId == profile.Id &&
                            t.ActionType == AchievementActionType.RewardRedeemed &&
                            t.Amount < 0)
                .SumAsync(t => (int?)t.Amount, cancellationToken) ?? 0;

            var metaLevels = profile.HobbyProgresses.Sum(h => Math.Max(0, h.CurrentLevel - 1));
            var rebuiltGlobal = Math.Max(0, (metaLevels * profile.BaseXpPerLevel) + redeemed);
            profile.TotalXp = rebuiltGlobal;
            XpLevelCalculator.RecalculateLevel(profile);
            profile.UpdatedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// One-shot: mueve XP de progresión (hobbies + global) a <see cref="PlayerProfile.SpendableXp"/>
    /// y reinicia niveles a 1. Idempotente vía <see cref="PlayerProfile.SpendableLedgerInitialized"/>.
    /// Si el ledger ya estaba activo pero el baseline no (p. ej. backfill histórico rellenó de nuevo),
    /// vuelve a poner progresión en 1/0 sin tocar el saldo.
    /// Debe ejecutarse después del backfill de hobbies.
    /// </summary>
    public static async Task EnsureSpendableLedgerAsync(
        HobbyXpDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        await EnsureHobbyProgressRowsAsync(dbContext, cancellationToken);

        var profiles = await dbContext.PlayerProfiles
            .Include(p => p.HobbyProgresses)
            .Where(p => !p.SpendableLedgerInitialized || !p.SpendableProgressBaselineApplied)
            .ToListAsync(cancellationToken);

        if (profiles.Count == 0)
            return;

        foreach (var profile in profiles)
        {
            if (!profile.SpendableLedgerInitialized)
            {
                var totalSpendable = profile.HobbyProgresses.Sum(h => h.TotalXp) + profile.TotalXp;

                foreach (var hobby in profile.HobbyProgresses)
                {
                    var metaFromHobby = Math.Max(0, hobby.CurrentLevel - 1) * profile.BaseXpPerLevel;
                    hobby.SpendableXp = hobby.TotalXp + metaFromHobby;
                }

                var assigned = profile.HobbyProgresses.Sum(h => h.SpendableXp);
                var remainder = totalSpendable - assigned;
                if (remainder > 0)
                {
                    var target = profile.HobbyProgresses
                        .OrderByDescending(h => h.SpendableXp)
                        .ThenBy(h => h.SourceType)
                        .First();
                    target.SpendableXp += remainder;
                }

                profile.SpendableXp = totalSpendable;
                profile.SpendableLedgerInitialized = true;
            }

            ResetProgressionToBaseline(profile);
            profile.SpendableProgressBaselineApplied = true;
            profile.UpdatedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void ResetProgressionToBaseline(PlayerProfile profile)
    {
        foreach (var hobby in profile.HobbyProgresses)
        {
            hobby.TotalXp = 0;
            hobby.CurrentLevel = 1;
        }

        profile.TotalXp = 0;
        profile.CurrentLevel = 1;
    }

    /// <summary>
    /// One-shot: reconstruye <see cref="HobbyProgress.SpendableXp"/> desde el ledger de transacciones
    /// (o reparte el saldo global si no hay historial). Idempotente vía
    /// <see cref="PlayerProfile.HobbySpendableLedgerInitialized"/>.
    /// </summary>
    public static async Task EnsureHobbySpendableLedgerAsync(
        HobbyXpDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        await EnsureHobbyProgressRowsAsync(dbContext, cancellationToken);

        var profiles = await dbContext.PlayerProfiles
            .Include(p => p.HobbyProgresses)
            .Where(p => !p.HobbySpendableLedgerInitialized)
            .ToListAsync(cancellationToken);

        if (profiles.Count == 0)
            return;

        var medalSources = await BuildMedalSourceLookupAsync(dbContext, cancellationToken);

        foreach (var profile in profiles)
        {
            foreach (var hobby in profile.HobbyProgresses)
                hobby.SpendableXp = 0;

            var transactions = await dbContext.XpTransactions
                .Where(t => t.PlayerProfileId == profile.Id)
                .ToListAsync(cancellationToken);

            if (transactions.Count > 0)
            {
                foreach (var tx in transactions)
                {
                    var hobbySource = ResolveHobbyForTransaction(tx, medalSources);
                    if (hobbySource is null || !HobbyProgressCatalog.IsTrackedHobby(hobbySource.Value))
                        continue;

                    var hobby = profile.HobbyProgresses.First(h => h.SourceType == hobbySource.Value);
                    hobby.SpendableXp = Math.Max(0, hobby.SpendableXp + tx.Amount);
                }
            }
            else if (profile.SpendableXp > 0)
            {
                // Perfil legacy sin txs: conserva el saldo en el primer hobby con progreso o Running.
                var target = profile.HobbyProgresses
                                 .OrderByDescending(h => h.TotalXp)
                                 .FirstOrDefault(h => h.TotalXp > 0)
                             ?? profile.HobbyProgresses.First(h => h.SourceType == MilestoneSourceType.Running);
                target.SpendableXp = profile.SpendableXp;
            }

            profile.SpendableXp = profile.HobbyProgresses.Sum(h => h.SpendableXp);
            profile.HobbySpendableLedgerInitialized = true;
            profile.UpdatedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static MilestoneSourceType? ResolveHobbyForTransaction(
        XpTransaction tx,
        IReadOnlyDictionary<int, MilestoneSourceType> medalSources)
    {
        if (tx.SourceType is { } source && HobbyProgressCatalog.IsTrackedHobby(source))
            return source;

        if (tx.ActionType == AchievementActionType.MedalPrivilegeBonus &&
            tx.SourceEntityId is int medalId &&
            medalSources.TryGetValue(medalId, out var medalSource))
            return medalSource;

        return null;
    }

    private static async Task<Dictionary<int, MilestoneSourceType>> BuildMedalSourceLookupAsync(
        HobbyXpDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var definitions = await dbContext.MedalDefinitions
            .AsNoTracking()
            .Select(d => new { d.Id, d.Code })
            .ToListAsync(cancellationToken);

        var lookup = new Dictionary<int, MilestoneSourceType>();
        foreach (var definition in definitions)
        {
            var entry = MedalCatalog.Entries.FirstOrDefault(e => e.Code == definition.Code);
            if (entry is null)
                continue;

            var source = MedalTrackMap.SourceFor(entry.Track);
            if (source is not null)
                lookup[definition.Id] = source.Value;
        }

        return lookup;
    }
}
