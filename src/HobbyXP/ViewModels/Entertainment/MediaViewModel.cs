using System.Collections.ObjectModel;
using HobbyXP.Helpers;
using HobbyXP.Models.Entertainment;
using HobbyXP.Models.Enums;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Messaging;
using HobbyXP.ViewModels.Common;
using HobbyXP.ViewModels.Messaging;

namespace HobbyXP.ViewModels.Entertainment;

public sealed class MediaViewModel : AchievementAwareViewModel
{
    private readonly IMediaService _mediaService;
    private readonly IMessageDialogService _messageDialogService;
    private readonly IProfileRefreshMessenger _profileRefreshMessenger;
    private string _title = string.Empty;
    private MediaType _mediaType = MediaType.Movie;
    private DateTime? _completedDate = DateTime.Today;
    private string _seriesTitle = string.Empty;
    private string _totalChapters = "10";
    private string _searchText = string.Empty;
    private EnumFilterOption<MediaType> _mediaTypeFilterOption;
    private DateTime? _filterFromDate;
    private DateTime? _filterToDate;
    private int _yearlyMovies;
    private int _yearlySeries;
    private int _yearlyTotal;
    private List<MediaEntry> _allHistory = [];
    private List<MediaSeries> _allInProgressSeries = [];

    public MediaViewModel(
        IMediaService mediaService,
        IXpService xpService,
        IWeeklyQuotaService weeklyQuotaService,
        IMessageDialogService messageDialogService,
        IProfileRefreshMessenger profileRefreshMessenger,
        IAchievementMessenger achievementMessenger)
        : base(achievementMessenger)
    {
        _mediaService = mediaService;
        _messageDialogService = messageDialogService;
        _profileRefreshMessenger = profileRefreshMessenger;
        HobbyXp = new HobbyProgressPresenter(xpService, MilestoneSourceType.Media, weeklyQuotaService);
        History = new ObservableCollection<MediaEntry>();
        InProgressSeriesRows = new ObservableCollection<SeriesProgressRowViewModel>();
        MediaTypeFilterOptions = EnumFilterOption<MediaType>.Create(
            "Todos los tipos",
            EntertainmentDisplayLabels.GetMediaType);
        _mediaTypeFilterOption = MediaTypeFilterOptions[0];
        RegisterCommand = new AsyncRelayCommand(RegisterAsync, CanRegister);
        RegisterSeriesCommand = new AsyncRelayCommand(RegisterSeriesAsync, CanRegisterSeries);
        ClearDateFilterCommand = new RelayCommand(ClearHistoryFilters);
        DeleteEntryCommand = new AsyncRelayCommand(p => DeleteEntryAsync(p));
        RefreshRegisterValidation();
        RefreshSeriesValidation();
    }

    public HobbyProgressPresenter HobbyXp { get; }

    public ObservableCollection<MediaEntry> History { get; }

    public ObservableCollection<SeriesProgressRowViewModel> InProgressSeriesRows { get; }

    public Array MediaTypes => Enum.GetValues(typeof(MediaType));

    public IReadOnlyList<EnumFilterOption<MediaType>> MediaTypeFilterOptions { get; }

    public string Title
    {
        get => _title;
        set
        {
            if (SetProperty(ref _title, value))
                RefreshRegisterValidation();
        }
    }

    public MediaType MediaType
    {
        get => _mediaType;
        set => SetProperty(ref _mediaType, value);
    }

    public DateTime? CompletedDate
    {
        get => _completedDate;
        set
        {
            if (SetProperty(ref _completedDate, value))
                RefreshRegisterValidation();
        }
    }

    public string SeriesTitle
    {
        get => _seriesTitle;
        set
        {
            if (SetProperty(ref _seriesTitle, value))
                RefreshSeriesValidation();
        }
    }

    public string TotalChapters
    {
        get => _totalChapters;
        set
        {
            if (SetProperty(ref _totalChapters, value))
                RefreshSeriesValidation();
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                ApplyFilter();
        }
    }

    public EnumFilterOption<MediaType> MediaTypeFilterOption
    {
        get => _mediaTypeFilterOption;
        set
        {
            if (SetProperty(ref _mediaTypeFilterOption, value))
                ApplyFilter();
        }
    }

    public DateTime? FilterFromDate
    {
        get => _filterFromDate;
        set
        {
            if (SetProperty(ref _filterFromDate, value))
                ApplyFilter();
        }
    }

    public DateTime? FilterToDate
    {
        get => _filterToDate;
        set
        {
            if (SetProperty(ref _filterToDate, value))
                ApplyFilter();
        }
    }

    public int YearlyMovies
    {
        get => _yearlyMovies;
        private set => SetProperty(ref _yearlyMovies, value);
    }

    public int YearlySeries
    {
        get => _yearlySeries;
        private set => SetProperty(ref _yearlySeries, value);
    }

    public int YearlyTotal
    {
        get => _yearlyTotal;
        private set => SetProperty(ref _yearlyTotal, value);
    }

    public AsyncRelayCommand RegisterCommand { get; }

    public AsyncRelayCommand RegisterSeriesCommand { get; }

    public RelayCommand ClearDateFilterCommand { get; }

    public AsyncRelayCommand DeleteEntryCommand { get; }

    protected override async Task LoadCoreAsync()
    {
        await HobbyXp.RefreshAsync();
        _allHistory = (await _mediaService.GetHistoryAsync()).ToList();
        _allInProgressSeries = (await _mediaService.GetInProgressSeriesAsync()).ToList();
        ApplyFilter();
        ApplySeriesRows();
        await RefreshCountersAsync();
    }

    private void ApplyFilter()
    {
        History.Clear();
        foreach (var entry in _allHistory.Where(MatchesFilters))
            History.Add(entry);
    }

    private void ApplySeriesRows()
    {
        InProgressSeriesRows.Clear();
        foreach (var series in _allInProgressSeries)
            InProgressSeriesRows.Add(new SeriesProgressRowViewModel(series, LogChaptersAsync));
    }

    private bool MatchesFilters(MediaEntry entry) =>
        TextSearchFilter.Matches(entry.Title, SearchText) &&
        MediaTypeFilterOption.Matches(entry.MediaType) &&
        DateRangeFilter.Matches(entry.CompletedAt, FilterFromDate, FilterToDate);

    private void ClearHistoryFilters()
    {
        _searchText = string.Empty;
        _mediaTypeFilterOption = MediaTypeFilterOptions[0];
        _filterFromDate = null;
        _filterToDate = null;
        OnPropertyChanged(nameof(SearchText));
        OnPropertyChanged(nameof(MediaTypeFilterOption));
        OnPropertyChanged(nameof(FilterFromDate));
        OnPropertyChanged(nameof(FilterToDate));
        ApplyFilter();
    }

    private async Task RefreshCountersAsync()
    {
        var counters = await _mediaService.GetYearlyCountersAsync();
        YearlyMovies = counters.MoviesCount;
        YearlySeries = counters.SeriesCount;
        YearlyTotal = counters.TotalCount;
    }

    private ValidationResult ValidateRegisterForm() =>
        FormValidation.FirstFailure(
            FormValidation.RequireText(Title, "el título"),
            CompletedDate.HasValue
                ? ValidationResult.Ok()
                : ValidationResult.Fail("Indique la fecha de finalización."));

    private ValidationResult ValidateSeriesForm() =>
        FormValidation.FirstFailure(
            FormValidation.RequireText(SeriesTitle, "el título de la serie"),
            FormValidation.RequirePositiveInt(TotalChapters, "Los capítulos totales", out _));

    private void RefreshRegisterValidation() =>
        RefreshValidation(ValidateRegisterForm(), RegisterCommand);

    private void RefreshSeriesValidation()
    {
        // Banner principal queda para el formulario de obra terminada; serie tiene su propia validación en CanExecute.
        RegisterSeriesCommand.RaiseCanExecuteChanged();
    }

    private bool CanRegister() => ValidateRegisterForm().IsValid;

    private bool CanRegisterSeries() => ValidateSeriesForm().IsValid;

    private async Task RegisterAsync()
    {
        if (!ValidateRegisterForm().IsValid)
        {
            RefreshRegisterValidation();
            return;
        }

        await RunBusyAsync(async () =>
        {
            var completedAt = DateTimeHelper.ToUtcFromLocalDate(CompletedDate ?? DateTime.Today);
            var result = await _mediaService.RegisterCompletedAsync(Title, MediaType, completedAt);
            PublishAchievements(result.Events);
            await HobbyXp.RefreshAsync();

            _allHistory.Insert(0, result.Value);
            ApplyFilter();
            await RefreshCountersAsync();

            Title = string.Empty;
            CompletedDate = DateTime.Today;
            ClearValidation();
            StatusMessage = $"Obra registrada · +{result.Value.XpEarned} XP";
        }, "Registrando obra...");
    }

    private async Task RegisterSeriesAsync()
    {
        if (!ValidateSeriesForm().IsValid)
            return;

        var chapters = int.Parse(TotalChapters);
        await RunBusyAsync(async () =>
        {
            var series = await _mediaService.RegisterSeriesAsync(SeriesTitle, chapters);
            _allInProgressSeries.Insert(0, series);
            ApplySeriesRows();

            SeriesTitle = string.Empty;
            TotalChapters = "10";
            StatusMessage = $"Serie «{series.Title}» agregada ({series.TotalChapters} capítulos).";
        }, "Agregando serie...");
    }

    private async Task LogChaptersAsync(MediaSeries series, DateTime watchDate, int chaptersDone)
    {
        await RunBusyAsync(async () =>
        {
            var result = await _mediaService.LogChaptersAsync(series.Id, watchDate, chaptersDone);
            PublishAchievements(result.Events);
            await HobbyXp.RefreshAsync();

            _allInProgressSeries = (await _mediaService.GetInProgressSeriesAsync()).ToList();
            ApplySeriesRows();

            if (result.Value.Status == MediaSeriesStatus.Completed)
            {
                _allHistory = (await _mediaService.GetHistoryAsync()).ToList();
                ApplyFilter();
                await RefreshCountersAsync();
            }

            _profileRefreshMessenger.RequestRefresh();

            var xpGained = result.Events.Sum(e => e.PointsEarned);
            StatusMessage = xpGained > 0
                ? $"{result.Value.Title}: {result.Value.ChaptersWatched}/{result.Value.TotalChapters} capítulos · +{xpGained} XP"
                : $"{result.Value.Title}: {result.Value.ChaptersWatched}/{result.Value.TotalChapters} capítulos";
        }, "Registrando capítulos...");
    }

    private async Task DeleteEntryAsync(object? parameter)
    {
        if (parameter is not MediaEntry entry)
            return;

        if (!_messageDialogService.Confirm(
                $"¿Eliminar «{entry.Title}» del historial?\nSe revertirá el XP asociado.",
                "Eliminar del historial"))
            return;

        await RunBusyAsync(async () =>
        {
            if (!await _mediaService.DeleteAsync(entry.Id))
                return;

            _allHistory.RemoveAll(e => e.Id == entry.Id);
            ApplyFilter();
            await RefreshCountersAsync();
            await HobbyXp.RefreshAsync();
            _profileRefreshMessenger.RequestRefresh();
            StatusMessage = $"«{entry.Title}» eliminado del historial.";
        }, "Eliminando obra...");
    }
}
