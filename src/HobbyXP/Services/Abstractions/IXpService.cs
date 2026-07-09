using HobbyXP.Models.Core;
using HobbyXP.Models.Enums;
using HobbyXP.Services.Results;

namespace HobbyXP.Services.Abstractions;

public interface IXpService
{
    Task<LevelProgressInfo> GetLevelProgressAsync(CancellationToken cancellationToken = default);

    Task<int> CalculatePointsAsync(
        AchievementActionType actionType,
        decimal units,
        CancellationToken cancellationToken = default);

    Task<XpAwardOutcome> AwardXpAsync(
        AchievementActionType actionType,
        decimal units,
        string description,
        MilestoneSourceType milestoneSource,
        string? sourceEntityType = null,
        int? sourceEntityId = null,
        string? milestoneTitle = null,
        CancellationToken cancellationToken = default);

    Task<XpAwardOutcome> AwardFlatBonusAsync(
        AchievementActionType actionType,
        int bonusPoints,
        string description,
        MilestoneSourceType milestoneSource,
        string? sourceEntityType = null,
        int? sourceEntityId = null,
        string? milestoneTitle = null,
        CancellationToken cancellationToken = default);

    Task<bool> TryDeductXpAsync(
        int amount,
        string description,
        CancellationToken cancellationToken = default);

    Task<int> RevokeXpForSourceAsync(
        MilestoneSourceType milestoneSource,
        string sourceEntityType,
        int sourceEntityId,
        string description,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DailyXpPoint>> GetDailyXpForLastDaysAsync(
        int days,
        CancellationToken cancellationToken = default);
}

public sealed record XpAwardOutcome(
    int AmountAwarded,
    int NewTotalXp,
    int? NewLevel,
    bool LeveledUp,
    Milestone? Milestone);
