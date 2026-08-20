using HobbyXP.Models.Core;
using HobbyXP.Models.Enums;
using HobbyXP.Services.Results;

namespace HobbyXP.Services.Abstractions;

public interface IXpService
{
    Task<LevelProgressInfo> GetLevelProgressAsync(CancellationToken cancellationToken = default);

    Task<LevelProgressInfo> GetHobbyProgressAsync(
        MilestoneSourceType sourceType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HobbyProgressInfo>> GetAllHobbyProgressAsync(
        CancellationToken cancellationToken = default);

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
        MilestoneSourceType hobbySource,
        string description,
        CancellationToken cancellationToken = default);

    Task<int> GetHobbySpendableXpAsync(
        MilestoneSourceType sourceType,
        CancellationToken cancellationToken = default);

    Task<int> RevokeXpForSourceAsync(
        MilestoneSourceType milestoneSource,
        string sourceEntityType,
        int sourceEntityId,
        string description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Quita el XP justo necesario para bajar un nivel del hobby (y el meta global asociado).
    /// </summary>
    Task<HobbyLevelPenaltyOutcome> ApplyHobbyLevelDownPenaltyAsync(
        MilestoneSourceType milestoneSource,
        string description,
        int? sourceEntityId = null,
        string? sourceEntityType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Devuelve XP de hobby/global previamente revocado por castigo de disciplina.
    /// </summary>
    Task RestoreHobbyLevelPenaltyAsync(
        MilestoneSourceType milestoneSource,
        int hobbyXpToRestore,
        int globalXpToRestore,
        string description,
        int? sourceEntityId = null,
        string? sourceEntityType = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DailyXpPoint>> GetDailyXpForLastDaysAsync(
        int days,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Resultado de otorgar XP de actividad (pool del hobby + posible bonus meta al global).
/// <see cref="NewTotalXp"/> es el total del hobby tras el award.
/// </summary>
public sealed record XpAwardOutcome(
    int AmountAwarded,
    int NewTotalXp,
    int? NewLevel,
    bool LeveledUp,
    Milestone? Milestone,
    int GlobalBonusAwarded = 0,
    int? NewGlobalLevel = null,
    bool GlobalLeveledUp = false);

public sealed record HobbyProgressInfo(
    MilestoneSourceType SourceType,
    string DisplayName,
    LevelProgressInfo Progress,
    string LevelTitle);
