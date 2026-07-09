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
    private DateTime? _filterFromDate;
    private DateTime? _filterToDate;
    private int _yearlyMovies;
    private int _yearlySeries;
    private int _yearlyTotal;
    private List<MediaEntry> _allHistory = [];

    public MediaViewModel(
        IMediaService mediaService,
        IMessageDialogService messageDialogService,
        IProfileRefreshMessenger profileRefreshMessenger,
        IAchievementMessenger achievementMessenger)
        : base(achievementMessenger)
    {
        _mediaService = mediaService;
        _messageDialogService = messageDialogService;
        _profileRefreshMessenger = profileRefreshMessenger;
        History = new ObservableCollection<MediaEntry>();
        RegisterCommand = new AsyncRelayCommand(RegisterAsync, () => !string.IsNullOrWhiteSpace(Title));
        ClearDateFilterCommand = new RelayCommand(ClearDateFilter);
        DeleteEntryCommand = new AsyncRelayCommand(p => DeleteEntryAsync(p));
    }

    public ObservableCollection<MediaEntry> History { get; }

    public Array MediaTypes => Enum.GetValues(typeof(MediaType));

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public MediaType MediaType
    {
        get => _mediaType;
        set => SetProperty(ref _mediaType, value);
    }

    public DateTime? CompletedDate
    {
        get => _completedDate;
        set => SetProperty(ref _completedDate, value);
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

    public RelayCommand ClearDateFilterCommand { get; }

    public AsyncRelayCommand DeleteEntryCommand { get; }

    protected override async Task LoadCoreAsync()
    {
        _allHistory = (await _mediaService.GetHistoryAsync()).ToList();
        ApplyFilter();
        await RefreshCountersAsync();
    }

    private void ApplyFilter()
    {
        History.Clear();
        foreach (var entry in _allHistory.Where(e => DateRangeFilter.Matches(e.CompletedAt, FilterFromDate, FilterToDate)))
            History.Add(entry);
    }

    private void ClearDateFilter()
    {
        _filterFromDate = null;
        _filterToDate = null;
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

    private async Task RegisterAsync()
    {
        await RunBusyAsync(async () =>
        {
            var completedAt = DateTimeHelper.ToUtcFromLocalDate(CompletedDate ?? DateTime.Today);
            var result = await _mediaService.RegisterCompletedAsync(Title, MediaType, completedAt);
            PublishAchievements(result.Events);

            _allHistory.Insert(0, result.Value);
            ApplyFilter();
            await RefreshCountersAsync();

            Title = string.Empty;
            CompletedDate = DateTime.Today;
            StatusMessage = $"Obra registrada · +{result.Value.XpEarned} XP";
        }, "Registrando obra...");
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
            _profileRefreshMessenger.RequestRefresh();
            StatusMessage = $"«{entry.Title}» eliminado del historial.";
        }, "Eliminando obra...");
    }
}
