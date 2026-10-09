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

    Task<bool> IsMedalEarnedAsync(int medalDefinitionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Otorga medallas del hobby cuyo umbral de XP ya fue alcanzado y aún no están ganadas.
    /// </summary>
    Task<IReadOnlyList<AchievementEvent>> TryAwardHobbyXpMedalsAsync(
        MilestoneSourceType sourceType,
        CancellationToken cancellationToken = default);
}
