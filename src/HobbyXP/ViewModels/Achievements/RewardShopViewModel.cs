using System.Collections.ObjectModel;
using HobbyXP.Helpers;
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
    private HobbyModuleOption? _selectedModule;
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
        AvailableSections = new ObservableCollection<RewardShopSectionViewModel>();
        InventorySections = new ObservableCollection<RewardShopSectionViewModel>();
        _selectedModule = ModuleOptions[0];

        CreateRewardCommand = new AsyncRelayCommand(CreateRewardAsync, CanCreateReward);
        AssignModuleCommand = new AsyncRelayCommand(AssignModuleAsync, CanAssignModule);
        RedeemRewardCommand = new AsyncRelayCommand(RedeemRewardAsync, CanRedeemSelected);
        EquipRewardCommand = new AsyncRelayCommand(EquipRewardAsync, CanEquipSelected);
        UnequipRewardCommand = new AsyncRelayCommand(UnequipRewardAsync, CanUnequip);
        RefreshCreateValidation();
    }

    public IReadOnlyList<HobbyModuleOption> ModuleOptions => HobbyModuleOption.Catalog;

    public ObservableCollection<RewardShopSectionViewModel> AvailableSections { get; }

    public ObservableCollection<RewardShopSectionViewModel> InventorySections { get; }

    public HobbyModuleOption? SelectedModule
    {
        get => _selectedModule;
        set
        {
            if (SetProperty(ref _selectedModule, value))
            {
                RefreshCreateValidation();
                AssignModuleCommand.RaiseCanExecuteChanged();
            }
        }
    }

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
                AssignModuleCommand.RaiseCanExecuteChanged();
                SyncModuleFromSelection(value);
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
                AssignModuleCommand.RaiseCanExecuteChanged();
                if (value is not null)
                    SyncModuleFromSelection(value);
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

    public AsyncRelayCommand AssignModuleCommand { get; }

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
        var available = new List<RewardRowViewModel>();
        var inventory = new List<RewardRowViewModel>();
        foreach (var reward in rewards)
        {
            var row = new RewardRowViewModel(reward, _currentLevel, _equippedRewardId ?? 0);
            if (row.IsAvailable)
                available.Add(row);
            else
                inventory.Add(row);
        }

        ReplaceSections(AvailableSections, available);
        ReplaceSections(InventorySections, inventory);

        UnequipRewardCommand.RaiseCanExecuteChanged();
        AssignModuleCommand.RaiseCanExecuteChanged();
    }

    private ValidationResult ValidateCreateForm() =>
        FormValidation.FirstFailure(
            FormValidation.RequireValue(SelectedModule, "el módulo del premio"),
            FormValidation.RequireText(Name, "el nombre del premio"),
            FormValidation.RequirePositiveInt(CostInPoints, "El costo en XP", out _));

    private void RefreshCreateValidation() =>
        RefreshValidation(ValidateCreateForm(), CreateRewardCommand);

    private bool CanCreateReward() => ValidateCreateForm().IsValid;

    private bool CanRedeemSelected() => CanAffordSelected;

    private bool CanEquipSelected() => SelectedInventory is { IsRedeemed: true, IsEquipped: false };

    private bool CanUnequip() => _equippedRewardId is > 0;

    private bool CanAssignModule()
    {
        var selected = SelectedAvailable ?? SelectedInventory;
        return SelectedModule is not null
            && selected is not null
            && selected.SourceType != SelectedModule.Value;
    }

    private void SyncModuleFromSelection(RewardRowViewModel? row)
    {
        if (row?.SourceType is not { } source)
            return;

        var match = ModuleOptions.FirstOrDefault(option => option.Value == source);
        if (match is not null)
            SelectedModule = match;
    }

    private static void ReplaceSections(
        ObservableCollection<RewardShopSectionViewModel> target,
        IReadOnlyList<RewardRowViewModel> rows)
    {
        target.Clear();
        foreach (var section in RewardShopCatalog.Group(rows, row => row.SourceType))
            target.Add(new RewardShopSectionViewModel(section));
    }

    private async Task CreateRewardAsync()
    {
        if (!ValidateCreateForm().IsValid || SelectedModule is null)
        {
            RefreshCreateValidation();
            return;
        }

        var cost = int.Parse(CostInPoints);
        var module = SelectedModule.Value;
        await RunBusyAsync(async () =>
        {
            await _rewardService.CreateAsync(Name, cost, module, Description);
            Name = string.Empty;
            CostInPoints = "500";
            Description = null;
            ClearValidation();
            await LoadCoreAsync();
            StatusMessage = $"Premio creado en {HobbyProgressCatalog.GetDisplayName(module)}.";
        }, "Creando premio...");
    }

    private async Task AssignModuleAsync()
    {
        var selected = SelectedAvailable ?? SelectedInventory;
        if (selected is null || SelectedModule is null)
            return;

        var rewardId = selected.Id;
        var module = SelectedModule.Value;
        await RunBusyAsync(async () =>
        {
            await _rewardService.UpdateSourceTypeAsync(rewardId, module);
            await LoadCoreAsync();
            StatusMessage = $"Premio asignado a {HobbyProgressCatalog.GetDisplayName(module)}.";
        }, "Asignando módulo...");
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
