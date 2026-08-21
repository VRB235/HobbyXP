using System.Windows.Input;
using HobbyXP.Helpers;
using HobbyXP.Models.Entertainment;
using HobbyXP.Models.Enums;
using HobbyXP.Services.Abstractions;
using HobbyXP.ViewModels.Common;

namespace HobbyXP.ViewModels.Entertainment;

public sealed class MediaEntryDetailViewModel : ViewModelBase
{
    private readonly IMediaService _mediaService;
    private readonly IFileDialogService _fileDialogService;
    private readonly MediaEntry _original;
    private readonly CoverImageDraft _cover;

    private string _title;
    private MediaType _mediaType;
    private DateTime? _completedDate;
    private string? _validationMessage;
    private string? _errorMessage;
    private bool _isBusy;

    public MediaEntryDetailViewModel(
        MediaEntry entry,
        IMediaService mediaService,
        IFileDialogService fileDialogService)
    {
        _mediaService = mediaService;
        _fileDialogService = fileDialogService;
        _original = entry;
        _cover = new CoverImageDraft(HobbyCoverPhotoStorage.Folders.MediaEntries, entry.ImageDisplayPath);
        _cover.Changed += OnCoverChanged;

        _title = entry.Title;
        _mediaType = entry.MediaType;
        _completedDate = entry.CompletedAt.ToLocalTime().Date;

        SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsBusy && CanSave());
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(false), () => !IsBusy);
        PickImageCommand = new RelayCommand(() => _cover.Pick(_fileDialogService), () => !IsBusy);
        ClearImageCommand = new RelayCommand(() => _cover.Clear(), () => !IsBusy && _cover.HasPreview);
        RefreshValidation();
    }

    public event Action<bool>? RequestClose;

    public Array MediaTypes => Enum.GetValues(typeof(MediaType));

    public string Title
    {
        get => _title;
        set
        {
            if (SetProperty(ref _title, value))
                RefreshValidation();
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
                RefreshValidation();
        }
    }

    public int XpEarned => _original.XpEarned;

    public string? PreviewImagePath => _cover.PreviewPath;

    public bool HasPreviewImage => _cover.HasPreview;

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

    public MediaEntry? SavedEntry { get; private set; }

    public AsyncRelayCommand SaveCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand PickImageCommand { get; }
    public RelayCommand ClearImageCommand { get; }

    private void OnCoverChanged()
    {
        OnPropertyChanged(nameof(PreviewImagePath));
        OnPropertyChanged(nameof(HasPreviewImage));
        CommandManager.InvalidateRequerySuggested();
    }

    private bool CanSave() => ValidateForm().IsValid;

    private ValidationResult ValidateForm() =>
        FormValidation.FirstFailure(
            FormValidation.RequireText(Title, "el título"),
            CompletedDate.HasValue
                ? ValidationResult.Ok()
                : ValidationResult.Fail("Indique la fecha de finalización."));

    private void RefreshValidation()
    {
        var result = ValidateForm();
        ValidationMessage = result.IsValid ? null : result.Message;
        SaveCommand.RaiseCanExecuteChanged();
    }

    private async Task SaveAsync()
    {
        if (!ValidateForm().IsValid)
        {
            RefreshValidation();
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var completedAt = DateTimeHelper.ToUtcFromLocalDate(CompletedDate ?? DateTime.Today);
            SavedEntry = await _mediaService.UpdateEntryAsync(
                _original.Id,
                Title.Trim(),
                MediaType,
                completedAt,
                _cover.PendingSourcePath,
                _cover.ClearOnSave);
            _cover.MarkSaved();
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
        if (SavedEntry is null)
            _cover.DiscardPending();
    }
}
