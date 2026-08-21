using System.Windows.Input;
using HobbyXP.Helpers;
using HobbyXP.Models.Physical;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Results;
using HobbyXP.ViewModels.Common;

namespace HobbyXP.ViewModels.Physical;

public sealed class OfficialRaceDetailViewModel : ViewModelBase
{
    private readonly IRunningService _runningService;
    private readonly IFileDialogService _fileDialogService;
    private readonly OfficialRace _original;
    private readonly bool _wasCompleted;

    private string _name;
    private string _distanceKm;
    private DateTime? _eventDate;
    private string _location;
    private string _description;
    private bool _isCompleted;
    private string? _previewImagePath;
    private string? _pendingImageSourcePath;
    private bool _clearImageOnSave;
    private string? _validationMessage;
    private string? _errorMessage;
    private bool _isBusy;
    private RacePreparationStats? _preparationStats;

    public OfficialRaceDetailViewModel(
        OfficialRace race,
        IRunningService runningService,
        IFileDialogService fileDialogService)
    {
        ArgumentNullException.ThrowIfNull(race);
        _runningService = runningService;
        _fileDialogService = fileDialogService;
        _original = race;
        _wasCompleted = race.IsCompleted;

        _name = race.Name;
        _distanceKm = race.DistanceKm.ToString("0.###");
        _eventDate = race.EventDate?.Date;
        _location = race.Location ?? string.Empty;
        _description = race.Description ?? string.Empty;
        _isCompleted = race.IsCompleted;
        _previewImagePath = race.ImageDisplayPath;

        SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsBusy && CanSave());
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(false), () => !IsBusy);
        PickImageCommand = new RelayCommand(PickImage, () => !IsBusy);
        ClearImageCommand = new RelayCommand(ClearImage, () => !IsBusy && CanClearImage());

        _ = LoadStatsAsync();
        RefreshValidation();
    }

    public event Action<bool>? RequestClose;

    public int RaceId => _original.Id;

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
                RefreshValidation();
        }
    }

    public string DistanceKm
    {
        get => _distanceKm;
        set
        {
            if (SetProperty(ref _distanceKm, value))
                RefreshValidation();
        }
    }

    public DateTime? EventDate
    {
        get => _eventDate;
        set => SetProperty(ref _eventDate, value);
    }

    public string Location
    {
        get => _location;
        set => SetProperty(ref _location, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public bool IsCompleted
    {
        get => _isCompleted;
        set => SetProperty(ref _isCompleted, value);
    }

    public string? PreviewImagePath
    {
        get => _previewImagePath;
        private set
        {
            if (SetProperty(ref _previewImagePath, value))
            {
                OnPropertyChanged(nameof(HasPreviewImage));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool HasPreviewImage => !string.IsNullOrWhiteSpace(PreviewImagePath);

    public string? ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value))
                return;

            SaveCommand.RaiseCanExecuteChanged();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public RacePreparationStats? PreparationStats
    {
        get => _preparationStats;
        private set => SetProperty(ref _preparationStats, value);
    }

    public OfficialRace? SavedRace { get; private set; }

    public AchievementEvent[] CompletionEvents { get; private set; } = [];

    public AsyncRelayCommand SaveCommand { get; }

    public RelayCommand CancelCommand { get; }

    public RelayCommand PickImageCommand { get; }

    public RelayCommand ClearImageCommand { get; }

    private async Task LoadStatsAsync()
    {
        try
        {
            PreparationStats = await _runningService.GetRacePreparationStatsAsync(_original.Id);
        }
        catch
        {
            PreparationStats = null;
        }
    }

    private bool CanClearImage() =>
        HasPreviewImage || _pendingImageSourcePath is not null || _clearImageOnSave;

    private bool CanSave() => ValidateForm().IsValid;

    private ValidationResult ValidateForm() =>
        FormValidation.FirstFailure(
            FormValidation.RequireText(Name, "el nombre de la carrera"),
            FormValidation.RequirePositiveDecimal(DistanceKm, "La distancia (km)", out _));

    private void RefreshValidation()
    {
        var result = ValidateForm();
        ValidationMessage = result.IsValid ? null : result.Message;
        SaveCommand.RaiseCanExecuteChanged();
    }

    private void PickImage()
    {
        var path = _fileDialogService.PickImageFile();
        if (string.IsNullOrWhiteSpace(path))
            return;

        DiscardPendingStagingImage();

        var persisted = RacePhotoStorage.ImportToStaging(path);
        if (persisted is null)
        {
            ErrorMessage = "No se pudo copiar la imagen al almacén de la aplicación.";
            return;
        }

        ErrorMessage = null;
        _pendingImageSourcePath = persisted;
        _clearImageOnSave = false;
        PreviewImagePath = persisted;
        CommandManager.InvalidateRequerySuggested();
    }

    private void ClearImage()
    {
        DiscardPendingStagingImage();
        _pendingImageSourcePath = null;
        _clearImageOnSave = true;
        PreviewImagePath = null;
        CommandManager.InvalidateRequerySuggested();
    }

    private void DiscardPendingStagingImage()
    {
        if (_pendingImageSourcePath is null)
            return;

        RacePhotoStorage.DeleteStagingFile(_pendingImageSourcePath);
        _pendingImageSourcePath = null;
    }

    private async Task SaveAsync()
    {
        if (!ValidateForm().IsValid)
        {
            RefreshValidation();
            return;
        }

        FormValidation.RequirePositiveDecimal(DistanceKm, "La distancia (km)", out var distanceKm);
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var draft = new OfficialRace
            {
                Id = _original.Id,
                Name = Name.Trim(),
                DistanceKm = distanceKm,
                EventDate = EventDate.HasValue
                    ? DateTime.SpecifyKind(EventDate.Value.Date, DateTimeKind.Utc)
                    : null,
                Location = string.IsNullOrWhiteSpace(Location) ? null : Location.Trim(),
                Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                ImagePath = _original.ImagePath,
                IsCompleted = _wasCompleted,
                CompletedAt = _original.CompletedAt,
                BonusXpAwarded = _original.BonusXpAwarded,
                CreatedAt = _original.CreatedAt
            };

            var saved = await _runningService.SaveOfficialRaceAsync(
                draft,
                imageSourcePath: _pendingImageSourcePath,
                clearImage: _clearImageOnSave);

            CompletionEvents = [];

            if (IsCompleted && !_wasCompleted)
            {
                var result = await _runningService.CompleteOfficialRaceAsync(saved.Id);
                saved = result.Value;
                CompletionEvents = result.Events.ToArray();
            }
            else if (!IsCompleted && _wasCompleted)
            {
                saved = await _runningService.MarkOfficialRacePendingAsync(saved.Id);
            }

            _pendingImageSourcePath = null;
            SavedRace = saved;
            RequestClose?.Invoke(true);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void OnClosedWithoutSave()
    {
        if (SavedRace is null)
            DiscardPendingStagingImage();
    }
}
