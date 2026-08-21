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

public sealed record NextRewardProgress(
    int RewardId,
    string Name,
    MilestoneSourceType SourceType,
    int EffectiveCost,
    int ModuleBalance,
    string? ImagePath,
    string? PurchaseUrl,
    decimal? Price)
{
    public int RemainingXp => Math.Max(0, EffectiveCost - ModuleBalance);

    public bool CanAfford => ModuleBalance >= EffectiveCost;

    public double Percent => EffectiveCost <= 0
        ? 0
        : Math.Clamp(ModuleBalance * 100d / EffectiveCost, 0d, 100d);

    public string? ResolvedImagePath => RewardPhotoStorage.ResolveAbsolutePath(ImagePath);

    public bool HasImage => !string.IsNullOrWhiteSpace(ResolvedImagePath);

    public string PriceLabel => Price is null
        ? string.Empty
        : $"Precio: {Price.Value:N2}";

    public string BannerText => CanAfford
        ? $"¡Puede canjear «{Name}» por {EffectiveCost:N0} XP!"
        : $"Te faltan {RemainingXp:N0} XP para canjear «{Name}» ({ModuleBalance:N0}/{EffectiveCost:N0}).";
}

public sealed record AchievementHubSnapshot(
    MedalShowcaseItem? LatestEarned,
    NextMedalProgress? ClosestNext,
    Reward? FeaturedReward,
    int FeaturedEffectiveCost,
    int FeaturedModuleBalance,
    bool CanAffordFeatured,
    string? HonorTitle,
    string? EquippedRewardName,
    DateTime? ImmunityUntilUtc)
{
    public bool IsImmune => MedalPrivilegeRules.IsActive(ImmunityUntilUtc, DateTime.UtcNow);

    public bool HasLatestEarned => LatestEarned is not null;

    public bool HasClosestNext => ClosestNext is not null;

    public bool HasFeaturedReward => FeaturedReward is not null;

    public int FeaturedRemainingXp => Math.Max(0, FeaturedEffectiveCost - FeaturedModuleBalance);

    public string ImmunityText =>
        IsImmune && ImmunityUntilUtc is DateTime until
            ? $"Inmunidad de disciplina hasta {until.ToLocalTime():dd/MM/yyyy HH:mm}"
            : string.Empty;

    public string FeaturedCostText =>
        FeaturedReward is null
            ? string.Empty
            : $"{FeaturedEffectiveCost:N0} XP (base {FeaturedReward.CostInPoints:N0} × nivel)";

    public string FeaturedModuleName =>
        FeaturedReward is null
            ? string.Empty
            : RewardShopCatalog.GetModuleDisplayName(FeaturedReward.SourceType);

    public string? FeaturedImagePath =>
        FeaturedReward is null
            ? null
            : RewardPhotoStorage.ResolveAbsolutePath(FeaturedReward.ImagePath);

    public bool HasFeaturedImage => !string.IsNullOrWhiteSpace(FeaturedImagePath);

    public string FeaturedMotivationText =>
        FeaturedReward is null
            ? string.Empty
            : CanAffordFeatured
                ? "¡Puede canjearlo!"
                : $"Te faltan {FeaturedRemainingXp:N0} XP para canjearlo.";
}
