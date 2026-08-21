using System.Windows.Input;
using HobbyXP.Helpers;
using HobbyXP.Models.Entertainment;
using HobbyXP.Models.Enums;
using HobbyXP.Services.Abstractions;
using HobbyXP.ViewModels.Common;

namespace HobbyXP.ViewModels.Entertainment;

public sealed class VideoGameDetailViewModel : ViewModelBase
{
    private readonly IVideoGameService _videoGameService;
    private readonly IFileDialogService _fileDialogService;
    private readonly VideoGame _original;
    private readonly CoverImageDraft _cover;

    private string _title;
    private VideoGamePlatform _platform;
    private DateTime? _startedDate;
    private DateTime? _platinumDate;
    private string? _validationMessage;
    private string? _errorMessage;
    private bool _isBusy;

    public VideoGameDetailViewModel(
        VideoGame game,
        IVideoGameService videoGameService,
        IFileDialogService fileDialogService)
    {
        _videoGameService = videoGameService;
        _fileDialogService = fileDialogService;
        _original = game;
        _cover = new CoverImageDraft(HobbyCoverPhotoStorage.Folders.VideoGames, game.ImageDisplayPath);
        _cover.Changed += OnCoverChanged;

        _title = game.Title;
        _platform = game.Platform;
        _startedDate = game.StartedAt?.ToLocalTime().Date;
        _platinumDate = game.PlatinumUnlockedAt?.ToLocalTime().Date;

        SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsBusy && CanSave());
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(false), () => !IsBusy);
        PickImageCommand = new RelayCommand(() => _cover.Pick(_fileDialogService), () => !IsBusy);
        ClearImageCommand = new RelayCommand(() => _cover.Clear(), () => !IsBusy && _cover.HasPreview);
        RefreshValidation();
    }

    public event Action<bool>? RequestClose;

    public Array Platforms => Enum.GetValues(typeof(VideoGamePlatform));

    public bool IsPlatinum => _original.Status == VideoGameStatus.Platinum;

    public string Title
    {
        get => _title;
        set
        {
            if (SetProperty(ref _title, value))
                RefreshValidation();
        }
    }

    public VideoGamePlatform Platform
    {
        get => _platform;
        set => SetProperty(ref _platform, value);
    }

    public DateTime? StartedDate
    {
        get => _startedDate;
        set => SetProperty(ref _startedDate, value);
    }

    public DateTime? PlatinumDate
    {
        get => _platinumDate;
        set => SetProperty(ref _platinumDate, value);
    }

    public int XpEarned => _original.XpEarned;

    public int CompletionPercentage => _original.CompletionPercentage;

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

    public VideoGame? SavedGame { get; private set; }

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
        FormValidation.RequireText(Title, "el título");

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
            SavedGame = await _videoGameService.UpdateMetadataAsync(
                _original.Id,
                Title.Trim(),
                Platform,
                StartedDate.HasValue
                    ? DateTimeHelper.ToUtcFromLocalDate(StartedDate.Value)
                    : null,
                PlatinumDate.HasValue
                    ? DateTimeHelper.ToUtcFromLocalDate(PlatinumDate.Value)
                    : null,
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
        if (SavedGame is null)
            _cover.DiscardPending();
    }
}
