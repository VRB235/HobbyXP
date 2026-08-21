using HobbyXP.Helpers;
using HobbyXP.Models.Enums;
using HobbyXP.Services.Abstractions;
using HobbyXP.ViewModels.Common;

namespace HobbyXP.ViewModels.Common;

/// <summary>
/// Estado enlazable de la barra de nivel/XP de un hobby.
/// </summary>
public sealed class HobbyProgressPresenter : ViewModelBase
{
    private readonly IXpService _xpService;
    private readonly IWeeklyQuotaService? _weeklyQuotaService;
    private readonly IAchievementProgressService? _achievementProgress;
    private readonly MilestoneSourceType _sourceType;
    private string _title;
    private int _currentLevel = 1;
    private int _totalXp;
    private int _xpIntoCurrentLevel;
    private int _xpRequiredForNextLevel = 1;
    private double _progressPercentage;
    private string? _penaltyReminder;
    private string? _nextMedalText;
    private double _nextMedalPercent;
    private string? _nearestRewardText;
    private double _nearestRewardPercent;
    private string? _nearestRewardImagePath;
    private string? _nearestRewardPriceLabel;

    public HobbyProgressPresenter(
        IXpService xpService,
        MilestoneSourceType sourceType,
        IWeeklyQuotaService? weeklyQuotaService = null,
        IAchievementProgressService? achievementProgress = null)
    {
        _xpService = xpService;
        _weeklyQuotaService = weeklyQuotaService;
        _achievementProgress = achievementProgress;
        _sourceType = sourceType;
        _title = HobbyProgressCatalog.GetDisplayName(sourceType);
    }

    public MilestoneSourceType SourceType => _sourceType;

    public string Title
    {
        get => _title;
        private set => SetProperty(ref _title, value);
    }

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
        private set => SetProperty(ref _progressPercentage, value);
    }

    public string? PenaltyReminder
    {
        get => _penaltyReminder;
        private set
        {
            if (SetProperty(ref _penaltyReminder, value))
                OnPropertyChanged(nameof(HasPenaltyReminder));
        }
    }

    public bool HasPenaltyReminder => !string.IsNullOrWhiteSpace(PenaltyReminder);

    public string? NextMedalText
    {
        get => _nextMedalText;
        private set
        {
            if (SetProperty(ref _nextMedalText, value))
                OnPropertyChanged(nameof(HasNextMedal));
        }
    }

    public double NextMedalPercent
    {
        get => _nextMedalPercent;
        private set => SetProperty(ref _nextMedalPercent, value);
    }

    public bool HasNextMedal => !string.IsNullOrWhiteSpace(NextMedalText);

    public string? NearestRewardText
    {
        get => _nearestRewardText;
        private set
        {
            if (SetProperty(ref _nearestRewardText, value))
                OnPropertyChanged(nameof(HasNearestReward));
        }
    }

    public double NearestRewardPercent
    {
        get => _nearestRewardPercent;
        private set => SetProperty(ref _nearestRewardPercent, value);
    }

    public string? NearestRewardImagePath
    {
        get => _nearestRewardImagePath;
        private set
        {
            if (SetProperty(ref _nearestRewardImagePath, value))
                OnPropertyChanged(nameof(HasNearestRewardImage));
        }
    }

    public string? NearestRewardPriceLabel
    {
        get => _nearestRewardPriceLabel;
        private set
        {
            if (SetProperty(ref _nearestRewardPriceLabel, value))
                OnPropertyChanged(nameof(HasNearestRewardPrice));
        }
    }

    public bool HasNearestReward => !string.IsNullOrWhiteSpace(NearestRewardText);

    public bool HasNearestRewardImage => !string.IsNullOrWhiteSpace(NearestRewardImagePath);

    public bool HasNearestRewardPrice => !string.IsNullOrWhiteSpace(NearestRewardPriceLabel);

    public string LevelText => HobbyLevelTitles.FormatLevelLabel(_sourceType, CurrentLevel);

    public string LevelTitle => HobbyLevelTitles.GetTitle(_sourceType, CurrentLevel);

    public string ProgressText =>
        $"{XpIntoCurrentLevel:N0} / {XpRequiredForNextLevel:N0} XP · Total: {TotalXp:N0}";

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var progress = await _xpService.GetHobbyProgressAsync(_sourceType, cancellationToken);
        Title = HobbyProgressCatalog.GetDisplayName(_sourceType);
        CurrentLevel = progress.CurrentLevel;
        TotalXp = progress.TotalXp;
        XpIntoCurrentLevel = progress.XpIntoCurrentLevel;
        XpRequiredForNextLevel = progress.XpRequiredForNextLevel;
        ProgressPercentage = progress.ProgressPercentage;

        if (_weeklyQuotaService is not null)
        {
            var reminders = await _weeklyQuotaService.GetActivePenaltyRemindersAsync(_sourceType, cancellationToken);
            PenaltyReminder = reminders.Count == 0
                ? null
                : string.Join(Environment.NewLine, reminders);
        }
        else
        {
            PenaltyReminder = null;
        }

        if (_achievementProgress is not null)
        {
            var next = await _achievementProgress.GetNextMedalAsync(_sourceType, cancellationToken);
            NextMedalText = next?.BannerText;
            NextMedalPercent = next?.Percent ?? 0;

            var nearest = await _achievementProgress.GetNearestRewardAsync(_sourceType, cancellationToken);
            NearestRewardText = nearest?.BannerText;
            NearestRewardPercent = nearest?.Percent ?? 0;
            NearestRewardImagePath = nearest?.ResolvedImagePath;
            NearestRewardPriceLabel = nearest is { Price: not null }
                ? nearest.PriceLabel
                : null;
        }
        else
        {
            NextMedalText = null;
            NextMedalPercent = 0;
            NearestRewardText = null;
            NearestRewardPercent = 0;
            NearestRewardImagePath = null;
            NearestRewardPriceLabel = null;
        }

        OnPropertyChanged(nameof(LevelText));
        OnPropertyChanged(nameof(LevelTitle));
        OnPropertyChanged(nameof(ProgressText));
    }
}
