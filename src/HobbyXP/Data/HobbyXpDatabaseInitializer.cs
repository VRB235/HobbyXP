using HobbyXP.Models.Core;
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
            BaseXpPerLevel = 1000
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
