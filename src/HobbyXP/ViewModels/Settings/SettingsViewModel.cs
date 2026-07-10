using HobbyXP.Data;
using HobbyXP.Helpers;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Messaging;
using HobbyXP.ViewModels.Common;

namespace HobbyXP.ViewModels.Settings;

public sealed class SettingsViewModel : LoadableViewModelBase
{
    private readonly IPlayerProfileService _playerProfileService;
    private readonly IDatabaseMaintenanceService _databaseMaintenanceService;
    private readonly IFileDialogService _fileDialogService;
    private readonly IMessageDialogService _messageDialogService;
    private readonly IProfileRefreshMessenger _profileRefreshMessenger;
    private readonly IApplicationDataResetMessenger _applicationDataResetMessenger;

    private string _baseXpPerLevelText = "1000";
    private string _databasePath = string.Empty;
    private string _dataDirectory = string.Empty;
    private int _currentLevel = 1;
    private int _totalXp;

    public SettingsViewModel(
        IPlayerProfileService playerProfileService,
        IDatabaseMaintenanceService databaseMaintenanceService,
        IFileDialogService fileDialogService,
        IMessageDialogService messageDialogService,
        IProfileRefreshMessenger profileRefreshMessenger,
        IApplicationDataResetMessenger applicationDataResetMessenger)
    {
        _playerProfileService = playerProfileService;
        _databaseMaintenanceService = databaseMaintenanceService;
        _fileDialogService = fileDialogService;
        _messageDialogService = messageDialogService;
        _profileRefreshMessenger = profileRefreshMessenger;
        _applicationDataResetMessenger = applicationDataResetMessenger;

        SaveBaseXpPerLevelCommand = new AsyncRelayCommand(SaveBaseXpPerLevelAsync, CanSaveBaseXpPerLevel);
        ExportDatabaseCommand = new AsyncRelayCommand(ExportDatabaseAsync);
        ResetApplicationDataCommand = new AsyncRelayCommand(ResetApplicationDataAsync);
    }

    public string BaseXpPerLevelText
    {
        get => _baseXpPerLevelText;
        set
        {
            if (!SetProperty(ref _baseXpPerLevelText, value))
                return;

            RefreshBaseXpValidation();
        }
    }

    public string DatabasePath
    {
        get => _databasePath;
        private set => SetProperty(ref _databasePath, value);
    }

    public string DataDirectory
    {
        get => _dataDirectory;
        private set => SetProperty(ref _dataDirectory, value);
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

    public string ProfileSummary => $"Nivel {CurrentLevel} · {TotalXp:N0} XP acumulados";

    public AsyncRelayCommand SaveBaseXpPerLevelCommand { get; }

    public AsyncRelayCommand ExportDatabaseCommand { get; }

    public AsyncRelayCommand ResetApplicationDataCommand { get; }

    protected override async Task LoadCoreAsync()
    {
        DatabasePath = DatabaseConstants.GetDatabasePath();
        DataDirectory = DatabaseConstants.GetDatabaseDirectory();

        var profile = await _playerProfileService.GetProfileAsync();
        var progress = await _playerProfileService.GetLevelProgressAsync();

        BaseXpPerLevelText = profile.BaseXpPerLevel.ToString();
        CurrentLevel = progress.CurrentLevel;
        TotalXp = progress.TotalXp;

        ClearValidation();
        RefreshBaseXpValidation();
        OnPropertyChanged(nameof(ProfileSummary));
    }

    private async Task SaveBaseXpPerLevelAsync()
    {
        if (!CanSaveBaseXpPerLevel())
        {
            RefreshBaseXpValidation();
            return;
        }

        await RunBusyAsync(async () =>
        {
            var validation = FormValidation.RequirePositiveInt(BaseXpPerLevelText, "El XP base por nivel", out var baseXp);
            if (!validation.IsValid)
            {
                ApplyValidation(validation);
                return;
            }

            await _playerProfileService.UpdateBaseXpPerLevelAsync(baseXp);
            ClearValidation();

            var progress = await _playerProfileService.GetLevelProgressAsync();
            CurrentLevel = progress.CurrentLevel;
            TotalXp = progress.TotalXp;
            OnPropertyChanged(nameof(ProfileSummary));

            StatusMessage = $"XP base por nivel actualizado a {baseXp:N0}.";
        }, "Guardando configuración…");
    }

    private async Task ExportDatabaseAsync()
    {
        var destinationPath = _fileDialogService.PickSaveFilePath(
            $"hobbyxp-backup-{DateTime.Now:yyyyMMdd-HHmm}.db",
            "Base de datos SQLite|*.db|Todos los archivos|*.*",
            "Exportar copia de la base de datos");

        if (string.IsNullOrWhiteSpace(destinationPath))
            return;

        await RunBusyAsync(async () =>
        {
            await _databaseMaintenanceService.ExportDatabaseAsync(destinationPath);
            StatusMessage = $"Copia exportada en:\n{destinationPath}";
        }, "Exportando base de datos…");
    }

    private async Task ResetApplicationDataAsync()
    {
        if (!_messageDialogService.Confirm(
                "Se eliminarán todas las actividades, XP, medallas ganadas, premios canjeados y el perfil volverá a su estado inicial.\n\n" +
                "Las reglas de XP y definiciones de medallas se conservan.\n\n" +
                "Esta acción no se puede deshacer.",
                "Restablecer todos los datos"))
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            await _databaseMaintenanceService.ResetApplicationDataAsync();
            _profileRefreshMessenger.RequestRefresh();
            _applicationDataResetMessenger.NotifyReset();
            IsLoaded = false;
            await LoadAsync();
            StatusMessage = "Aplicación restablecida al estado inicial.";
        }, "Restableciendo datos…");
    }

    private bool CanSaveBaseXpPerLevel() =>
        FormValidation.RequirePositiveInt(BaseXpPerLevelText, "El XP base por nivel", out _).IsValid;

    private void RefreshBaseXpValidation()
    {
        var result = FormValidation.RequirePositiveInt(BaseXpPerLevelText, "El XP base por nivel", out _);
        RefreshValidation(result, SaveBaseXpPerLevelCommand);
    }
}
