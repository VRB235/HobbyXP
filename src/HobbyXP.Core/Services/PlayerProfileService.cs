using System.IO;
using HobbyXP.Data;
using HobbyXP.Helpers;
using HobbyXP.Models.Core;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Internal;
using HobbyXP.Services.Messaging;
using HobbyXP.Services.Results;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Services;

public sealed class PlayerProfileService : IPlayerProfileService
{
    private readonly IDbContextFactory<HobbyXpDbContext> _dbContextFactory;
    private readonly IProfileRefreshMessenger _profileRefreshMessenger;

    public PlayerProfileService(
        IDbContextFactory<HobbyXpDbContext> dbContextFactory,
        IProfileRefreshMessenger profileRefreshMessenger)
    {
        _dbContextFactory = dbContextFactory;
        _profileRefreshMessenger = profileRefreshMessenger;
    }

    public async Task<PlayerProfile> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var profile = await GetProfileAsync(db, cancellationToken);
        return await SanitizeAvatarPathAsync(db, profile, cancellationToken);
    }

    public async Task<LevelProgressInfo> GetLevelProgressAsync(CancellationToken cancellationToken = default)
    {
        var profile = await GetProfileAsync(cancellationToken);
        return XpLevelCalculator.BuildProgress(profile);
    }

    public async Task<PlayerProfile> UpdateDisplayNameAsync(
        string displayName,
        CancellationToken cancellationToken = default)
    {
        var normalized = displayName.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            throw new ArgumentException("El nombre no puede estar vacío.", nameof(displayName));

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var profile = await GetProfileAsync(db, cancellationToken);
        profile.DisplayName = normalized;
        profile.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return profile;
    }

    public async Task<PlayerProfile> UpdateAvatarPathAsync(
        string? avatarPath,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var profile = await GetProfileAsync(db, cancellationToken);

        string? storedPath = null;
        if (!string.IsNullOrWhiteSpace(avatarPath))
        {
            storedPath = AvatarStorage.IsManagedPath(avatarPath)
                ? ToStoredPathIfNeeded(avatarPath)
                : AvatarStorage.SaveFromSource(avatarPath);
        }

        profile.AvatarPath = storedPath;
        profile.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return profile;
    }

    public async Task<PlayerProfile> UpdateBaseXpPerLevelAsync(
        int baseXpPerLevel,
        CancellationToken cancellationToken = default)
    {
        if (baseXpPerLevel <= 0)
            throw new ArgumentOutOfRangeException(nameof(baseXpPerLevel), "El XP base por nivel debe ser mayor que cero.");

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var profile = await GetProfileAsync(db, cancellationToken);

        profile.BaseXpPerLevel = baseXpPerLevel;
        XpLevelCalculator.RecalculateLevel(profile);
        profile.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        _profileRefreshMessenger.RequestRefresh();
        return profile;
    }

    private static string? ToStoredPathIfNeeded(string path)
    {
        if (!Path.IsPathRooted(path))
            return path;

        var databaseDirectory = DatabaseConstants.GetDatabaseDirectory();
        if (path.StartsWith(databaseDirectory, StringComparison.OrdinalIgnoreCase))
            return Path.GetRelativePath(databaseDirectory, path);

        return path;
    }

    private static async Task<PlayerProfile> GetProfileAsync(
        HobbyXpDbContext db,
        CancellationToken cancellationToken)
    {
        return await db.PlayerProfiles.FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No existe un perfil de jugador inicializado.");
    }

    private static async Task<PlayerProfile> SanitizeAvatarPathAsync(
        HobbyXpDbContext db,
        PlayerProfile profile,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(profile.AvatarPath))
            return profile;

        if (!AvatarStorage.Exists(profile.AvatarPath))
        {
            profile.AvatarPath = null;
            profile.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return profile;
        }

        if (AvatarStorage.IsManagedPath(profile.AvatarPath))
            return profile;

        var migratedPath = AvatarStorage.MigrateExternalIfNeeded(profile.AvatarPath);
        if (migratedPath is null || string.Equals(migratedPath, profile.AvatarPath, StringComparison.OrdinalIgnoreCase))
            return profile;

        profile.AvatarPath = migratedPath;
        profile.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return profile;
    }
}
