using HobbyXP.Helpers;
using HobbyXP.Models.Achievements;
using HobbyXP.Models.Enums;

namespace HobbyXP.Services.Results;

public sealed record NextMedalProgress(
    int MedalDefinitionId,
    string Name,
    string HobbyLabel,
    int CurrentXp,
    int XpThreshold,
    string? IconPath)
{
    public int Remaining => Math.Max(0, XpThreshold - CurrentXp);

    public double Percent => XpThreshold <= 0
        ? 0
        : Math.Clamp(CurrentXp * 100d / XpThreshold, 0d, 100d);

    public string ProgressText => $"{CurrentXp:N0} / {XpThreshold:N0} XP · {HobbyLabel}";

    public string BannerText =>
        $"Siguiente medalla: {Name} ({CurrentXp:N0}/{XpThreshold:N0} XP · {HobbyLabel})";
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
