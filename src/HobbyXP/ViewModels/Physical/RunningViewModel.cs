using System.Collections.ObjectModel;
using HobbyXP.Helpers;
using HobbyXP.Models.Physical;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Messaging;
using HobbyXP.Services.Results;
using HobbyXP.ViewModels.Common;
using HobbyXP.ViewModels.Messaging;

namespace HobbyXP.ViewModels.Physical;

public sealed class RunningViewModel : AchievementAwareViewModel
{
    private readonly IRunningService _runningService;
    private readonly IMessageDialogService _messageDialogService;
    private readonly IProfileRefreshMessenger _profileRefreshMessenger;
    private string _distanceKm = string.Empty;
    private string _durationMinutes = string.Empty;
    private string _durationSeconds = string.Empty;
    private RaceOption? _selectedRaceOption;
    private OfficialRace? _selectedRace;
    private RacePreparationStats? _selectedRaceStats;
    private string _newRaceName = string.Empty;
    private string _newRaceDistanceKm = string.Empty;
    private DateTime? _newRaceEventDate;
    private string _newRaceLocation = string.Empty;
    private DateTime? _sessionsFromDate;
    private DateTime? _sessionsToDate;
    private List<RunningSession> _allSessions = [];
    private List<OfficialRace> _allOfficialRaces = [];
    private string _raceSearchText = string.Empty;
    private DateTime? _racesFromDate;
    private DateTime? _racesToDate;
    private string? _sessionValidationMessage;
    private string? _raceValidationMessage;

    public RunningViewModel(
        IRunningService runningService,
        IMessageDialogService messageDialogService,
        IProfileRefreshMessenger profileRefreshMessenger,
        IAchievementMessenger achievementMessenger)
        : base(achievementMessenger)
    {
        _runningService = runningService;
        _messageDialogService = messageDialogService;
        _profileRefreshMessenger = profileRefreshMessenger;
        Sessions = new ObservableCollection<RunningSession>();
        OfficialRaces = new ObservableCollection<OfficialRace>();
        RaceOptions = new ObservableCollection<RaceOption> { RaceOption.None };

        SaveSessionCommand = new AsyncRelayCommand(SaveSessionAsync, CanSaveSession);
        RegisterOfficialRaceCommand = new AsyncRelayCommand(RegisterOfficialRaceAsync, CanRegisterOfficialRace);
        CompleteRaceCommand = new AsyncRelayCommand(CompleteRaceAsync, () => SelectedRace is { IsCompleted: false });
        ClearSessionsDateFilterCommand = new RelayCommand(ClearSessionsDateFilter);
        ClearOfficialRacesFilterCommand = new RelayCommand(ClearOfficialRacesFilter);
        DeleteSessionCommand = new AsyncRelayCommand(p => DeleteSessionAsync(p));
        DeleteOfficialRaceCommand = new AsyncRelayCommand(
            p => DeleteOfficialRaceAsync(p),
            p => p is OfficialRace || SelectedRace is not null);
        RefreshSessionValidation();
        RefreshRaceValidation();
    }

    public string? SessionValidationMessage
    {
        get => _sessionValidationMessage;
        private set => SetProperty(ref _sessionValidationMessage, value);
    }

    public string? RaceValidationMessage
    {
        get => _raceValidationMessage;
        private set => SetProperty(ref _raceValidationMessage, value);
    }

    public ObservableCollection<RunningSession> Sessions { get; }

    public ObservableCollection<OfficialRace> OfficialRaces { get; }

    public ObservableCollection<RaceOption> RaceOptions { get; }

    public string DistanceKm
    {
        get => _distanceKm;
        set
        {
            if (SetProperty(ref _distanceKm, value))
                RefreshSessionValidation();
        }
    }

    public string DurationMinutes
    {
        get => _durationMinutes;
        set
        {
            if (SetProperty(ref _durationMinutes, value))
                RefreshSessionValidation();
        }
    }

    public string DurationSeconds
    {
        get => _durationSeconds;
        set
        {
            if (SetProperty(ref _durationSeconds, value))
                RefreshSessionValidation();
        }
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

            DeleteOfficialRaceCommand.RaiseCanExecuteChanged();
            _ = LoadRaceStatsAsync();
        }
    }

    public RacePreparationStats? SelectedRaceStats
    {
        get => _selectedRaceStats;
        private set => SetProperty(ref _selectedRaceStats, value);
    }

    public string NewRaceName
    {
        get => _newRaceName;
        set
        {
            if (SetProperty(ref _newRaceName, value))
                RefreshRaceValidation();
        }
    }

    public string NewRaceDistanceKm
    {
        get => _newRaceDistanceKm;
        set
        {
            if (SetProperty(ref _newRaceDistanceKm, value))
                RefreshRaceValidation();
        }
    }

    public DateTime? NewRaceEventDate
    {
        get => _newRaceEventDate;
        set => SetProperty(ref _newRaceEventDate, value);
    }

    public string NewRaceLocation
    {
        get => _newRaceLocation;
        set => SetProperty(ref _newRaceLocation, value);
    }

    public DateTime? SessionsFromDate
    {
        get => _sessionsFromDate;
        set
        {
            if (SetProperty(ref _sessionsFromDate, value))
                ApplySessionsFilter();
        }
    }

    public DateTime? SessionsToDate
    {
        get => _sessionsToDate;
        set
        {
            if (SetProperty(ref _sessionsToDate, value))
                ApplySessionsFilter();
        }
    }

    public string RaceSearchText
    {
        get => _raceSearchText;
        set
        {
            if (SetProperty(ref _raceSearchText, value))
                ApplyOfficialRacesFilter();
        }
    }

    public DateTime? RacesFromDate
    {
        get => _racesFromDate;
        set
        {
            if (SetProperty(ref _racesFromDate, value))
                ApplyOfficialRacesFilter();
        }
    }

    public DateTime? RacesToDate
    {
        get => _racesToDate;
        set
        {
            if (SetProperty(ref _racesToDate, value))
                ApplyOfficialRacesFilter();
        }
    }

    public AsyncRelayCommand SaveSessionCommand { get; }

    public AsyncRelayCommand RegisterOfficialRaceCommand { get; }

    public AsyncRelayCommand CompleteRaceCommand { get; }

    public RelayCommand ClearSessionsDateFilterCommand { get; }

    public RelayCommand ClearOfficialRacesFilterCommand { get; }

    public AsyncRelayCommand DeleteSessionCommand { get; }

    public AsyncRelayCommand DeleteOfficialRaceCommand { get; }

    protected override async Task LoadCoreAsync()
    {
        _allSessions = (await _runningService.GetSessionsAsync()).ToList();
        ApplySessionsFilter();

        var races = await _runningService.GetOfficialRacesAsync();
        SyncRaceCollections(races);
        SelectedRaceOption ??= RaceOption.None;
    }

    private void ApplySessionsFilter()
    {
        Sessions.Clear();
        foreach (var session in _allSessions.Where(s => DateRangeFilter.Matches(s.RecordedAt, SessionsFromDate, SessionsToDate)))
            Sessions.Add(session);
    }

    private void ClearSessionsDateFilter()
    {
        _sessionsFromDate = null;
        _sessionsToDate = null;
        OnPropertyChanged(nameof(SessionsFromDate));
        OnPropertyChanged(nameof(SessionsToDate));
        ApplySessionsFilter();
    }

    private void ClearOfficialRacesFilter()
    {
        _raceSearchText = string.Empty;
        _racesFromDate = null;
        _racesToDate = null;
        OnPropertyChanged(nameof(RaceSearchText));
        OnPropertyChanged(nameof(RacesFromDate));
        OnPropertyChanged(nameof(RacesToDate));
        ApplyOfficialRacesFilter();
    }

    private void SyncRaceCollections(IEnumerable<OfficialRace> races)
    {
        _allOfficialRaces = races.ToList();
        ApplyOfficialRacesFilter();
        SyncRaceOptions();
    }

    private void SyncRaceOptions()
    {
        RaceOptions.Clear();
        RaceOptions.Add(RaceOption.None);
        foreach (var race in _allOfficialRaces)
            RaceOptions.Add(new RaceOption { Id = race.Id, Name = race.Name });
    }

    private void ApplyOfficialRacesFilter()
    {
        var selectedId = SelectedRace?.Id;

        OfficialRaces.Clear();
        foreach (var race in _allOfficialRaces.Where(MatchesOfficialRaceFilter))
            OfficialRaces.Add(race);

        var nextSelection = selectedId.HasValue
            ? OfficialRaces.FirstOrDefault(r => r.Id == selectedId.Value)
            : OfficialRaces.FirstOrDefault();

        if (!ReferenceEquals(SelectedRace, nextSelection))
            SelectedRace = nextSelection;
        else if (nextSelection is null)
            SelectedRaceStats = null;
    }

    private bool MatchesOfficialRaceFilter(OfficialRace race)
    {
        if (!string.IsNullOrWhiteSpace(RaceSearchText))
        {
            var term = RaceSearchText.Trim();
            var matchesName = race.Name.Contains(term, StringComparison.OrdinalIgnoreCase);
            var matchesLocation = race.Location?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false;
            if (!matchesName && !matchesLocation)
                return false;
        }

        if (RacesFromDate is null && RacesToDate is null)
            return true;

        if (race.EventDate is null)
            return false;

        return DateRangeFilter.Matches(race.EventDate.Value, RacesFromDate, RacesToDate);
    }

    private void UpdateOfficialRace(OfficialRace race)
    {
        var index = _allOfficialRaces.FindIndex(r => r.Id == race.Id);
        if (index >= 0)
            _allOfficialRaces[index] = race;

        ApplyOfficialRacesFilter();
    }

    private ValidationResult ValidateSessionForm()
    {
        var distance = FormValidation.RequirePositiveDecimal(DistanceKm, "La distancia (km)", out _);
        if (!distance.IsValid)
            return distance;

        var minutes = FormValidation.RequireNonNegativeInt(DurationMinutes, "Los minutos", out var min);
        if (!minutes.IsValid)
            return minutes;

        var seconds = FormValidation.RequireIntInRange(DurationSeconds, "Los segundos", 0, 59, out var sec);
        if (!seconds.IsValid)
            return seconds;

        return min == 0 && sec == 0
            ? ValidationResult.Fail("Indique una duración mayor que cero.")
            : ValidationResult.Ok();
    }

    private ValidationResult ValidateRaceForm() =>
        FormValidation.FirstFailure(
            FormValidation.RequireText(NewRaceName, "el nombre de la carrera"),
            FormValidation.RequirePositiveDecimal(NewRaceDistanceKm, "La distancia (km)", out _));

    private void RefreshSessionValidation()
    {
        var result = ValidateSessionForm();
        SessionValidationMessage = result.IsValid ? null : result.Message;
        SaveSessionCommand.RaiseCanExecuteChanged();
    }

    private void RefreshRaceValidation()
    {
        var result = ValidateRaceForm();
        RaceValidationMessage = result.IsValid ? null : result.Message;
        RegisterOfficialRaceCommand.RaiseCanExecuteChanged();
    }

    private bool CanRegisterOfficialRace() => ValidateRaceForm().IsValid;

    private async Task RegisterOfficialRaceAsync()
    {
        if (!ValidateRaceForm().IsValid)
        {
            RefreshRaceValidation();
            return;
        }

        FormValidation.RequirePositiveDecimal(NewRaceDistanceKm, "La distancia (km)", out var distanceKm);

        await RunBusyAsync(async () =>
        {
            var race = new OfficialRace
            {
                Name = NewRaceName.Trim(),
                DistanceKm = distanceKm,
                EventDate = NewRaceEventDate.HasValue
                    ? DateTime.SpecifyKind(NewRaceEventDate.Value.Date, DateTimeKind.Utc)
                    : null,
                Location = string.IsNullOrWhiteSpace(NewRaceLocation) ? null : NewRaceLocation.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            var saved = await _runningService.SaveOfficialRaceAsync(race);
            _allOfficialRaces.Insert(0, saved);
            ApplyOfficialRacesFilter();
            SyncRaceOptions();
            SelectedRace = OfficialRaces.FirstOrDefault(r => r.Id == saved.Id) ?? saved;

            NewRaceName = string.Empty;
            NewRaceDistanceKm = string.Empty;
            NewRaceEventDate = null;
            NewRaceLocation = string.Empty;
            RaceValidationMessage = null;

            StatusMessage = $"Carrera oficial registrada: {saved.Name} ({saved.DistanceKm:0.##} km)";
        }, "Registrando carrera...");
    }

    private bool CanSaveSession() => ValidateSessionForm().IsValid;

    private async Task SaveSessionAsync()
    {
        if (!ValidateSessionForm().IsValid)
        {
            RefreshSessionValidation();
            return;
        }

        FormValidation.RequirePositiveDecimal(DistanceKm, "La distancia (km)", out var distance);
        FormValidation.RequireNonNegativeInt(DurationMinutes, "Los minutos", out var min);
        FormValidation.RequireIntInRange(DurationSeconds, "Los segundos", 0, 59, out var sec);
        var duration = new TimeSpan(0, min, sec);
        var raceId = SelectedRaceOption?.Id;

        await RunBusyAsync(async () =>
        {
            var result = await _runningService.SaveSessionAsync(distance, duration, raceId);
            PublishAchievements(result.Events);
            _allSessions.Insert(0, result.Value);
            ApplySessionsFilter();

            DistanceKm = string.Empty;
            DurationMinutes = string.Empty;
            DurationSeconds = string.Empty;
            SessionValidationMessage = null;
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

            UpdateOfficialRace(result.Value);
            SelectedRace = OfficialRaces.FirstOrDefault(r => r.Id == result.Value.Id);
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

    private async Task DeleteSessionAsync(object? parameter)
    {
        if (parameter is not RunningSession session)
            return;

        if (!_messageDialogService.Confirm(
                $"¿Eliminar la sesión del {session.RecordedAt:dd/MM/yyyy} ({session.DistanceKm:0.##} km)?\nSe revertirá el XP asociado.",
                "Eliminar del historial"))
            return;

        await RunBusyAsync(async () =>
        {
            if (!await _runningService.DeleteSessionAsync(session.Id))
                return;

            _allSessions.RemoveAll(s => s.Id == session.Id);
            ApplySessionsFilter();
            _profileRefreshMessenger.RequestRefresh();
            StatusMessage = "Sesión eliminada del historial.";
        }, "Eliminando sesión...");
    }

    private async Task DeleteOfficialRaceAsync(object? parameter)
    {
        var race = parameter as OfficialRace ?? SelectedRace;
        if (race is null)
            return;

        if (!_messageDialogService.Confirm(
                $"¿Eliminar la carrera «{race.Name}» del historial?\nLas sesiones vinculadas quedarán sin carrera asociada.",
                "Eliminar del historial"))
            return;

        await RunBusyAsync(async () =>
        {
            if (!await _runningService.DeleteOfficialRaceAsync(race.Id))
                return;

            _allOfficialRaces.RemoveAll(r => r.Id == race.Id);
            ApplyOfficialRacesFilter();
            SyncRaceOptions();
            SelectedRace = OfficialRaces.FirstOrDefault();
            SelectedRaceOption = RaceOptions.FirstOrDefault();
            _profileRefreshMessenger.RequestRefresh();
            StatusMessage = $"Carrera «{race.Name}» eliminada del historial.";
        }, "Eliminando carrera...");
    }
}
