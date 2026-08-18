using HobbyXP.Helpers;
using HobbyXP.Models.Achievements;
using HobbyXP.Models.Enums;
using HobbyXP.ViewModels.Common;

namespace HobbyXP.ViewModels.Achievements;

public sealed class RewardRowViewModel : ViewModelBase
{
    private bool _isEquipped;

    public RewardRowViewModel(Reward reward, int currentLevel, int equippedRewardId)
    {
        Reward = reward;
        CurrentLevel = currentLevel;
        _isEquipped = equippedRewardId == reward.Id;
    }

    public Reward Reward { get; }

    public int CurrentLevel { get; }

    public int Id => Reward.Id;

    public string Name => Reward.Name;

    public string? Description => Reward.Description;

    public int BaseCost => Reward.CostInPoints;

    public int EffectiveCost => RewardCostCalculator.GetEffectiveCost(BaseCost, CurrentLevel);

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
}
