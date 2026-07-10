using HobbyXP.Models.Achievements;
using HobbyXP.Models.Enums;
using HobbyXP.Services.Results;

namespace HobbyXP.Services.Abstractions;

public interface IAchievementEngineService
{
    Task<AchievementRule?> GetActiveRuleAsync(
        AchievementActionType actionType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AchievementRule>> GetAllRulesAsync(CancellationToken cancellationToken = default);

    Task<AchievementRule> UpdateRuleAsync(
        AchievementRule rule,
        CancellationToken cancellationToken = default);

    Task<bool> IsMedalEarnedAsync(MedalCode code, CancellationToken cancellationToken = default);

    Task<AchievementEvent?> TryAwardMedalAsync(
        MedalCode code,
        MilestoneSourceType sourceType,
        string? sourceEntityType = null,
        int? sourceEntityId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AchievementEvent>> TryAwardMilestonesForTrackAsync(
        MedalMilestoneTrack track,
        MilestoneSourceType sourceType,
        string? sourceEntityType = null,
        int? sourceEntityId = null,
        CancellationToken cancellationToken = default);
}
