using HobbyXP.Data;
using HobbyXP.Helpers;
using HobbyXP.Models.Core;
using HobbyXP.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Tests.Helpers;

internal static class TestHobbyProgress
{
    public static async Task SetTotalXpAsync(
        HobbyXpDbContext db,
        MilestoneSourceType sourceType,
        int totalXp)
    {
        var profile = await db.PlayerProfiles.Include(p => p.HobbyProgresses).FirstAsync();
        var hobby = profile.HobbyProgresses.FirstOrDefault(h => h.SourceType == sourceType);
        if (hobby is null)
        {
            hobby = new HobbyProgress
            {
                PlayerProfileId = profile.Id,
                SourceType = sourceType,
                CurrentLevel = 1,
                TotalXp = totalXp,
                SpendableXp = 0
            };
            profile.HobbyProgresses.Add(hobby);
        }
        else
        {
            hobby.TotalXp = totalXp;
        }

        await db.SaveChangesAsync();
    }

    public static async Task EnsureTrackedHobbiesAsync(HobbyXpDbContext db)
    {
        var profile = await db.PlayerProfiles.Include(p => p.HobbyProgresses).FirstAsync();
        foreach (var hobby in HobbyProgressCatalog.TrackedHobbies)
        {
            if (profile.HobbyProgresses.Any(h => h.SourceType == hobby))
                continue;

            profile.HobbyProgresses.Add(new HobbyProgress
            {
                PlayerProfileId = profile.Id,
                SourceType = hobby,
                CurrentLevel = 1,
                TotalXp = 0,
                SpendableXp = 0
            });
        }

        await db.SaveChangesAsync();
    }
}
