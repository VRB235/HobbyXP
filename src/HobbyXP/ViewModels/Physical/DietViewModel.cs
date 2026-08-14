using System.Collections.ObjectModel;
using HobbyXP.Helpers;
using HobbyXP.Models.Enums;
using HobbyXP.Models.Physical;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Messaging;
using HobbyXP.ViewModels.Common;
using HobbyXP.ViewModels.Messaging;

namespace HobbyXP.ViewModels.Physical;

public sealed class DietViewModel : AchievementAwareViewModel
{
    private readonly IDietService _dietService;
    private readonly IMessageDialogService _messageDialogService;
    private readonly IProfileRefreshMessenger _profileRefreshMessenger;
    private DateTime? _selectedDate = DateTime.Today;
    private DateTime? _historyFromDate;
    private DateTime? _historyToDate;
    private DietDayLog? _selectedLog;
    private List<DietDayLog> _allLogs = [];
    private bool _isTodayExpanded = true;
    private bool _isHistoryExpanded;
    private bool _suppressSectionAccordion;
    private bool _suppressMealChanged;
    private string _daySummary = DietMealLabels.DayKind(0);
    private string _scoreSummary = DietMealLabels.Score(0);

    public DietViewModel(
        IDietService dietService,
        IXpService xpService,
        IWeeklyQuotaService weeklyQuotaService,
        IMessageDialogService messageDialogService,
        IProfileRefreshMessenger profileRefreshMessenger,
        IAchievementMessenger achievementMessenger)
        : base(achievementMessenger)
    {
        _dietService = dietService;
        _messageDialogService = messageDialogService;
        _profileRefreshMessenger = profileRefreshMessenger;
        HobbyXp = new HobbyProgressPresenter(xpService, MilestoneSourceType.Diet, weeklyQuotaService);
        History = new ObservableCollection<DietDayLog>();
        Meals =
        [
            new DietMealRowViewModel(DietMealSlot.Breakfast, OnMealChanged),
            new DietMealRowViewModel(DietMealSlot.Lunch, OnMealChanged),
            new DietMealRowViewModel(DietMealSlot.Dinner, OnMealChanged),
            new DietMealRowViewModel(DietMealSlot.Snack, OnMealChanged)
        ];

        SaveDayCommand = new AsyncRelayCommand(SaveDayAsync, CanSaveDay);
        ClearHistoryDateFilterCommand = new RelayCommand(ClearHistoryDateFilter);
        DeleteDayCommand = new AsyncRelayCommand(p => DeleteDayAsync(p));
        RefreshDaySummary();
        RefreshDayValidation();
    }

    public HobbyProgressPresenter HobbyXp { get; }

    public IReadOnlyList<DietMealRowViewModel> Meals { get; }

    public ObservableCollection<DietDayLog> History { get; }

    public DietDayLog? SelectedLog
    {
        get => _selectedLog;
        set => SetProperty(ref _selectedLog, value);
    }

    public DateTime? SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (!SetProperty(ref _selectedDate, value))
                return;

            RefreshDayValidation();
            _ = LoadSelectedDayAsync();
        }
    }

    public DateTime? HistoryFromDate
    {
        get => _historyFromDate;
        set
        {
            if (SetProperty(ref _historyFromDate, value))
                ApplyHistoryFilter();
        }
    }

    public DateTime? HistoryToDate
    {
        get => _historyToDate;
        set
        {
            if (SetProperty(ref _historyToDate, value))
                ApplyHistoryFilter();
        }
    }

    public bool IsTodayExpanded
    {
        get => _isTodayExpanded;
        set => SetSectionExpanded(DietSection.Today, value, ref _isTodayExpanded, nameof(IsTodayExpanded));
    }

    public bool IsHistoryExpanded
    {
        get => _isHistoryExpanded;
        set => SetSectionExpanded(DietSection.History, value, ref _isHistoryExpanded, nameof(IsHistoryExpanded));
    }

    public string DaySummary
    {
        get => _daySummary;
        private set => SetProperty(ref _daySummary, value);
    }

    public string ScoreSummary
    {
        get => _scoreSummary;
        private set => SetProperty(ref _scoreSummary, value);
    }

    public AsyncRelayCommand SaveDayCommand { get; }

    public RelayCommand ClearHistoryDateFilterCommand { get; }

    public AsyncRelayCommand DeleteDayCommand { get; }

    protected override async Task LoadCoreAsync()
    {
        await HobbyXp.RefreshAsync();
        await LoadHistoryAsync();
        await LoadSelectedDayAsync();
    }

    private async Task LoadHistoryAsync()
    {
        _allLogs = (await _dietService.GetHistoryAsync()).ToList();
        ApplyHistoryFilter();
    }

    private async Task LoadSelectedDayAsync()
    {
        if (!SelectedDate.HasValue)
        {
            ApplyLogToMeals(null);
            return;
        }

        var log = await _dietService.GetByLocalDateAsync(SelectedDate.Value);
        ApplyLogToMeals(log);
    }

    private void ApplyLogToMeals(DietDayLog? log)
    {
        _suppressMealChanged = true;
        GetMeal(DietMealSlot.Breakfast).SetStatusSilent(log?.BreakfastStatus ?? DietMealStatus.Unlogged);
        GetMeal(DietMealSlot.Lunch).SetStatusSilent(log?.LunchStatus ?? DietMealStatus.Unlogged);
        GetMeal(DietMealSlot.Dinner).SetStatusSilent(log?.DinnerStatus ?? DietMealStatus.Unlogged);
        GetMeal(DietMealSlot.Snack).SetStatusSilent(log?.SnackStatus ?? DietMealStatus.Unlogged);
        _suppressMealChanged = false;
        RefreshDaySummary();
        RefreshDayValidation();
    }

    private DietMealRowViewModel GetMeal(DietMealSlot slot) =>
        Meals.First(m => m.Slot == slot);

    private void OnMealChanged()
    {
        if (_suppressMealChanged)
            return;

        RefreshDaySummary();
        RefreshDayValidation();
    }

    private int CurrentOnPlanCount() =>
        DietDayRules.OnPlanCount(
            GetMeal(DietMealSlot.Breakfast).Status,
            GetMeal(DietMealSlot.Lunch).Status,
            GetMeal(DietMealSlot.Dinner).Status,
            GetMeal(DietMealSlot.Snack).Status);

    private void RefreshDaySummary()
    {
        var count = CurrentOnPlanCount();
        ScoreSummary = DietMealLabels.Score(count);
        DaySummary = DietMealLabels.DayKind(count);
    }

    private ValidationResult ValidateDayForm()
    {
        if (!SelectedDate.HasValue)
            return ValidationResult.Fail("Indique la fecha del día.");

        if (!DietDayRules.HasAnyLoggedMeal(
                GetMeal(DietMealSlot.Breakfast).Status,
                GetMeal(DietMealSlot.Lunch).Status,
                GetMeal(DietMealSlot.Dinner).Status,
                GetMeal(DietMealSlot.Snack).Status))
        {
            return ValidationResult.Fail("Marque al menos una comida (En plan o Fuera de plan).");
        }

        return ValidationResult.Ok();
    }

    private void RefreshDayValidation() =>
        RefreshValidation(ValidateDayForm(), SaveDayCommand);

    private bool CanSaveDay() => ValidateDayForm().IsValid;

    private async Task SaveDayAsync()
    {
        if (!ValidateDayForm().IsValid)
        {
            RefreshDayValidation();
            return;
        }

        await RunBusyAsync(async () =>
        {
            var draft = new DietDayDraft(
                SelectedDate ?? DateTime.Today,
                GetMeal(DietMealSlot.Breakfast).Status,
                GetMeal(DietMealSlot.Lunch).Status,
                GetMeal(DietMealSlot.Dinner).Status,
                GetMeal(DietMealSlot.Snack).Status,
                Notes: null);

            var result = await _dietService.SaveDayAsync(draft);
            PublishAchievements(result.Events);
            await HobbyXp.RefreshAsync();
            _profileRefreshMessenger.RequestRefresh();

            _historyFromDate = null;
            _historyToDate = null;
            OnPropertyChanged(nameof(HistoryFromDate));
            OnPropertyChanged(nameof(HistoryToDate));

            await LoadHistoryAsync();
            SelectedLog = History.FirstOrDefault(d => d.Id == result.Value.Id) ?? History.FirstOrDefault();
            ApplyLogToMeals(result.Value);
            ClearValidation();
            IsHistoryExpanded = true;

            var kind = DietMealLabels.DayKind(result.Value.OnPlanCount);
            StatusMessage =
                $"Dieta {result.Value.DayDate:dd/MM/yyyy} · {result.Value.ScoreLabel} · {kind} · +{result.Value.XpEarned} XP";
        }, "Guardando día de dieta...");
    }

    private async Task DeleteDayAsync(object? parameter)
    {
        if (parameter is not DietDayLog log)
            return;

        if (!_messageDialogService.Confirm(
                $"¿Eliminar el registro de dieta del {log.DayDate:dd/MM/yyyy}?\nSe revertirá el XP asociado.",
                "Eliminar del historial"))
            return;

        await RunBusyAsync(async () =>
        {
            if (!await _dietService.DeleteDayAsync(log.Id))
                return;

            _allLogs.RemoveAll(d => d.Id == log.Id);
            ApplyHistoryFilter();
            await HobbyXp.RefreshAsync();
            _profileRefreshMessenger.RequestRefresh();

            if (SelectedDate.HasValue &&
                DateTimeHelper.ToUtcFromLocalDate(SelectedDate.Value) == log.DayDate)
            {
                ApplyLogToMeals(null);
            }

            StatusMessage = "Día de dieta eliminado del historial.";
        }, "Eliminando día de dieta...");
    }

    private void ClearHistoryDateFilter()
    {
        _historyFromDate = null;
        _historyToDate = null;
        OnPropertyChanged(nameof(HistoryFromDate));
        OnPropertyChanged(nameof(HistoryToDate));
        ApplyHistoryFilter();
    }

    private void ApplyHistoryFilter()
    {
        var selectedId = SelectedLog?.Id;
        History.Clear();
        foreach (var log in _allLogs.Where(MatchesHistoryFilters))
            History.Add(log);

        SelectedLog = selectedId.HasValue
            ? History.FirstOrDefault(d => d.Id == selectedId.Value)
            : History.FirstOrDefault();
    }

    private bool MatchesHistoryFilters(DietDayLog log)
    {
        if (HistoryFromDate.HasValue && log.DayDate < DateTimeHelper.ToUtcFromLocalDate(HistoryFromDate.Value))
            return false;
        if (HistoryToDate.HasValue && log.DayDate > DateTimeHelper.ToUtcFromLocalDate(HistoryToDate.Value))
            return false;
        return true;
    }

    private void SetSectionExpanded(DietSection section, bool value, ref bool field, string propertyName)
    {
        if (_suppressSectionAccordion)
        {
            field = value;
            OnPropertyChanged(propertyName);
            return;
        }

        if (!SetProperty(ref field, value, propertyName) || !value)
            return;

        _suppressSectionAccordion = true;
        try
        {
            if (section != DietSection.Today && _isTodayExpanded)
            {
                _isTodayExpanded = false;
                OnPropertyChanged(nameof(IsTodayExpanded));
            }

            if (section != DietSection.History && _isHistoryExpanded)
            {
                _isHistoryExpanded = false;
                OnPropertyChanged(nameof(IsHistoryExpanded));
            }
        }
        finally
        {
            _suppressSectionAccordion = false;
        }
    }

    private enum DietSection
    {
        Today,
        History
    }
}
