using System.Collections.ObjectModel;
using System.Windows.Media;
using HobbyXP.Helpers;
using HobbyXP.Models.Core;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Results;
using HobbyXP.ViewModels.Common;
using HobbyXP.ViewModels.Navigation;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace HobbyXP.ViewModels.Dashboard;

public sealed class DashboardViewModel : LoadableViewModelBase
{
    private readonly IDashboardService _dashboardService;
    private readonly IPlayerProfileService _playerProfileService;
    private int _currentLevel = 1;
    private int _totalXp;
    private int _xpIntoCurrentLevel;
    private int _xpRequiredForNextLevel;
    private double _progressPercentage;
    private ISeries[] _weeklyXpSeries = [];
    private ISeries[] _hobbyDistributionSeries = [];
    private string _playerDisplayName = "Aventurero";
    private ImageSource? _avatarImage;
    private bool _hasCustomAvatar;
    private string _suggestionsSummary = "Sigue registrando actividades para subir de nivel.";

    public DashboardViewModel(
        IDashboardService dashboardService,
        IPlayerProfileService playerProfileService)
    {
        _dashboardService = dashboardService;
        _playerProfileService = playerProfileService;
        RecentMilestones = new ObservableCollection<Milestone>();
        SuggestedActivities = new ObservableCollection<LevelUpSuggestion>();
        RefreshCommand = new AsyncRelayCommand(() => LoadAsync());
    }

    public ObservableCollection<Milestone> RecentMilestones { get; }
    public ObservableCollection<LevelUpSuggestion> SuggestedActivities { get; }

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
        $"Nivel {CurrentLevel} · {XpIntoCurrentLevel}/{XpRequiredForNextLevel} XP · Total: {TotalXp:N0}";

    public string XpHeroSummary =>
        $"XP: {XpIntoCurrentLevel:N0} / {XpRequiredForNextLevel:N0}  |  {ProgressPercentage:0}%";

    public string LevelHeroTitle => $"{PlayerDisplayName.ToUpperInvariant()} — NIVEL {CurrentLevel}";

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

    public AsyncRelayCommand RefreshCommand { get; }

    public string SuggestionsSummary
    {
        get => _suggestionsSummary;
        private set => SetProperty(ref _suggestionsSummary, value);
    }

    protected override async Task LoadCoreAsync()
    {
        var profile = await _playerProfileService.GetProfileAsync();
        PlayerDisplayName = profile.DisplayName;
        AvatarImage = string.IsNullOrWhiteSpace(profile.AvatarPath)
            ? null
            : AvatarImageLoader.LoadOrDefault(profile.AvatarPath);
        HasCustomAvatar = !string.IsNullOrWhiteSpace(profile.AvatarPath);

        var summary = await _dashboardService.GetSummaryAsync();
        ApplyLevelProgress(summary.LevelProgress);
        BuildWeeklyChart(summary.WeeklyXp);
        BuildDistributionChart(summary.MonthlyHobbyDistribution);
        BuildSuggestions(summary);

        RecentMilestones.Clear();
        foreach (var milestone in summary.RecentMilestones)
            RecentMilestones.Add(milestone);

        OnPropertyChanged(nameof(LevelProgressText));
        OnPropertyChanged(nameof(XpHeroSummary));
        OnPropertyChanged(nameof(LevelHeroTitle));
    }

    private void BuildSuggestions(DashboardSummary summary)
    {
        var xpRemaining = Math.Max(0, summary.LevelProgress.XpRequiredForNextLevel - summary.LevelProgress.XpIntoCurrentLevel);
        SuggestionsSummary = xpRemaining == 0
            ? "Listo para subir de nivel. Registre una actividad más para activar la celebración."
            : $"Le faltan {xpRemaining:N0} XP para el próximo nivel. Mínimos sugeridos según las reglas actuales:";

        SuggestedActivities.Clear();
        foreach (var suggestion in summary.LevelUpSuggestions)
            SuggestedActivities.Add(suggestion);
    }

    private void ApplyLevelProgress(LevelProgressInfo progress)
    {
        CurrentLevel = progress.CurrentLevel;
        TotalXp = progress.TotalXp;
        XpIntoCurrentLevel = progress.XpIntoCurrentLevel;
        XpRequiredForNextLevel = progress.XpRequiredForNextLevel;
        ProgressPercentage = progress.ProgressPercentage;
    }

    private void BuildWeeklyChart(IReadOnlyList<DailyXpPoint> weeklyXp)
    {
        var cyan = SKColor.Parse("00E5FF");
        WeeklyXpSeries =
        [
            new LineSeries<int>
            {
                Name = "XP diaria",
                Values = weeklyXp.Select(p => p.TotalXp).ToArray(),
                Fill = null,
                Stroke = new SolidColorPaint(cyan) { StrokeThickness = 3 },
                GeometryFill = new SolidColorPaint(cyan),
                GeometryStroke = null,
                LineSmoothness = 0.35
            }
        ];
    }

    private void BuildDistributionChart(IReadOnlyList<HobbyDistributionSlice> distribution)
    {
        if (distribution.Count == 0)
        {
            HobbyDistributionSeries = [];
            return;
        }

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
}
