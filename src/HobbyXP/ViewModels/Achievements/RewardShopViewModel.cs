using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using HobbyXP.Models.Enums;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Messaging;
using HobbyXP.ViewModels.Common;
using HobbyXP.ViewModels.Messaging;
using HobbyXP.Views.Dialogs;

namespace HobbyXP.ViewModels.Achievements;

public sealed class RewardShopViewModel : AchievementAwareViewModel
{
    private readonly IRewardService _rewardService;
    private readonly IPlayerProfileService _playerProfileService;
    private readonly IXpService _xpService;
    private readonly IFileDialogService _fileDialogService;
    private readonly IImagePreviewService _imagePreviewService;
    private readonly IProfileRefreshMessenger _profileRefreshMessenger;
    private readonly Dictionary<MilestoneSourceType, int> _moduleBalances = new();
    private int _availableXp;
    private int _currentLevel = 1;
    private int? _equippedRewardId;

    public RewardShopViewModel(
        IRewardService rewardService,
        IPlayerProfileService playerProfileService,
        IXpService xpService,
        IFileDialogService fileDialogService,
        IImagePreviewService imagePreviewService,
        IProfileRefreshMessenger profileRefreshMessenger,
        IAchievementMessenger achievementMessenger)
        : base(achievementMessenger)
    {
        _rewardService = rewardService;
        _playerProfileService = playerProfileService;
        _xpService = xpService;
        _fileDialogService = fileDialogService;
        _imagePreviewService = imagePreviewService;
        _profileRefreshMessenger = profileRefreshMessenger;
        AvailableRewards = new ObservableCollection<RewardRowViewModel>();
        InventoryRewards = new ObservableCollection<RewardRowViewModel>();

        OpenRewardDetailCommand = new RelayCommand(OpenRewardDetail);
        OpenCreateRewardCommand = new RelayCommand(OpenCreateReward);
    }

    public ObservableCollection<RewardRowViewModel> AvailableRewards { get; }

    public ObservableCollection<RewardRowViewModel> InventoryRewards { get; }

    public int AvailableXp
    {
        get => _availableXp;
        private set
        {
            if (SetProperty(ref _availableXp, value))
                OnPropertyChanged(nameof(BalanceText));
        }
    }

    public string BalanceText =>
        $"Saldo total: {AvailableXp:N0} XP · nivel {_currentLevel} (el costo = base × nivel)";

    public RelayCommand OpenRewardDetailCommand { get; }

    public RelayCommand OpenCreateRewardCommand { get; }

    protected override async Task LoadCoreAsync()
    {
        var profile = await _playerProfileService.GetProfileAsync();
        AvailableXp = profile.SpendableXp;
        _currentLevel = profile.CurrentLevel;
        _equippedRewardId = profile.EquippedRewardId;

        _moduleBalances.Clear();
        foreach (var hobby in await _xpService.GetAllHobbyProgressAsync())
        {
            var balance = await _xpService.GetHobbySpendableXpAsync(hobby.SourceType);
            _moduleBalances[hobby.SourceType] = balance;
        }

        var rewards = await _rewardService.GetAllAsync();
        var available = new List<RewardRowViewModel>();
        var inventory = new List<RewardRowViewModel>();
        foreach (var reward in rewards)
        {
            var moduleBalance = reward.SourceType is { } module
                ? _moduleBalances.GetValueOrDefault(module, 0)
                : (int?)null;
            var row = new RewardRowViewModel(reward, _currentLevel, _equippedRewardId ?? 0, moduleBalance);
            if (row.IsAvailable)
                available.Add(row);
            else
                inventory.Add(row);
        }

        ReplaceRewards(AvailableRewards, available);
        ReplaceRewards(InventoryRewards, inventory);
    }

    private void OpenCreateReward() =>
        ShowDetailDialog(null);

    private void OpenRewardDetail(object? parameter)
    {
        if (parameter is not RewardRowViewModel row)
            return;

        ShowDetailDialog(row);
    }

    private void ShowDetailDialog(RewardRowViewModel? reward)
    {
        var detailVm = new RewardDetailViewModel(
            reward,
            _rewardService,
            _fileDialogService,
            _imagePreviewService,
            _currentLevel,
            _moduleBalances,
            _equippedRewardId);

        var dialog = new RewardDetailWindow(detailVm)
        {
            Owner = Application.Current.MainWindow
        };

        var accepted = dialog.ShowDialog() == true;
        if (!accepted || !detailVm.WasActionCompleted)
            return;

        if (detailVm.CompletionEvents.Length > 0)
            PublishAchievements(detailVm.CompletionEvents);

        if (detailVm.NeedsProfileRefresh)
            _profileRefreshMessenger.RequestRefresh();

        StatusMessage = detailVm.ResultMessage;
        _ = LoadCoreAsync();
    }

    private static void ReplaceRewards(
        ObservableCollection<RewardRowViewModel> target,
        IReadOnlyList<RewardRowViewModel> rows)
    {
        target.Clear();
        foreach (var row in rows.OrderBy(r => r.ModuleLabel).ThenBy(r => r.Name))
            target.Add(row);
    }
}
