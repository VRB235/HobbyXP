using System.Collections.ObjectModel;
using System.Windows.Media;
using HobbyXP.Helpers;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Messaging;
using HobbyXP.Services.Results;
using HobbyXP.ViewModels.Common;
using HobbyXP.ViewModels.Dashboard;
using HobbyXP.ViewModels.Messaging;
using HobbyXP.ViewModels.Navigation;

namespace HobbyXP.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IAchievementMessenger _achievementMessenger;
    private readonly ILevelUpMessenger _levelUpMessenger;
    private readonly IPlayerProfileService _playerProfileService;
    private readonly IFileDialogService _fileDialogService;
    private readonly IProfileRefreshMessenger _profileRefreshMessenger;
    private readonly IApplicationDataResetMessenger _applicationDataResetMessenger;
    private readonly IAchievementProgressService _achievementProgress;
    private readonly Queue<AchievementEvent> _pendingMedalCelebrations = new();
    private object? _currentViewModel;
    private NavigationSection _currentSection = NavigationSection.Dashboard;
    private string? _latestAchievementMessage;
    private int _playerLevel = 1;
    private int _playerTotalXp;
    private int _playerSpendableXp;
    private double _playerProgressPercentage;
    private string _displayName = "Aventurero";
    private string? _displayNameValidationMessage;
    private ImageSource? _avatarImage;
    private bool _hasCustomAvatar;
    private bool _isLevelUpVisible;
    private int _celebrationLevel = 1;
    private int _celebrationTotalXp;
    private bool _isMedalUnlockVisible;
    private string _celebrationMedalName = string.Empty;
    private string _celebrationMedalDescription = string.Empty;
    private string? _celebrationMedalIconPath;
    private int _celebrationMedalBonus;
    private string? _honorTitle;
    private string? _equippedRewardName;
    private bool _hasEquippedReward;
    private string? _immunityText;

    public MainViewModel(
        INavigationService navigationService,
        IAchievementMessenger achievementMessenger,
        ILevelUpMessenger levelUpMessenger,
        IPlayerProfileService playerProfileService,
        IFileDialogService fileDialogService,
        IProfileRefreshMessenger profileRefreshMessenger,
        IApplicationDataResetMessenger applicationDataResetMessenger,
        IAchievementProgressService achievementProgress)
    {
        _navigationService = navigationService;
        _achievementMessenger = achievementMessenger;
        _levelUpMessenger = levelUpMessenger;
        _playerProfileService = playerProfileService;
        _fileDialogService = fileDialogService;
        _profileRefreshMessenger = profileRefreshMessenger;
        _applicationDataResetMessenger = applicationDataResetMessenger;
        _achievementProgress = achievementProgress;

        NavigationItems = new ObservableCollection<NavigationItem>(new[]
        {
            new NavigationItem(NavigationSection.Dashboard, "Dashboard", "🏠"),
            new NavigationItem(NavigationSection.PhysicalActivities, "Actividades Físicas", "🏃"),
            new NavigationItem(NavigationSection.Entertainment, "Entretenimiento", "🎮"),
            new NavigationItem(NavigationSection.PersonalGrowth, "Crecimiento Personal", "📚"),
            new NavigationItem(NavigationSection.Achievements, "Logros y Premios", "🏆"),
            new NavigationItem(NavigationSection.Settings, "Configuración", "⚙️")
        });

        NavigateCommand = new AsyncRelayCommand(NavigateAsync);
        PickAvatarCommand = new AsyncRelayCommand(PickAvatarAsync);
        SaveDisplayNameCommand = new AsyncRelayCommand(SaveDisplayNameAsync, CanSaveDisplayName);
        DismissLevelUpCommand = new RelayCommand(DismissLevelUp);
        DismissMedalUnlockCommand = new RelayCommand(DismissMedalUnlock);
        OpenAchievementsFromMedalCommand = new AsyncRelayCommand(OpenAchievementsFromMedalAsync);

        RefreshDisplayNameValidation();

        _navigationService.CurrentViewModelChanged += (_, _) => SyncNavigationState();
        _achievementMessenger.AchievementPublished += OnAchievementPublished;
        _levelUpMessenger.LevelUpPublished += OnLevelUpPublished;
        _profileRefreshMessenger.ProfileRefreshRequested += OnProfileRefreshRequested;
        _applicationDataResetMessenger.ApplicationDataReset += OnApplicationDataReset;

        SyncNavigationState();
    }

    public ObservableCollection<NavigationItem> NavigationItems { get; }

    public object? CurrentViewModel
    {
        get => _currentViewModel;
        private set => SetProperty(ref _currentViewModel, value);
    }

    public NavigationSection CurrentSection
    {
        get => _currentSection;
        private set => SetProperty(ref _currentSection, value);
    }

    public string? LatestAchievementMessage
    {
        get => _latestAchievementMessage;
        private set => SetProperty(ref _latestAchievementMessage, value);
    }

    public int PlayerLevel
    {
        get => _playerLevel;
        private set => SetProperty(ref _playerLevel, value);
    }

    public int PlayerTotalXp
    {
        get => _playerTotalXp;
        private set => SetProperty(ref _playerTotalXp, value);
    }

    public int PlayerSpendableXp
    {
        get => _playerSpendableXp;
        private set => SetProperty(ref _playerSpendableXp, value);
    }

    public double PlayerProgressPercentage
    {
        get => _playerProgressPercentage;
        private set => SetProperty(ref _playerProgressPercentage, ClampProgress(value));
    }

    public string DisplayName
    {
        get => _displayName;
        set
        {
            if (SetProperty(ref _displayName, value))
            {
                RefreshDisplayNameValidation();
                OnPropertyChanged(nameof(SidebarProfileTitle));
            }
        }
    }

    public string? DisplayNameValidationMessage
    {
        get => _displayNameValidationMessage;
        private set => SetProperty(ref _displayNameValidationMessage, value);
    }

    public ImageSource? AvatarImage
    {
        get => _avatarImage;
        private set => SetProperty(ref _avatarImage, value);
    }

    public bool HasCustomAvatar
    {
        get => _hasCustomAvatar;
        private set => SetProperty(ref _hasCustomAvatar, value);
    }

    public bool IsLevelUpVisible
    {
        get => _isLevelUpVisible;
        private set => SetProperty(ref _isLevelUpVisible, value);
    }

    public int CelebrationLevel
    {
        get => _celebrationLevel;
        private set
        {
            if (SetProperty(ref _celebrationLevel, value))
                OnPropertyChanged(nameof(CelebrationLevelTitle));
        }
    }

    public int CelebrationTotalXp
    {
        get => _celebrationTotalXp;
        private set => SetProperty(ref _celebrationTotalXp, value);
    }

    public string CelebrationLevelTitle => GlobalLevelTitles.GetTitle(CelebrationLevel);

    public string SidebarProfileTitle => DisplayName;

    public string SidebarLevelText => GlobalLevelTitles.FormatLevelLabel(PlayerLevel);

    public string SidebarXpSummary => $"{PlayerTotalXp:N0} XP de progresión";

    public string SidebarSpendableSummary => $"Saldo: {PlayerSpendableXp:N0}";

    public AsyncRelayCommand NavigateCommand { get; }

    public AsyncRelayCommand PickAvatarCommand { get; }

    public AsyncRelayCommand SaveDisplayNameCommand { get; }

    public RelayCommand DismissLevelUpCommand { get; }

    public RelayCommand DismissMedalUnlockCommand { get; }

    public AsyncRelayCommand OpenAchievementsFromMedalCommand { get; }

    public bool IsMedalUnlockVisible
    {
        get => _isMedalUnlockVisible;
        private set => SetProperty(ref _isMedalUnlockVisible, value);
    }

    public string CelebrationMedalName
    {
        get => _celebrationMedalName;
        private set => SetProperty(ref _celebrationMedalName, value);
    }

    public string CelebrationMedalDescription
    {
        get => _celebrationMedalDescription;
        private set => SetProperty(ref _celebrationMedalDescription, value);
    }

    public string? CelebrationMedalIconPath
    {
        get => _celebrationMedalIconPath;
        private set => SetProperty(ref _celebrationMedalIconPath, value);
    }

    public int CelebrationMedalBonus
    {
        get => _celebrationMedalBonus;
        private set => SetProperty(ref _celebrationMedalBonus, value);
    }

    public string? HonorTitle
    {
        get => _honorTitle;
        private set
        {
            if (SetProperty(ref _honorTitle, value))
                OnPropertyChanged(nameof(HasHonorTitle));
        }
    }

    public bool HasHonorTitle => !string.IsNullOrWhiteSpace(HonorTitle);

    public string? EquippedRewardName
    {
        get => _equippedRewardName;
        private set => SetProperty(ref _equippedRewardName, value);
    }

    public bool HasEquippedReward
    {
        get => _hasEquippedReward;
        private set => SetProperty(ref _hasEquippedReward, value);
    }

    public string? ImmunityText
    {
        get => _immunityText;
        private set
        {
            if (SetProperty(ref _immunityText, value))
                OnPropertyChanged(nameof(HasImmunity));
        }
    }

    public bool HasImmunity => !string.IsNullOrWhiteSpace(ImmunityText);

    public async Task InitializeAsync()
    {
        await RefreshProfileAsync();
        await _navigationService.NavigateAsync(NavigationSection.Dashboard);
    }

    public async Task RefreshProfileAsync()
    {
        var profile = await _playerProfileService.GetProfileAsync();
        var progress = await _playerProfileService.GetLevelProgressAsync();

        PlayerLevel = progress.CurrentLevel;
        PlayerTotalXp = progress.TotalXp;
        PlayerSpendableXp = profile.SpendableXp;
        PlayerProgressPercentage = progress.ProgressPercentage;
        DisplayName = profile.DisplayName;
        RefreshDisplayNameValidation();
        ApplyAvatar(profile.AvatarPath);

        OnPropertyChanged(nameof(SidebarProfileTitle));
        OnPropertyChanged(nameof(SidebarLevelText));
        OnPropertyChanged(nameof(SidebarXpSummary));
        OnPropertyChanged(nameof(SidebarSpendableSummary));

        await RefreshUnseenBadgeAsync();
        await RefreshDashboardAsync();
    }

    private async Task RefreshUnseenBadgeAsync()
    {
        var unseen = await _achievementProgress.GetUnseenMedalCountAsync();
        var hub = await _achievementProgress.GetHubSnapshotAsync();
        HonorTitle = hub.HonorTitle;
        EquippedRewardName = string.IsNullOrWhiteSpace(hub.EquippedRewardName)
            ? null
            : $"Reliquia: {hub.EquippedRewardName}";
        HasEquippedReward = !string.IsNullOrWhiteSpace(hub.EquippedRewardName);
        ImmunityText = hub.IsImmune ? hub.ImmunityText : null;

        var achievementsNav = NavigationItems.FirstOrDefault(i => i.Section == NavigationSection.Achievements);
        if (achievementsNav is not null)
            achievementsNav.BadgeCount = unseen;
    }

    private async Task PickAvatarAsync()
    {
        var path = _fileDialogService.PickImageFile();
        if (string.IsNullOrWhiteSpace(path))
            return;

        var profile = await _playerProfileService.UpdateAvatarPathAsync(path);
        ApplyAvatar(profile.AvatarPath);
        LatestAchievementMessage = "Avatar actualizado.";
        await RefreshDashboardAsync();
    }

    private void RefreshDisplayNameValidation()
    {
        var result = FormValidation.RequireText(DisplayName, "el nombre del personaje");
        DisplayNameValidationMessage = result.IsValid ? null : result.Message;
        SaveDisplayNameCommand.RaiseCanExecuteChanged();
    }

    private bool CanSaveDisplayName() =>
        FormValidation.RequireText(DisplayName, "el nombre del personaje").IsValid;

    private async Task SaveDisplayNameAsync()
    {
        if (!CanSaveDisplayName())
        {
            RefreshDisplayNameValidation();
            return;
        }

        await _playerProfileService.UpdateDisplayNameAsync(DisplayName.Trim());
        DisplayNameValidationMessage = null;
        OnPropertyChanged(nameof(SidebarProfileTitle));
        LatestAchievementMessage = $"Nombre guardado: {DisplayName}";
        await RefreshDashboardAsync();
    }

    private void ApplyAvatar(string? path)
    {
        var result = AvatarImageLoader.Load(path);
        AvatarImage = result.Image;
        HasCustomAvatar = result.HasCustomAvatar;
    }

    private async Task RefreshDashboardAsync()
    {
        if (CurrentViewModel is DashboardViewModel dashboard)
            await dashboard.LoadAsync();
    }

    private async Task NavigateAsync(object? parameter)
    {
        if (parameter is not NavigationSection section)
            return;

        await _navigationService.NavigateAsync(section);
        if (section == NavigationSection.Achievements)
        {
            await _achievementProgress.MarkMedalsSeenAsync();
            await RefreshUnseenBadgeAsync();
        }
    }

    private void SyncNavigationState()
    {
        CurrentViewModel = _navigationService.CurrentViewModel;
        CurrentSection = _navigationService.CurrentSection;

        foreach (var item in NavigationItems)
            item.IsActive = item.Section == CurrentSection;
    }

    private async void OnAchievementPublished(object? sender, AchievementEvent e)
    {
        var medal = e.MedalUnlocked.HasValue ? $" · Medalla: {e.MedalUnlocked}" : string.Empty;
        LatestAchievementMessage = e.RequiresCelebration
            ? $"🎉 {e.Title} (+{e.PointsEarned:N0} XP){medal}"
            : $"{e.Title} (+{e.PointsEarned:N0} XP){medal}";

        if (e.MedalUnlocked.HasValue && e.RequiresCelebration)
        {
            _pendingMedalCelebrations.Enqueue(e);
            TryShowNextMedalCelebration();
        }

        await RefreshProfileAsync();
    }

    private async void OnLevelUpPublished(object? sender, LevelUpCelebrationInfo info)
    {
        CelebrationLevel = info.NewLevel;
        CelebrationTotalXp = info.TotalXp;
        IsLevelUpVisible = true;
        LatestAchievementMessage =
            $"🎉 ¡{GlobalLevelTitles.FormatLevelLabel(info.NewLevel)}!";
        await RefreshProfileAsync();
    }

    private async void OnProfileRefreshRequested(object? sender, EventArgs e) =>
        await RefreshProfileAsync();

    private async void OnApplicationDataReset(object? sender, EventArgs e)
    {
        _navigationService.InvalidateAllLoadedSections();
        await RefreshProfileAsync();
        await _navigationService.NavigateAsync(NavigationSection.Dashboard);

        if (_navigationService.CurrentViewModel is DashboardViewModel dashboard)
            await dashboard.LoadAsync();
    }

    private void DismissLevelUp()
    {
        IsLevelUpVisible = false;
        TryShowNextMedalCelebration();
    }

    private void DismissMedalUnlock()
    {
        IsMedalUnlockVisible = false;
        TryShowNextMedalCelebration();
    }

    private async Task OpenAchievementsFromMedalAsync()
    {
        IsMedalUnlockVisible = false;
        _pendingMedalCelebrations.Clear();
        await _navigationService.NavigateAsync(NavigationSection.Achievements);
        await _achievementProgress.MarkMedalsSeenAsync();
        await RefreshUnseenBadgeAsync();
    }

    private void TryShowNextMedalCelebration()
    {
        if (IsLevelUpVisible || IsMedalUnlockVisible || _pendingMedalCelebrations.Count == 0)
            return;

        var next = _pendingMedalCelebrations.Dequeue();
        CelebrationMedalName = next.Title;
        CelebrationMedalDescription = next.Description;
        CelebrationMedalBonus = next.PointsEarned;
        CelebrationMedalIconPath = next.MedalUnlocked.HasValue
            ? MedalIconPaths.ForMedalCode(next.MedalUnlocked.Value)
            : null;
        IsMedalUnlockVisible = true;
    }

    private static double ClampProgress(double value) =>
        double.IsNaN(value) || double.IsInfinity(value) ? 0d : Math.Clamp(value, 0d, 100d);
}
