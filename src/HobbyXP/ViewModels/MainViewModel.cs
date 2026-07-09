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
    private object? _currentViewModel;
    private NavigationSection _currentSection = NavigationSection.Dashboard;
    private string? _latestAchievementMessage;
    private int _playerLevel = 1;
    private int _playerTotalXp;
    private double _playerProgressPercentage;
    private string _displayName = "Aventurero";
    private ImageSource? _avatarImage;
    private bool _hasCustomAvatar;
    private bool _isLevelUpVisible;
    private int _celebrationLevel = 1;
    private int _celebrationTotalXp;

    public MainViewModel(
        INavigationService navigationService,
        IAchievementMessenger achievementMessenger,
        ILevelUpMessenger levelUpMessenger,
        IPlayerProfileService playerProfileService,
        IFileDialogService fileDialogService,
        IProfileRefreshMessenger profileRefreshMessenger)
    {
        _navigationService = navigationService;
        _achievementMessenger = achievementMessenger;
        _levelUpMessenger = levelUpMessenger;
        _playerProfileService = playerProfileService;
        _fileDialogService = fileDialogService;
        _profileRefreshMessenger = profileRefreshMessenger;

        NavigationItems = new ObservableCollection<NavigationItem>(new[]
        {
            new NavigationItem(NavigationSection.Dashboard, "Dashboard", "🏠"),
            new NavigationItem(NavigationSection.PhysicalActivities, "Actividades Físicas", "🏃"),
            new NavigationItem(NavigationSection.Entertainment, "Entretenimiento", "🎮"),
            new NavigationItem(NavigationSection.PersonalGrowth, "Crecimiento Personal", "📚"),
            new NavigationItem(NavigationSection.Achievements, "Logros y Premios", "🏆")
        });

        NavigateCommand = new AsyncRelayCommand(NavigateAsync);
        PickAvatarCommand = new AsyncRelayCommand(PickAvatarAsync);
        SaveDisplayNameCommand = new AsyncRelayCommand(SaveDisplayNameAsync, () => !string.IsNullOrWhiteSpace(DisplayName));
        DismissLevelUpCommand = new RelayCommand(DismissLevelUp);

        _navigationService.CurrentViewModelChanged += (_, _) => SyncNavigationState();
        _achievementMessenger.AchievementPublished += OnAchievementPublished;
        _levelUpMessenger.LevelUpPublished += OnLevelUpPublished;
        _profileRefreshMessenger.ProfileRefreshRequested += OnProfileRefreshRequested;

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

    public double PlayerProgressPercentage
    {
        get => _playerProgressPercentage;
        private set => SetProperty(ref _playerProgressPercentage, ClampProgress(value));
    }

    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
    }

    public ImageSource? AvatarImage
    {
        get => _avatarImage;
        private set
        {
            if (SetProperty(ref _avatarImage, value))
                HasCustomAvatar = value is not null;
        }
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
        private set => SetProperty(ref _celebrationLevel, value);
    }

    public int CelebrationTotalXp
    {
        get => _celebrationTotalXp;
        private set => SetProperty(ref _celebrationTotalXp, value);
    }

    public string SidebarProfileTitle => DisplayName;

    public string SidebarLevelText => $"Nivel {PlayerLevel}";

    public string SidebarXpSummary => $"{PlayerTotalXp:N0} XP acumulados";

    public AsyncRelayCommand NavigateCommand { get; }

    public AsyncRelayCommand PickAvatarCommand { get; }

    public AsyncRelayCommand SaveDisplayNameCommand { get; }

    public RelayCommand DismissLevelUpCommand { get; }

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
        PlayerProgressPercentage = progress.ProgressPercentage;
        DisplayName = profile.DisplayName;
        ApplyAvatar(profile.AvatarPath);

        OnPropertyChanged(nameof(SidebarProfileTitle));
        OnPropertyChanged(nameof(SidebarLevelText));
        OnPropertyChanged(nameof(SidebarXpSummary));

        await RefreshDashboardAsync();
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

    private async Task SaveDisplayNameAsync()
    {
        if (string.IsNullOrWhiteSpace(DisplayName))
            return;

        await _playerProfileService.UpdateDisplayNameAsync(DisplayName);
        OnPropertyChanged(nameof(SidebarProfileTitle));
        LatestAchievementMessage = $"Nombre guardado: {DisplayName}";
        await RefreshDashboardAsync();
    }

    private void ApplyAvatar(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            AvatarImage = null;
            HasCustomAvatar = false;
            return;
        }

        AvatarImage = AvatarImageLoader.LoadOrDefault(path);
        HasCustomAvatar = true;
    }

    private async Task RefreshDashboardAsync()
    {
        if (CurrentViewModel is DashboardViewModel dashboard)
            await dashboard.LoadAsync();
    }

    private async Task NavigateAsync(object? parameter)
    {
        if (parameter is NavigationSection section)
            await _navigationService.NavigateAsync(section);
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
            ? $"🎉 {e.Title} (+{e.PointsEarned} XP){medal}"
            : $"{e.Title} (+{e.PointsEarned} XP){medal}";

        await RefreshProfileAsync();
    }

    private async void OnLevelUpPublished(object? sender, LevelUpCelebrationInfo info)
    {
        CelebrationLevel = info.NewLevel;
        CelebrationTotalXp = info.TotalXp;
        IsLevelUpVisible = true;
        LatestAchievementMessage = $"🎉 ¡Subiste al nivel {info.NewLevel}!";
        await RefreshProfileAsync();
    }

    private async void OnProfileRefreshRequested(object? sender, EventArgs e) =>
        await RefreshProfileAsync();

    private void DismissLevelUp() => IsLevelUpVisible = false;

    private static double ClampProgress(double value) =>
        double.IsNaN(value) || double.IsInfinity(value) ? 0d : Math.Clamp(value, 0d, 100d);
}
