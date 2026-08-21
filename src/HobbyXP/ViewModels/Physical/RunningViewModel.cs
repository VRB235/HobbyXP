using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using HobbyXP.Helpers;
using HobbyXP.Models.Enums;
using HobbyXP.Models.Physical;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Messaging;
using HobbyXP.Services.Results;
using HobbyXP.ViewModels.Common;
using HobbyXP.ViewModels.Messaging;
using HobbyXP.Views.Dialogs;

namespace HobbyXP.ViewModels.Physical;

public sealed class RunningViewModel : AchievementAwareViewModel
{
    private readonly IRunningService _runningService;
    private readonly IMessageDialogService _messageDialogService;
    private readonly IFileDialogService _fileDialogService;
    private readonly IProfileRefreshMessenger _profileRefreshMessenger;
    private string _distanceKm = string.Empty;
    private string _durationMinutes = string.Empty;
    private string _durationSeconds = string.Empty;
    private DateTime? _sessionDate = DateTime.Today;
    private RaceOption? _selectedRaceOption;
    private OfficialRace? _selectedRace;
    private RacePreparationStats? _selectedRaceStats;
    private string _newRaceName = string.Empty;
    private string _newRaceDistanceKm = string.Empty;
    private DateTime? _newRaceEventDate;
    private string _newRaceLocation = string.Empty;
    private string? _newRacePreviewImagePath;
    private string? _newRacePendingImagePath;
    private DateTime? _sessionsFromDate;
    private DateTime? _sessionsToDate;
    private RunningSessionTypeOption _selectedSessionTypeOption;
    private RunningSessionTypeOption _sessionsTypeFilterOption;
    private RunningSessionTypeOption _editSessionTypeOption;
    private RunningSession? _selectedSession;
    private List<RunningSession> _allSessions = [];
    private List<OfficialRace> _allOfficialRaces = [];
    private string _raceSearchText = string.Empty;
    private DateTime? _racesFromDate;
    private DateTime? _racesToDate;
    private string? _sessionValidationMessage;
    private string? _raceValidationMessage;
    private bool _isNewSessionExpanded = true;
    private bool _isNewRaceExpanded;
    private bool _isOfficialRacesExpanded;
    private bool _isSessionsExpanded;
    private bool _suppressSectionAccordion;
    private int _seriesCount;

    public RunningViewModel(
        IRunningService runningService,
        IXpService xpService,
        IWeeklyQuotaService weeklyQuotaService,
        IMessageDialogService messageDialogService,
        IFileDialogService fileDialogService,
        IProfileRefreshMessenger profileRefreshMessenger,
        IAchievementMessenger achievementMessenger,
        IAchievementProgressService achievementProgress)
        : base(achievementMessenger)
    {
        _runningService = runningService;
        _messageDialogService = messageDialogService;
        _fileDialogService = fileDialogService;
        _profileRefreshMessenger = profileRefreshMessenger;
        HobbyXp = new HobbyProgressPresenter(xpService, MilestoneSourceType.Running, weeklyQuotaService, achievementProgress);
        OfficialRaceXp = new HobbyProgressPresenter(xpService, MilestoneSourceType.OfficialRace, achievementProgress: achievementProgress);
        Sessions = new ObservableCollection<RunningSession>();
        OfficialRaces = new ObservableCollection<OfficialRace>();
        RaceOptions = new ObservableCollection<RaceOption> { RaceOption.None };
        SeriesRows = new ObservableCollection<RunningSeriesRowViewModel>();
        SessionTypeOptions = RunningSessionTypeOption.CreateCatalogOptions();
        SessionTypeFilterOptions = RunningSessionTypeOption.CreateFilterOptions();
        _selectedSessionTypeOption = SessionTypeOptions[0];
        _sessionsTypeFilterOption = SessionTypeFilterOptions[0];
        _editSessionTypeOption = SessionTypeOptions[0];

        SaveSessionCommand = new AsyncRelayCommand(SaveSessionAsync, CanSaveSession);
        RegisterOfficialRaceCommand = new AsyncRelayCommand(RegisterOfficialRaceAsync, CanRegisterOfficialRace);
        CompleteRaceCommand = new AsyncRelayCommand(CompleteRaceAsync, () => SelectedRace is { IsCompleted: false });
        ClearSessionsDateFilterCommand = new RelayCommand(ClearSessionsDateFilter);
        ClearOfficialRacesFilterCommand = new RelayCommand(ClearOfficialRacesFilter);
        UpdateSessionTypeCommand = new AsyncRelayCommand(UpdateSessionTypeAsync, () => SelectedSession is not null);
        DeleteSessionCommand = new AsyncRelayCommand(p => DeleteSessionAsync(p));
        DeleteOfficialRaceCommand = new AsyncRelayCommand(
            p => DeleteOfficialRaceAsync(p),
            p => p is OfficialRace || SelectedRace is not null);
        OpenRaceDetailCommand = new RelayCommand(p => OpenRaceDetail(p), p => p is OfficialRace || SelectedRace is not null);
        PickNewRaceImageCommand = new RelayCommand(PickNewRaceImage);
        ClearNewRaceImageCommand = new RelayCommand(ClearNewRaceImage, () => HasNewRacePreviewImage);
        ApplyFirstSeriesToAllCommand = new RelayCommand(ApplyFirstSeriesToAll, () => SeriesRows.Count > 1);
        RefreshSessionValidation();
        RefreshRaceValidation();
    }

    public HobbyProgressPresenter HobbyXp { get; }

    public HobbyProgressPresenter OfficialRaceXp { get; }

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

    public bool IsNewSessionExpanded
    {
        get => _isNewSessionExpanded;
        set => SetSectionExpanded(RunningSection.NewSession, value, ref _isNewSessionExpanded, nameof(IsNewSessionExpanded));
    }

    public bool IsNewRaceExpanded
    {
        get => _isNewRaceExpanded;
        set => SetSectionExpanded(RunningSection.NewRace, value, ref _isNewRaceExpanded, nameof(IsNewRaceExpanded));
    }

    public bool IsOfficialRacesExpanded
    {
        get => _isOfficialRacesExpanded;
        set => SetSectionExpanded(RunningSection.OfficialRaces, value, ref _isOfficialRacesExpanded, nameof(IsOfficialRacesExpanded));
    }

    public bool IsSessionsExpanded
    {
        get => _isSessionsExpanded;
        set => SetSectionExpanded(RunningSection.Sessions, value, ref _isSessionsExpanded, nameof(IsSessionsExpanded));
    }

    public ObservableCollection<RunningSession> Sessions { get; }

    public ObservableCollection<OfficialRace> OfficialRaces { get; }

    public ObservableCollection<RaceOption> RaceOptions { get; }

    public ObservableCollection<RunningSeriesRowViewModel> SeriesRows { get; }

    public IReadOnlyList<RunningSessionTypeOption> SessionTypeOptions { get; }

    public IReadOnlyList<RunningSessionTypeOption> SessionTypeFilterOptions { get; }

    public bool ShowUmbralSeriesPanel => SelectedSessionTypeOption.Value == RunningSessionType.Umbral;

    public int SeriesCount
    {
        get => _seriesCount;
        set
        {
            var clamped = Math.Clamp(value, 0, 30);
            if (!SetProperty(ref _seriesCount, clamped))
                return;

            SyncSeriesRows();
            RefreshSessionValidation();
        }
    }

    public RunningSessionTypeOption SelectedSessionTypeOption
    {
        get => _selectedSessionTypeOption;
        set
        {
            if (!SetProperty(ref _selectedSessionTypeOption, value))
                return;

            OnPropertyChanged(nameof(ShowUmbralSeriesPanel));
            RefreshSessionValidation();
        }
    }

    public RunningSessionTypeOption SessionsTypeFilterOption
    {
        get => _sessionsTypeFilterOption;
        set
        {
            if (SetProperty(ref _sessionsTypeFilterOption, value))
                ApplySessionsFilter();
        }
    }

    public RunningSessionTypeOption EditSessionTypeOption
    {
        get => _editSessionTypeOption;
        set => SetProperty(ref _editSessionTypeOption, value);
    }

    public RunningSession? SelectedSession
    {
        get => _selectedSession;
        set
        {
            if (!SetProperty(ref _selectedSession, value))
                return;

            SyncEditSessionTypeOption();
            UpdateSessionTypeCommand.RaiseCanExecuteChanged();
        }
    }

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

    public DateTime? SessionDate
    {
        get => _sessionDate;
        set
        {
            if (SetProperty(ref _sessionDate, value))
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
            CompleteRaceCommand.RaiseCanExecuteChanged();
            CommandManager.InvalidateRequerySuggested();
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

    public string? NewRacePreviewImagePath
    {
        get => _newRacePreviewImagePath;
        private set
        {
            if (SetProperty(ref _newRacePreviewImagePath, value))
            {
                OnPropertyChanged(nameof(HasNewRacePreviewImage));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool HasNewRacePreviewImage => !string.IsNullOrWhiteSpace(NewRacePreviewImagePath);

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

    public AsyncRelayCommand UpdateSessionTypeCommand { get; }

    public RelayCommand ApplyFirstSeriesToAllCommand { get; }

    public AsyncRelayCommand RegisterOfficialRaceCommand { get; }

    public AsyncRelayCommand CompleteRaceCommand { get; }

    public RelayCommand ClearSessionsDateFilterCommand { get; }

    public RelayCommand ClearOfficialRacesFilterCommand { get; }

    public AsyncRelayCommand DeleteSessionCommand { get; }

    public AsyncRelayCommand DeleteOfficialRaceCommand { get; }

    public RelayCommand OpenRaceDetailCommand { get; }

    public RelayCommand PickNewRaceImageCommand { get; }

    public RelayCommand ClearNewRaceImageCommand { get; }

    protected override async Task LoadCoreAsync()
    {
        await HobbyXp.RefreshAsync();
        await OfficialRaceXp.RefreshAsync();

        _allSessions = (await _runningService.GetSessionsAsync()).ToList();
        ApplySessionsFilter();

        var races = await _runningService.GetOfficialRacesAsync();
        SyncRaceCollections(races);
        SelectedRaceOption ??= RaceOption.None;
    }

    private void ApplySessionsFilter()
    {
        var selectedId = SelectedSession?.Id;

        Sessions.Clear();
        foreach (var session in _allSessions.Where(MatchesSessionFilters))
            Sessions.Add(session);

        if (selectedId is int id)
        {
            var stillVisible = Sessions.FirstOrDefault(s => s.Id == id);
            if (!ReferenceEquals(SelectedSession, stillVisible))
                SelectedSession = stillVisible;
        }
    }

    private void SyncEditSessionTypeOption()
    {
        if (SelectedSession?.SessionType is RunningSessionType type)
        {
            EditSessionTypeOption = SessionTypeOptions.FirstOrDefault(o => o.Value == type)
                ?? SessionTypeOptions[0];
            return;
        }

        EditSessionTypeOption = SessionTypeOptions[0];
    }

    private async Task UpdateSessionTypeAsync()
    {
        if (SelectedSession is null || EditSessionTypeOption.Value is null)
            return;

        var sessionId = SelectedSession.Id;
        var type = EditSessionTypeOption.Value.Value;

        await RunBusyAsync(async () =>
        {
            var updated = await _runningService.UpdateSessionTypeAsync(sessionId, type);
            if (updated is null)
                return;

            var index = _allSessions.FindIndex(s => s.Id == sessionId);
            if (index >= 0)
                _allSessions[index] = updated;

            ApplySessionsFilter();
            SelectedSession = Sessions.FirstOrDefault(s => s.Id == sessionId)
                ?? _allSessions.FirstOrDefault(s => s.Id == sessionId);
            StatusMessage = $"Tipo actualizado: {updated.SessionTypeLabel} ({updated.RecordedAt:dd/MM/yyyy}, {updated.DistanceKm:0.##} km).";
        }, "Actualizando tipo...");
    }

    private bool MatchesSessionFilters(RunningSession session) =>
        DateRangeFilter.Matches(session.RecordedAt, SessionsFromDate, SessionsToDate) &&
        SessionsTypeFilterOption.Matches(session.SessionType);

    private void ClearSessionsDateFilter()
    {
        _sessionsFromDate = null;
        _sessionsToDate = null;
        _sessionsTypeFilterOption = SessionTypeFilterOptions[0];
        OnPropertyChanged(nameof(SessionsFromDate));
        OnPropertyChanged(nameof(SessionsToDate));
        OnPropertyChanged(nameof(SessionsTypeFilterOption));
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

        if (!SessionDate.HasValue)
            return ValidationResult.Fail("Indique la fecha de la sesión.");

        if (min == 0 && sec == 0)
            return ValidationResult.Fail("Indique una duración mayor que cero.");

        if (ShowUmbralSeriesPanel && SeriesRows.Count > 0)
        {
            foreach (var row in SeriesRows)
            {
                if (!row.TryBuildDraft(out _, out var seriesError))
                    return ValidationResult.Fail(seriesError ?? "Complete las series de umbral.");
            }
        }

        return ValidationResult.Ok();
    }

    private void SyncSeriesRows()
    {
        while (SeriesRows.Count > SeriesCount)
            SeriesRows.RemoveAt(SeriesRows.Count - 1);

        while (SeriesRows.Count < SeriesCount)
        {
            var order = SeriesRows.Count + 1;
            var previous = SeriesRows.LastOrDefault();
            var row = new RunningSeriesRowViewModel(order, RefreshSessionValidation);
            if (previous is not null &&
                !string.IsNullOrWhiteSpace(previous.Distance) &&
                !string.IsNullOrWhiteSpace(previous.DurationMinutes))
            {
                row.Distance = previous.Distance;
                row.DistanceUnit = previous.DistanceUnit;
                row.DurationMinutes = previous.DurationMinutes;
                row.DurationSeconds = previous.DurationSeconds;
            }

            SeriesRows.Add(row);
        }
    }

    private void ApplyFirstSeriesToAll()
    {
        if (SeriesRows.Count == 0)
            return;

        var first = SeriesRows[0];
        for (var i = 1; i < SeriesRows.Count; i++)
        {
            SeriesRows[i].Distance = first.Distance;
            SeriesRows[i].DistanceUnit = first.DistanceUnit;
            SeriesRows[i].DurationMinutes = first.DurationMinutes;
            SeriesRows[i].DurationSeconds = first.DurationSeconds;
        }

        RefreshSessionValidation();
    }

    private void ClearSeriesForm()
    {
        SeriesCount = 0;
        SeriesRows.Clear();
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

            var saved = await _runningService.SaveOfficialRaceAsync(
                race,
                imageSourcePath: _newRacePendingImagePath);
            _newRacePendingImagePath = null;
            _allOfficialRaces.Insert(0, saved);
            ApplyOfficialRacesFilter();
            SyncRaceOptions();
            SelectedRace = OfficialRaces.FirstOrDefault(r => r.Id == saved.Id) ?? saved;

            NewRaceName = string.Empty;
            NewRaceDistanceKm = string.Empty;
            NewRaceEventDate = null;
            NewRaceLocation = string.Empty;
            NewRacePreviewImagePath = null;
            RaceValidationMessage = null;

            StatusMessage = $"Carrera oficial registrada: {saved.Name} ({saved.DistanceKm:0.##} km)";
        }, "Registrando carrera...");
    }

    private void PickNewRaceImage()
    {
        var path = _fileDialogService.PickImageFile();
        if (string.IsNullOrWhiteSpace(path))
            return;

        DiscardNewRaceStagingImage();

        var persisted = RacePhotoStorage.ImportToStaging(path);
        if (persisted is null)
        {
            ErrorMessage = "No se pudo copiar la imagen al almacén de la aplicación.";
            return;
        }

        ErrorMessage = null;
        _newRacePendingImagePath = persisted;
        NewRacePreviewImagePath = persisted;
    }

    private void ClearNewRaceImage()
    {
        DiscardNewRaceStagingImage();
        NewRacePreviewImagePath = null;
    }

    private void DiscardNewRaceStagingImage()
    {
        if (_newRacePendingImagePath is null)
            return;

        RacePhotoStorage.DeleteStagingFile(_newRacePendingImagePath);
        _newRacePendingImagePath = null;
    }

    private void OpenRaceDetail(object? parameter)
    {
        var race = parameter as OfficialRace ?? SelectedRace;
        if (race is null)
            return;

        SelectedRace = race;

        var detailVm = new OfficialRaceDetailViewModel(race, _runningService, _fileDialogService);
        var dialog = new OfficialRaceDetailWindow(detailVm)
        {
            Owner = Application.Current.MainWindow
        };

        var accepted = dialog.ShowDialog() == true;
        if (!accepted || detailVm.SavedRace is null)
            return;

        UpdateOfficialRace(detailVm.SavedRace);
        SyncRaceOptions();
        SelectedRace = OfficialRaces.FirstOrDefault(r => r.Id == detailVm.SavedRace.Id)
            ?? detailVm.SavedRace;

        if (detailVm.CompletionEvents.Length > 0)
        {
            PublishAchievements(detailVm.CompletionEvents);
            _ = OfficialRaceXp.RefreshAsync();
            _profileRefreshMessenger.RequestRefresh();
            StatusMessage = $"¡Carrera completada! +{detailVm.SavedRace.BonusXpAwarded} XP";
        }
        else
        {
            StatusMessage = $"Carrera actualizada: {detailVm.SavedRace.Name}";
        }
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
        var sessionType = SelectedSessionTypeOption.Value ?? RunningSessionType.Regenerativa;

        IReadOnlyList<RunningSeriesDraft>? seriesDrafts = null;
        if (sessionType == RunningSessionType.Umbral && SeriesRows.Count > 0)
        {
            var drafts = new List<RunningSeriesDraft>(SeriesRows.Count);
            foreach (var row in SeriesRows)
            {
                if (!row.TryBuildDraft(out var draft, out _) || draft is null)
                {
                    RefreshSessionValidation();
                    return;
                }

                drafts.Add(draft);
            }

            seriesDrafts = drafts;
        }

        await RunBusyAsync(async () =>
        {
            var result = await _runningService.SaveSessionAsync(
                distance,
                duration,
                sessionType,
                SessionDate ?? DateTime.Today,
                raceId,
                series: seriesDrafts);
            PublishAchievements(result.Events);
            await HobbyXp.RefreshAsync();
            _allSessions.Insert(0, result.Value);

            // Evitar que un filtro previo oculte la fila recién guardada.
            _sessionsFromDate = null;
            _sessionsToDate = null;
            _sessionsTypeFilterOption = SessionTypeFilterOptions[0];
            OnPropertyChanged(nameof(SessionsFromDate));
            OnPropertyChanged(nameof(SessionsToDate));
            OnPropertyChanged(nameof(SessionsTypeFilterOption));
            ApplySessionsFilter();

            DistanceKm = string.Empty;
            DurationMinutes = string.Empty;
            DurationSeconds = string.Empty;
            SessionDate = DateTime.Today;
            SelectedSessionTypeOption = SessionTypeOptions[0];
            ClearSeriesForm();
            SessionValidationMessage = null;
            // El acordeón deja "Nueva sesión" abierta por defecto; abrir el historial
            // para que el alta sea visible sin un clic extra.
            IsSessionsExpanded = true;
            var seriesNote = result.Value.Series.Count > 0
                ? $" · Series: {result.Value.SeriesSummary}"
                : string.Empty;
            StatusMessage = $"Sesión {result.Value.SessionTypeLabel} · Ritmo: {result.Value.PaceMinPerKm:0.00} min/km · +{result.Value.XpEarned} XP{seriesNote}";
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
            await OfficialRaceXp.RefreshAsync();

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
            await HobbyXp.RefreshAsync();
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
            await OfficialRaceXp.RefreshAsync();
            _profileRefreshMessenger.RequestRefresh();
            StatusMessage = $"Carrera «{race.Name}» eliminada del historial.";
        }, "Eliminando carrera...");
    }

    private void SetSectionExpanded(RunningSection section, bool isExpanded, ref bool field, string propertyName)
    {
        if (!SetProperty(ref field, isExpanded, propertyName) || _suppressSectionAccordion)
            return;

        if (!isExpanded)
            return;

        _suppressSectionAccordion = true;
        try
        {
            if (section != RunningSection.NewSession && _isNewSessionExpanded)
            {
                _isNewSessionExpanded = false;
                OnPropertyChanged(nameof(IsNewSessionExpanded));
            }

            if (section != RunningSection.NewRace && _isNewRaceExpanded)
            {
                _isNewRaceExpanded = false;
                OnPropertyChanged(nameof(IsNewRaceExpanded));
            }

            if (section != RunningSection.OfficialRaces && _isOfficialRacesExpanded)
            {
                _isOfficialRacesExpanded = false;
                OnPropertyChanged(nameof(IsOfficialRacesExpanded));
            }

            if (section != RunningSection.Sessions && _isSessionsExpanded)
            {
                _isSessionsExpanded = false;
                OnPropertyChanged(nameof(IsSessionsExpanded));
            }
        }
        finally
        {
            _suppressSectionAccordion = false;
        }
    }

    private enum RunningSection
    {
        NewSession,
        NewRace,
        OfficialRaces,
        Sessions
    }
}
