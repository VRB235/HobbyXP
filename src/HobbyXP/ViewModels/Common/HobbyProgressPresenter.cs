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
    private readonly MilestoneSourceType _sourceType;
    private string _title;
    private int _currentLevel = 1;
    private int _totalXp;
    private int _xpIntoCurrentLevel;
    private int _xpRequiredForNextLevel = 1;
    private double _progressPercentage;
    private string? _penaltyReminder;

    public HobbyProgressPresenter(
        IXpService xpService,
        MilestoneSourceType sourceType,
        IWeeklyQuotaService? weeklyQuotaService = null)
    {
        _xpService = xpService;
        _weeklyQuotaService = weeklyQuotaService;
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

        OnPropertyChanged(nameof(LevelText));
        OnPropertyChanged(nameof(LevelTitle));
        OnPropertyChanged(nameof(ProgressText));
    }
}
