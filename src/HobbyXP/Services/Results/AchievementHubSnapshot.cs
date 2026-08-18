using HobbyXP.Helpers;
using HobbyXP.Models.Achievements;
using HobbyXP.Models.Enums;

namespace HobbyXP.Services.Results;

public sealed record NextMedalProgress(
    MedalCode Code,
    string Name,
    string TrackLabel,
    int CurrentCount,
    int Threshold,
    string? IconPath)
{
    public int Remaining => Math.Max(0, Threshold - CurrentCount);

    public double Percent => Threshold <= 0
        ? 0
        : Math.Clamp(CurrentCount * 100d / Threshold, 0d, 100d);

    public string ProgressText => $"{CurrentCount:N0} / {Threshold:N0} · {TrackLabel}";

    public string BannerText =>
        $"Siguiente logro: {Name} ({CurrentCount:N0}/{Threshold:N0} {TrackLabel})";
}

public sealed record AchievementHubSnapshot(
    MedalShowcaseItem? LatestEarned,
    NextMedalProgress? ClosestNext,
    Reward? FeaturedReward,
    int FeaturedEffectiveCost,
    bool CanAffordFeatured,
    string? HonorTitle,
    string? EquippedRewardName,
    DateTime? ImmunityUntilUtc)
{
    public bool IsImmune => MedalPrivilegeRules.IsActive(ImmunityUntilUtc, DateTime.UtcNow);

    public bool HasLatestEarned => LatestEarned is not null;

    public bool HasClosestNext => ClosestNext is not null;

    public bool HasFeaturedReward => FeaturedReward is not null;

    public string ImmunityText =>
        IsImmune && ImmunityUntilUtc is DateTime until
            ? $"Inmunidad de disciplina hasta {until.ToLocalTime():dd/MM/yyyy HH:mm}"
            : string.Empty;

    public string FeaturedCostText =>
        FeaturedReward is null
            ? string.Empty
            : $"{FeaturedEffectiveCost:N0} XP (base {FeaturedReward.CostInPoints:N0} × nivel)";
}
