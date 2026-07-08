using System.Collections.ObjectModel;
using HobbyXP.Models.Achievements;
using HobbyXP.Models.Enums;
using HobbyXP.Services.Abstractions;
using HobbyXP.ViewModels.Common;
using HobbyXP.ViewModels.Messaging;

namespace HobbyXP.ViewModels.Achievements;

public sealed class RewardShopViewModel : AchievementAwareViewModel
{
    private readonly IRewardService _rewardService;
    private readonly IPlayerProfileService _playerProfileService;
    private string _name = string.Empty;
    private string _costInPoints = "500";
    private string? _description;
    private int _availableXp;
    private Reward? _selectedReward;

    public RewardShopViewModel(
        IRewardService rewardService,
        IPlayerProfileService playerProfileService,
        IAchievementMessenger achievementMessenger)
        : base(achievementMessenger)
    {
        _rewardService = rewardService;
        _playerProfileService = playerProfileService;
        Rewards = new ObservableCollection<Reward>();

        CreateRewardCommand = new AsyncRelayCommand(CreateRewardAsync, CanCreateReward);
        RedeemRewardCommand = new AsyncRelayCommand(RedeemRewardAsync, CanRedeemSelected);
    }

    public ObservableCollection<Reward> Rewards { get; }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string CostInPoints
    {
        get => _costInPoints;
        set
        {
            if (SetProperty(ref _costInPoints, value))
                OnPropertyChanged(nameof(CanAffordSelected));
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
            }
        }
    }

    public string BalanceText => $"Saldo disponible: {AvailableXp:N0} XP";

    public Reward? SelectedReward
    {
        get => _selectedReward;
        set
        {
            if (SetProperty(ref _selectedReward, value))
            {
                OnPropertyChanged(nameof(CanAffordSelected));
                OnPropertyChanged(nameof(SelectedRewardRedeemHint));
            }
        }
    }

    public bool CanAffordSelected =>
        SelectedReward is { Status: RewardStatus.Available } &&
        AvailableXp >= SelectedReward.CostInPoints;

    public string SelectedRewardRedeemHint =>
        SelectedReward is null
            ? "Seleccione un premio."
            : CanAffordSelected
                ? "¡Puedes canjear este premio!"
                : $"Necesitas {SelectedReward.CostInPoints:N0} XP (te faltan {SelectedReward.CostInPoints - AvailableXp:N0}).";

    public AsyncRelayCommand CreateRewardCommand { get; }

    public AsyncRelayCommand RedeemRewardCommand { get; }

    protected override async Task LoadCoreAsync()
    {
        var profile = await _playerProfileService.GetProfileAsync();
        AvailableXp = profile.TotalXp;

        var rewards = await _rewardService.GetAllAsync();
        Rewards.Clear();
        foreach (var reward in rewards)
            Rewards.Add(reward);
    }

    private bool CanCreateReward() =>
        !string.IsNullOrWhiteSpace(Name) &&
        int.TryParse(CostInPoints, out var cost) && cost > 0;

    private bool CanRedeemSelected() => CanAffordSelected;

    private async Task CreateRewardAsync()
    {
        if (!CanCreateReward())
            return;

        var cost = int.Parse(CostInPoints);
        await RunBusyAsync(async () =>
        {
            var reward = await _rewardService.CreateAsync(Name, cost, Description);
            Rewards.Insert(0, reward);

            Name = string.Empty;
            CostInPoints = "500";
            Description = null;
            StatusMessage = $"Premio '{reward.Name}' creado.";
        }, "Creando premio...");
    }

    private async Task RedeemRewardAsync()
    {
        if (SelectedReward is null || !CanAffordSelected)
            return;

        await RunBusyAsync(async () =>
        {
            var result = await _rewardService.RedeemAsync(SelectedReward.Id);
            PublishAchievements(result.Events);

            var index = Rewards.IndexOf(SelectedReward);
            if (index >= 0)
                Rewards[index] = result.Value;

            SelectedReward = result.Value;
            var profile = await _playerProfileService.GetProfileAsync();
            AvailableXp = profile.TotalXp;

            StatusMessage = $"Premio canjeado: {result.Value.Name}";
        }, "Canjeando premio...");
    }
}
