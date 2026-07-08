using System.Collections.ObjectModel;
using HobbyXP.Models.Entertainment;
using HobbyXP.Models.Enums;
using HobbyXP.Services.Abstractions;
using HobbyXP.ViewModels.Common;
using HobbyXP.ViewModels.Messaging;

namespace HobbyXP.ViewModels.Entertainment;

public sealed class MediaViewModel : AchievementAwareViewModel
{
    private readonly IMediaService _mediaService;
    private string _title = string.Empty;
    private MediaType _mediaType = MediaType.Movie;
    private int _yearlyMovies;
    private int _yearlySeries;
    private int _yearlyTotal;

    public MediaViewModel(IMediaService mediaService, IAchievementMessenger achievementMessenger)
        : base(achievementMessenger)
    {
        _mediaService = mediaService;
        History = new ObservableCollection<MediaEntry>();
        RegisterCommand = new AsyncRelayCommand(RegisterAsync, () => !string.IsNullOrWhiteSpace(Title));
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

    protected override async Task LoadCoreAsync()
    {
        var history = await _mediaService.GetHistoryAsync();
        var counters = await _mediaService.GetYearlyCountersAsync();

        History.Clear();
        foreach (var entry in history)
            History.Add(entry);

        YearlyMovies = counters.MoviesCount;
        YearlySeries = counters.SeriesCount;
        YearlyTotal = counters.TotalCount;
    }

    private async Task RegisterAsync()
    {
        await RunBusyAsync(async () =>
        {
            var result = await _mediaService.RegisterCompletedAsync(Title, MediaType);
            PublishAchievements(result.Events);
            History.Insert(0, result.Value);

            var counters = await _mediaService.GetYearlyCountersAsync();
            YearlyMovies = counters.MoviesCount;
            YearlySeries = counters.SeriesCount;
            YearlyTotal = counters.TotalCount;

            Title = string.Empty;
            StatusMessage = $"Obra registrada · +{result.Value.XpEarned} XP";
        }, "Registrando obra...");
    }
}
