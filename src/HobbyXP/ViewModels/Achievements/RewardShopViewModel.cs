using System.Collections.ObjectModel;
using HobbyXP.Helpers;
using HobbyXP.Models.Enums;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Messaging;
using HobbyXP.ViewModels.Common;
using HobbyXP.ViewModels.Messaging;

namespace HobbyXP.ViewModels.Achievements;

public sealed class RewardShopViewModel : AchievementAwareViewModel
{
    private readonly IRewardService _rewardService;
    private readonly IPlayerProfileService _playerProfileService;
    private readonly IProfileRefreshMessenger _profileRefreshMessenger;
    private string _name = string.Empty;
    private string _costInPoints = "500";
    private string? _description;
    private int _availableXp;
    private int _currentLevel = 1;
    private int? _equippedRewardId;
    private RewardRowViewModel? _selectedAvailable;
    private RewardRowViewModel? _selectedInventory;

    public RewardShopViewModel(
        IRewardService rewardService,
        IPlayerProfileService playerProfileService,
        IProfileRefreshMessenger profileRefreshMessenger,
        IAchievementMessenger achievementMessenger)
        : base(achievementMessenger)
    {
        _rewardService = rewardService;
        _playerProfileService = playerProfileService;
        _profileRefreshMessenger = profileRefreshMessenger;
        AvailableRewards = new ObservableCollection<RewardRowViewModel>();
        InventoryRewards = new ObservableCollection<RewardRowViewModel>();

        CreateRewardCommand = new AsyncRelayCommand(CreateRewardAsync, CanCreateReward);
        RedeemRewardCommand = new AsyncRelayCommand(RedeemRewardAsync, CanRedeemSelected);
        EquipRewardCommand = new AsyncRelayCommand(EquipRewardAsync, CanEquipSelected);
        UnequipRewardCommand = new AsyncRelayCommand(UnequipRewardAsync, CanUnequip);
        RefreshCreateValidation();
    }

    public ObservableCollection<RewardRowViewModel> AvailableRewards { get; }

    public ObservableCollection<RewardRowViewModel> InventoryRewards { get; }

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
                RefreshCreateValidation();
        }
    }

    public string CostInPoints
    {
        get => _costInPoints;
        set
        {
            if (SetProperty(ref _costInPoints, value))
                RefreshCreateValidation();
        }
    }

    public string? Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public int AvailableXp
    {
        get => _availableXp;
        private set
        {
            if (SetProperty(ref _availableXp, value))
            {
                OnPropertyChanged(nameof(CanAffordSelected));
                OnPropertyChanged(nameof(BalanceText));
                RedeemRewardCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string BalanceText =>
        $"Saldo canjeable: {AvailableXp:N0} XP · nivel {_currentLevel} (el costo = base × nivel)";

    public RewardRowViewModel? SelectedAvailable
    {
        get => _selectedAvailable;
        set
        {
            if (SetProperty(ref _selectedAvailable, value))
            {
                OnPropertyChanged(nameof(CanAffordSelected));
                OnPropertyChanged(nameof(SelectedRewardRedeemHint));
                RedeemRewardCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public RewardRowViewModel? SelectedInventory
    {
        get => _selectedInventory;
        set
        {
            if (SetProperty(ref _selectedInventory, value))
            {
                EquipRewardCommand.RaiseCanExecuteChanged();
                UnequipRewardCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool CanAffordSelected =>
        SelectedAvailable is { IsAvailable: true } selected &&
        AvailableXp >= selected.EffectiveCost;

    public string SelectedRewardRedeemHint
    {
        get
        {
            if (SelectedAvailable is null)
                return "Seleccione un premio disponible.";

            if (CanAffordSelected)
                return $"Puede canjearlo por {SelectedAvailable.EffectiveCost:N0} XP.";

            var missing = SelectedAvailable.EffectiveCost - AvailableXp;
            return $"Necesita {SelectedAvailable.EffectiveCost:N0} XP (faltan {missing:N0}).";
        }
    }

    public AsyncRelayCommand CreateRewardCommand { get; }

    public AsyncRelayCommand RedeemRewardCommand { get; }

    public AsyncRelayCommand EquipRewardCommand { get; }

    public AsyncRelayCommand UnequipRewardCommand { get; }

    protected override async Task LoadCoreAsync()
    {
        var profile = await _playerProfileService.GetProfileAsync();
        AvailableXp = profile.SpendableXp;
        _currentLevel = profile.CurrentLevel;
        _equippedRewardId = profile.EquippedRewardId;
        OnPropertyChanged(nameof(BalanceText));

        var rewards = await _rewardService.GetAllAsync();
        AvailableRewards.Clear();
        InventoryRewards.Clear();
        foreach (var reward in rewards)
        {
            var row = new RewardRowViewModel(reward, _currentLevel, _equippedRewardId ?? 0);
            if (row.IsAvailable)
                AvailableRewards.Add(row);
            else
                InventoryRewards.Add(row);
        }

        UnequipRewardCommand.RaiseCanExecuteChanged();
    }

    private ValidationResult ValidateCreateForm() =>
        FormValidation.FirstFailure(
            FormValidation.RequireText(Name, "el nombre del premio"),
            FormValidation.RequirePositiveInt(CostInPoints, "El costo en XP", out _));

    private void RefreshCreateValidation() =>
        RefreshValidation(ValidateCreateForm(), CreateRewardCommand);

    private bool CanCreateReward() => ValidateCreateForm().IsValid;

    private bool CanRedeemSelected() => CanAffordSelected;

    private bool CanEquipSelected() => SelectedInventory is { IsRedeemed: true, IsEquipped: false };

    private bool CanUnequip() => _equippedRewardId is > 0;

    private async Task CreateRewardAsync()
    {
        if (!ValidateCreateForm().IsValid)
        {
            RefreshCreateValidation();
            return;
        }

        var cost = int.Parse(CostInPoints);
        await RunBusyAsync(async () =>
        {
            await _rewardService.CreateAsync(Name, cost, Description);
            Name = string.Empty;
            CostInPoints = "500";
            Description = null;
            ClearValidation();
            await LoadCoreAsync();
            StatusMessage = "Premio creado en el catálogo.";
        }, "Creando premio...");
    }

    private async Task RedeemRewardAsync()
    {
        if (SelectedAvailable is null || !CanAffordSelected)
            return;

        var rewardId = SelectedAvailable.Id;
        await RunBusyAsync(async () =>
        {
            var result = await _rewardService.RedeemAsync(rewardId);
            PublishAchievements(result.Events);
            await LoadCoreAsync();
            _profileRefreshMessenger.RequestRefresh();
            StatusMessage = $"Premio canjeado: {result.Value.Name}. Ya está en el inventario.";
        }, "Canjeando premio...");
    }

    private async Task EquipRewardAsync()
    {
        if (SelectedInventory is null)
            return;

        var rewardId = SelectedInventory.Id;
        await RunBusyAsync(async () =>
        {
            await _rewardService.EquipAsync(rewardId);
            await LoadCoreAsync();
            _profileRefreshMessenger.RequestRefresh();
            StatusMessage = "Reliquia equipada. Se muestra en el perfil.";
        }, "Equipando premio...");
    }

    private async Task UnequipRewardAsync()
    {
        await RunBusyAsync(async () =>
        {
            await _rewardService.UnequipAsync();
            await LoadCoreAsync();
            _profileRefreshMessenger.RequestRefresh();
            StatusMessage = "Reliquia desequipada.";
        }, "Quitando reliquia...");
    }
}
