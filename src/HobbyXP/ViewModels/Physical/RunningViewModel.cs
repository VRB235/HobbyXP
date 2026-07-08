using System.Collections.ObjectModel;
using HobbyXP.Models.Physical;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Results;
using HobbyXP.ViewModels.Common;
using HobbyXP.ViewModels.Messaging;

namespace HobbyXP.ViewModels.Physical;

public sealed class RunningViewModel : AchievementAwareViewModel
{
    private readonly IRunningService _runningService;
    private string _distanceKm = string.Empty;
    private string _durationMinutes = string.Empty;
    private string _durationSeconds = string.Empty;
    private RaceOption? _selectedRaceOption;
    private OfficialRace? _selectedRace;
    private RacePreparationStats? _selectedRaceStats;

    public RunningViewModel(IRunningService runningService, IAchievementMessenger achievementMessenger)
        : base(achievementMessenger)
    {
        _runningService = runningService;
        Sessions = new ObservableCollection<RunningSession>();
        OfficialRaces = new ObservableCollection<OfficialRace>();
        RaceOptions = new ObservableCollection<RaceOption> { RaceOption.None };

        SaveSessionCommand = new AsyncRelayCommand(SaveSessionAsync, CanSaveSession);
        CompleteRaceCommand = new AsyncRelayCommand(CompleteRaceAsync, () => SelectedRace is { IsCompleted: false });
    }

    public ObservableCollection<RunningSession> Sessions { get; }

    public ObservableCollection<OfficialRace> OfficialRaces { get; }

    public ObservableCollection<RaceOption> RaceOptions { get; }

    public string DistanceKm
    {
        get => _distanceKm;
        set => SetProperty(ref _distanceKm, value);
    }

    public string DurationMinutes
    {
        get => _durationMinutes;
        set => SetProperty(ref _durationMinutes, value);
    }

    public string DurationSeconds
    {
        get => _durationSeconds;
        set => SetProperty(ref _durationSeconds, value);
    }

    public RaceOption? SelectedRaceOption
    {
        get => _selectedRaceOption;
        set => SetProperty(ref _selectedRaceOption, value);
    }

    public OfficialRace? SelectedRace
    {
        get => _selectedRace;
        set
        {
            if (!SetProperty(ref _selectedRace, value))
                return;

            _ = LoadRaceStatsAsync();
        }
    }

    public RacePreparationStats? SelectedRaceStats
    {
        get => _selectedRaceStats;
        private set => SetProperty(ref _selectedRaceStats, value);
    }

    public AsyncRelayCommand SaveSessionCommand { get; }

    public AsyncRelayCommand CompleteRaceCommand { get; }

    protected override async Task LoadCoreAsync()
    {
        var sessions = await _runningService.GetSessionsAsync();
        var races = await _runningService.GetOfficialRacesAsync();

        Sessions.Clear();
        foreach (var session in sessions)
            Sessions.Add(session);

        OfficialRaces.Clear();
        foreach (var race in races)
            OfficialRaces.Add(race);

        RaceOptions.Clear();
        RaceOptions.Add(RaceOption.None);
        foreach (var race in races)
            RaceOptions.Add(new RaceOption { Id = race.Id, Name = race.Name });

        SelectedRaceOption ??= RaceOption.None;
    }

    private bool CanSaveSession() =>
        decimal.TryParse(DistanceKm, out var km) && km > 0 &&
        int.TryParse(DurationMinutes, out var min) && min >= 0 &&
        int.TryParse(DurationSeconds, out var sec) && sec >= 0 &&
        (min > 0 || sec > 0);

    private async Task SaveSessionAsync()
    {
        if (!CanSaveSession())
            return;

        var distance = decimal.Parse(DistanceKm);
        var duration = new TimeSpan(0, int.Parse(DurationMinutes), int.Parse(DurationSeconds));
        var raceId = SelectedRaceOption?.Id;

        await RunBusyAsync(async () =>
        {
            var result = await _runningService.SaveSessionAsync(distance, duration, raceId);
            PublishAchievements(result.Events);
            Sessions.Insert(0, result.Value);

            DistanceKm = string.Empty;
            DurationMinutes = string.Empty;
            DurationSeconds = string.Empty;
            StatusMessage = $"Sesión guardada · Ritmo: {result.Value.PaceMinPerKm:0.00} min/km · +{result.Value.XpEarned} XP";
        }, "Guardando sesión...");
    }

    private async Task CompleteRaceAsync()
    {
        if (SelectedRace is null || SelectedRace.IsCompleted)
            return;

        await RunBusyAsync(async () =>
        {
            var result = await _runningService.CompleteOfficialRaceAsync(SelectedRace.Id);
            PublishAchievements(result.Events);

            var index = OfficialRaces.IndexOf(SelectedRace);
            if (index >= 0)
                OfficialRaces[index] = result.Value;

            SelectedRace = result.Value;
            StatusMessage = $"¡Carrera completada! +{result.Value.BonusXpAwarded} XP";
        }, "Completando carrera...");
    }

    private async Task LoadRaceStatsAsync()
    {
        if (SelectedRace is null)
        {
            SelectedRaceStats = null;
            return;
        }

        SelectedRaceStats = await _runningService.GetRacePreparationStatsAsync(SelectedRace.Id);
    }
}
