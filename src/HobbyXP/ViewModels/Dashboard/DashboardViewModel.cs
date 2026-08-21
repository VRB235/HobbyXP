using System.Collections.ObjectModel;
using System.Windows.Media;
using HobbyXP.Helpers;
using HobbyXP.Models.Core;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Messaging;
using HobbyXP.Services.Results;
using HobbyXP.ViewModels.Common;
using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace HobbyXP.ViewModels.Dashboard;

public sealed class DashboardViewModel : LoadableViewModelBase
{
    private static readonly SKColor ChartAccent = SKColor.Parse("00E5FF");
    private static readonly SKColor ChartLabel = SKColor.Parse("8892A4");
    private static readonly SKColor ChartSeparator = SKColor.Parse("2A3347");

    private readonly IDashboardService _dashboardService;
    private readonly IPlayerProfileService _playerProfileService;
    private readonly IXpService _xpService;
    private readonly IWeeklyQuotaService _weeklyQuotaService;
    private readonly IAchievementProgressService _achievementProgress;
    private readonly IImagePreviewService _imagePreviewService;
    private int _currentLevel = 1;
    private int _totalXp;
    private int _xpIntoCurrentLevel;
    private int _xpRequiredForNextLevel;
    private double _progressPercentage;
    private ISeries[] _weeklyXpSeries = [];
    private ISeries[] _hobbyDistributionSeries = [];
    private ICartesianAxis[] _weeklyXpXAxes = [];
    private ICartesianAxis[] _weeklyXpYAxes = [];
    private bool _hasHobbyDistribution;
    private string _playerDisplayName = "Aventurero";
    private ImageSource? _avatarImage;
    private bool _hasCustomAvatar;
    private string _suggestionsSummary = "Sigue registrando actividades para subir de nivel.";
    private bool _showLevelUpSuggestions;
    private bool _isDisciplineExpanded;
    private bool _isHobbyProgressExpanded;
    private bool _isChartsExpanded;
    private bool _isSuggestionsExpanded = true;
    private AchievementHubSnapshot? _achievementHub;

    public DashboardViewModel(
        IDashboardService dashboardService,
        IPlayerProfileService playerProfileService,
        IXpService xpService,
        IWeeklyQuotaService weeklyQuotaService,
        IAchievementProgressService achievementProgress,
        IImagePreviewService imagePreviewService,
        IApplicationDataResetMessenger applicationDataResetMessenger)
    {
        _dashboardService = dashboardService;
        _playerProfileService = playerProfileService;
        _xpService = xpService;
        _weeklyQuotaService = weeklyQuotaService;
        _achievementProgress = achievementProgress;
        _imagePreviewService = imagePreviewService;
        RecentMilestones = new ObservableCollection<Milestone>();
        SuggestedActivities = new ObservableCollection<LevelUpSuggestion>();
        HobbyProgressItems = new ObservableCollection<HobbyProgressInfo>();
        WeeklyQuotaItems = new ObservableCollection<WeeklyQuotaProgress>();
        RefreshCommand = new AsyncRelayCommand(() => LoadAsync());
        OpenFeaturedRewardImageCommand = new RelayCommand(OpenFeaturedRewardImage, CanOpenFeaturedRewardImage);
        applicationDataResetMessenger.ApplicationDataReset += OnApplicationDataReset;
    }

    public RelayCommand OpenFeaturedRewardImageCommand { get; }

    public ObservableCollection<Milestone> RecentMilestones { get; }
    public ObservableCollection<LevelUpSuggestion> SuggestedActivities { get; }
    public ObservableCollection<HobbyProgressInfo> HobbyProgressItems { get; }
    public ObservableCollection<WeeklyQuotaProgress> WeeklyQuotaItems { get; }

    public int CurrentLevel
    {
        get => _currentLevel;
        private set => SetProperty(ref _currentLevel, value);
    }

    public int TotalXp
    {
        get => _totalXp;
        private set => SetProperty(ref _totalXp, value);
    }

    public int XpIntoCurrentLevel
    {
        get => _xpIntoCurrentLevel;
        private set => SetProperty(ref _xpIntoCurrentLevel, value);
    }

    public int XpRequiredForNextLevel
    {
        get => _xpRequiredForNextLevel;
        private set => SetProperty(ref _xpRequiredForNextLevel, value);
    }

    public double ProgressPercentage
    {
        get => _progressPercentage;
        private set => SetProperty(ref _progressPercentage, ClampProgress(value));
    }

    public string LevelProgressText =>
        $"{GlobalLevelTitles.FormatLevelLabel(CurrentLevel)} · {XpIntoCurrentLevel}/{XpRequiredForNextLevel} XP · Total: {TotalXp:N0}";

    public string XpHeroSummary =>
        $"XP: {XpIntoCurrentLevel:N0} / {XpRequiredForNextLevel:N0}  |  {ProgressPercentage:0}%";

    public string LevelHeroTitle =>
        $"{PlayerDisplayName.ToUpperInvariant()} — {GlobalLevelTitles.FormatLevelLabel(CurrentLevel).ToUpperInvariant()}";

    public string GlobalLevelTitle => GlobalLevelTitles.GetTitle(CurrentLevel);

    public string PlayerDisplayName
    {
        get => _playerDisplayName;
        private set => SetProperty(ref _playerDisplayName, value);
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

    public ISeries[] WeeklyXpSeries
    {
        get => _weeklyXpSeries;
        private set => SetProperty(ref _weeklyXpSeries, value);
    }

    public ISeries[] HobbyDistributionSeries
    {
        get => _hobbyDistributionSeries;
        private set => SetProperty(ref _hobbyDistributionSeries, value);
    }

    public ICartesianAxis[] WeeklyXpXAxes
    {
        get => _weeklyXpXAxes;
        private set => SetProperty(ref _weeklyXpXAxes, value);
    }

    public ICartesianAxis[] WeeklyXpYAxes
    {
        get => _weeklyXpYAxes;
        private set => SetProperty(ref _weeklyXpYAxes, value);
    }

    public bool HasHobbyDistribution
    {
        get => _hasHobbyDistribution;
        private set => SetProperty(ref _hasHobbyDistribution, value);
    }

    public AsyncRelayCommand RefreshCommand { get; }

    public AchievementHubSnapshot? AchievementHub
    {
        get => _achievementHub;
        private set
        {
            if (SetProperty(ref _achievementHub, value))
                OnPropertyChanged(nameof(HasAchievementHub));
        }
    }

    public bool HasAchievementHub => AchievementHub is not null;

    public string SuggestionsSummary
    {
        get => _suggestionsSummary;
        private set => SetProperty(ref _suggestionsSummary, value);
    }

    public bool ShowLevelUpSuggestions
    {
        get => _showLevelUpSuggestions;
        private set => SetProperty(ref _showLevelUpSuggestions, value);
    }

    public bool IsDisciplineExpanded
    {
        get => _isDisciplineExpanded;
        set => SetProperty(ref _isDisciplineExpanded, value);
    }

    public bool IsHobbyProgressExpanded
    {
        get => _isHobbyProgressExpanded;
        set => SetProperty(ref _isHobbyProgressExpanded, value);
    }

    public bool IsChartsExpanded
    {
        get => _isChartsExpanded;
        set => SetProperty(ref _isChartsExpanded, value);
    }

    public bool IsSuggestionsExpanded
    {
        get => _isSuggestionsExpanded;
        set => SetProperty(ref _isSuggestionsExpanded, value);
    }

    protected override async Task LoadCoreAsync()
    {
        var profile = await _playerProfileService.GetProfileAsync();
        var avatar = AvatarImageLoader.Load(profile.AvatarPath);
        PlayerDisplayName = profile.DisplayName;
        AvatarImage = avatar.Image;
        HasCustomAvatar = avatar.HasCustomAvatar;

        var summary = await _dashboardService.GetSummaryAsync();
        ApplyLevelProgress(summary.LevelProgress);
        BuildWeeklyChart(summary.WeeklyXp);
        BuildDistributionChart(summary.MonthlyHobbyDistribution);
        BuildSuggestions(summary);
        await LoadHobbyProgressAsync();
        await LoadWeeklyQuotasAsync();
        AchievementHub = await _achievementProgress.GetHubSnapshotAsync();

        RecentMilestones.Clear();
        foreach (var milestone in summary.RecentMilestones)
            RecentMilestones.Add(milestone);

        OnPropertyChanged(nameof(LevelProgressText));
        OnPropertyChanged(nameof(XpHeroSummary));
        OnPropertyChanged(nameof(LevelHeroTitle));
        OnPropertyChanged(nameof(GlobalLevelTitle));
    }

    private async Task LoadHobbyProgressAsync()
    {
        var items = await _xpService.GetAllHobbyProgressAsync();
        HobbyProgressItems.Clear();
        foreach (var item in items)
            HobbyProgressItems.Add(item);
    }

    private async Task LoadWeeklyQuotasAsync()
    {
        var items = await _weeklyQuotaService.GetCurrentWeekProgressAsync();
        WeeklyQuotaItems.Clear();
        foreach (var item in items)
            WeeklyQuotaItems.Add(item);

        if (items.Any(i => i.HasActivePenalty))
            IsDisciplineExpanded = true;
    }

    private void BuildSuggestions(DashboardSummary summary)
    {
        if (summary.LevelProgress.TotalXp == 0)
        {
            SuggestionsSummary = "Registre su primera actividad para comenzar a acumular XP.";
            SuggestedActivities.Clear();
            ShowLevelUpSuggestions = false;
            return;
        }

        var xpRemaining = Math.Max(0, summary.LevelProgress.XpRequiredForNextLevel - summary.LevelProgress.XpIntoCurrentLevel);
        SuggestionsSummary = xpRemaining == 0
            ? "Listo para subir de nivel. Registre una actividad más para activar la celebración."
            : $"Le faltan {xpRemaining:N0} XP para el próximo nivel. Mínimos sugeridos según las reglas actuales:";

        SuggestedActivities.Clear();
        foreach (var suggestion in summary.LevelUpSuggestions)
            SuggestedActivities.Add(suggestion);

        ShowLevelUpSuggestions = SuggestedActivities.Count > 0 || xpRemaining == 0;
    }

    private async void OnApplicationDataReset(object? sender, EventArgs e)
    {
        InvalidateLoaded();
        await LoadAsync();
    }

    private void ApplyLevelProgress(LevelProgressInfo progress)
    {
        CurrentLevel = progress.CurrentLevel;
        TotalXp = progress.TotalXp;
        XpIntoCurrentLevel = progress.XpIntoCurrentLevel;
        XpRequiredForNextLevel = progress.XpRequiredForNextLevel;
        ProgressPercentage = progress.ProgressPercentage;
        OnPropertyChanged(nameof(GlobalLevelTitle));
    }

    private void BuildWeeklyChart(IReadOnlyList<DailyXpPoint> weeklyXp)
    {
        WeeklyXpXAxes =
        [
            new Axis
            {
                Labels = weeklyXp.Select(p => p.Date.ToString("dd/MM")).ToArray(),
                LabelsPaint = new SolidColorPaint(ChartLabel),
                SeparatorsPaint = new SolidColorPaint(ChartSeparator) { StrokeThickness = 1 },
                TextSize = 12
            }
        ];

        WeeklyXpYAxes =
        [
            new Axis
            {
                MinLimit = 0,
                Labeler = value => value.ToString("N0"),
                LabelsPaint = new SolidColorPaint(ChartLabel),
                SeparatorsPaint = new SolidColorPaint(ChartSeparator) { StrokeThickness = 1 },
                TextSize = 12
            }
        ];

        WeeklyXpSeries =
        [
            new LineSeries<int>
            {
                Name = "XP diaria",
                Values = weeklyXp.Select(p => p.TotalXp).ToArray(),
                Fill = null,
                Stroke = new SolidColorPaint(ChartAccent) { StrokeThickness = 3 },
                GeometryFill = new SolidColorPaint(ChartAccent),
                GeometryStroke = null,
                LineSmoothness = 0.35
            }
        ];
    }

    private void BuildDistributionChart(IReadOnlyList<HobbyDistributionSlice> distribution)
    {
        if (distribution.Count == 0)
        {
            HasHobbyDistribution = false;
            HobbyDistributionSeries = [];
            return;
        }

        HasHobbyDistribution = true;
        var palette = new[]
        {
            SKColor.Parse("00B0FF"),
            SKColor.Parse("00E676"),
            SKColor.Parse("FF9100"),
            SKColor.Parse("7C4DFF"),
            SKColor.Parse("FF5252"),
            SKColor.Parse("FFD54F"),
            SKColor.Parse("26A69A")
        };

        HobbyDistributionSeries = distribution
            .Select((slice, index) => new PieSeries<double>
            {
                Name = $"{slice.Label} ({slice.Percentage:0}%)",
                Values = new[] { slice.Percentage },
                InnerRadius = 45,
                Fill = new SolidColorPaint(palette[index % palette.Length])
            })
            .Cast<ISeries>()
            .ToArray();
    }

    private static double ClampProgress(double value) =>
        double.IsNaN(value) || double.IsInfinity(value) ? 0d : Math.Clamp(value, 0d, 100d);

    private bool CanOpenFeaturedRewardImage() =>
        AchievementHub is { HasFeaturedImage: true, FeaturedImagePath: not null };

    private void OpenFeaturedRewardImage()
    {
        if (AchievementHub?.FeaturedImagePath is not { } path)
            return;

        _imagePreviewService.Show(path, AchievementHub.FeaturedReward?.Name ?? "Premio");
    }
}
