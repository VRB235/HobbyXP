using HobbyXP.Data;
using HobbyXP.Models.Core;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Internal;
using HobbyXP.Services.Results;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Services;

public sealed class PlayerProfileService : IPlayerProfileService
{
    private readonly IDbContextFactory<HobbyXpDbContext> _dbContextFactory;

    public PlayerProfileService(IDbContextFactory<HobbyXpDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<PlayerProfile> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await GetProfileAsync(db, cancellationToken);
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
        profile.AvatarPath = avatarPath;
        profile.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return profile;
    }

    private static async Task<PlayerProfile> GetProfileAsync(
        HobbyXpDbContext db,
        CancellationToken cancellationToken)
    {
        return await db.PlayerProfiles.FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No existe un perfil de jugador inicializado.");
    }
}
