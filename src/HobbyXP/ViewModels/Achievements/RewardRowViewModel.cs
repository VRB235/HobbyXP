using System.Globalization;
using HobbyXP.Helpers;
using HobbyXP.Models.Achievements;
using HobbyXP.Models.Enums;
using HobbyXP.ViewModels.Common;

namespace HobbyXP.ViewModels.Achievements;

public sealed class RewardRowViewModel : ViewModelBase
{
    private bool _isEquipped;

    public RewardRowViewModel(
        Reward reward,
        int currentLevel,
        int equippedRewardId,
        int? moduleBalance = null)
    {
        Reward = reward;
        CurrentLevel = currentLevel;
        _isEquipped = equippedRewardId == reward.Id;
        HasModule = reward.SourceType is not null;
        ModuleBalance = moduleBalance ?? 0;
    }

    public Reward Reward { get; }

    public int CurrentLevel { get; }

    public bool HasModule { get; }

    public int ModuleBalance { get; }

    public bool CanRedeem => IsAvailable && HasModule && ModuleBalance >= EffectiveCost;

    public int MissingXp =>
        IsAvailable && HasModule && !CanRedeem
            ? Math.Max(0, EffectiveCost - ModuleBalance)
            : 0;

    public int Id => Reward.Id;

    public string Name => Reward.Name;

    public string? Description => Reward.Description;

    public int BaseCost => Reward.CostInPoints;

    public int EffectiveCost => RewardCostCalculator.GetEffectiveCost(BaseCost, CurrentLevel);

    public decimal? Price => Reward.Price;

    public string? PurchaseUrl => Reward.PurchaseUrl;

    public string? ImagePath => Reward.ImagePath;

    public string? ImageDisplayPath => RewardPhotoStorage.ResolveAbsolutePath(ImagePath);

    public bool HasImage => !string.IsNullOrWhiteSpace(ImageDisplayPath);

    public bool HasPurchaseUrl => !string.IsNullOrWhiteSpace(PurchaseUrl);

    public string PriceLabel => Price is null
        ? "—"
        : Price.Value.ToString("N2", CultureInfo.CurrentCulture);

    public RewardStatus Status => Reward.Status;

    public string StatusLabel => Reward.StatusLabel;

    public DateTime? RedeemedAt => Reward.RedeemedAt;

    public int? RedeemedCostInPoints => Reward.RedeemedCostInPoints;

    public bool IsAvailable => Status == RewardStatus.Available;

    public bool IsRedeemed => Status == RewardStatus.Redeemed;

    public bool IsEquipped
    {
        get => _isEquipped;
        set
        {
            if (SetProperty(ref _isEquipped, value))
                OnPropertyChanged(nameof(EquipButtonLabel));
        }
    }

    public string CostLabel => IsRedeemed
        ? $"Pagó {RedeemedCostInPoints ?? BaseCost:N0} XP"
        : $"{EffectiveCost:N0} XP (base {BaseCost:N0} × niv. {CurrentLevel})";

    public string EquipButtonLabel => IsEquipped ? "Equipado" : "Equipar";

    public string RedeemStatusLabel
    {
        get
        {
            if (!IsAvailable)
                return string.Empty;

            if (!HasModule)
                return "Asigne módulo";

            if (CanRedeem)
                return "Listo para canjear";

            return $"Faltan {MissingXp:N0} XP";
        }
    }

    public string RedeemStatusToolTip
    {
        get
        {
            if (!IsAvailable)
                return string.Empty;

            if (!HasModule)
                return "Asigne un módulo al premio antes de canjearlo.";

            return
                $"{ModuleLabel}: {ModuleBalance:N0} XP disponibles · Costo {EffectiveCost:N0} XP · {RedeemStatusLabel}";
        }
    }

    public MilestoneSourceType? SourceType => Reward.SourceType;

    public string ModuleLabel => RewardShopCatalog.GetModuleDisplayName(SourceType);
}
