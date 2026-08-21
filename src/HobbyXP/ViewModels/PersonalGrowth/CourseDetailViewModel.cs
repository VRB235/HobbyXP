using System.Windows.Input;
using HobbyXP.Helpers;
using HobbyXP.Models.PersonalGrowth;
using HobbyXP.Services.Abstractions;
using HobbyXP.ViewModels.Common;

namespace HobbyXP.ViewModels.PersonalGrowth;

public sealed class CourseDetailViewModel : ViewModelBase
{
    private readonly ICourseService _courseService;
    private readonly IFileDialogService _fileDialogService;
    private readonly Course _original;
    private readonly CoverImageDraft _cover;

    private string _name;
    private string _platform;
    private string _totalSessions;
    private DateTime? _completedDate;
    private string? _validationMessage;
    private string? _errorMessage;
    private bool _isBusy;

    public CourseDetailViewModel(
        Course course,
        ICourseService courseService,
        IFileDialogService fileDialogService)
    {
        _courseService = courseService;
        _fileDialogService = fileDialogService;
        _original = course;
        _cover = new CoverImageDraft(HobbyCoverPhotoStorage.Folders.Courses, course.ImageDisplayPath);
        _cover.Changed += OnCoverChanged;

        _name = course.Name;
        _platform = course.Platform;
        _totalSessions = course.TotalSessions.ToString();
        _completedDate = course.CompletedAt?.ToLocalTime().Date;

        SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsBusy && CanSave());
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(false), () => !IsBusy);
        PickImageCommand = new RelayCommand(() => _cover.Pick(_fileDialogService), () => !IsBusy);
        ClearImageCommand = new RelayCommand(() => _cover.Clear(), () => !IsBusy && _cover.HasPreview);
        RefreshValidation();
    }

    public event Action<bool>? RequestClose;

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
                RefreshValidation();
        }
    }

    public string Platform
    {
        get => _platform;
        set => SetProperty(ref _platform, value);
    }

    public string TotalSessions
    {
        get => _totalSessions;
        set
        {
            if (SetProperty(ref _totalSessions, value))
                RefreshValidation();
        }
    }

    public DateTime? CompletedDate
    {
        get => _completedDate;
        set => SetProperty(ref _completedDate, value);
    }

    public int SessionsCompleted => _original.SessionsCompleted;
    public int XpEarned => _original.XpEarned;
    public bool IsCompleted => _original.Status == Models.Enums.CourseStatus.Completed;

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

    public Course? SavedCourse { get; private set; }

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

    private ValidationResult ValidateForm()
    {
        var sessionsResult = FormValidation.RequirePositiveInt(TotalSessions, "Las sesiones totales", out var total);
        return FormValidation.FirstFailure(
            FormValidation.RequireText(Name, "el nombre"),
            sessionsResult,
            sessionsResult.IsValid && total < SessionsCompleted
                ? ValidationResult.Fail("Las sesiones totales no pueden ser menores a las ya completadas.")
                : ValidationResult.Ok());
    }

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

        FormValidation.RequirePositiveInt(TotalSessions, "Las sesiones totales", out var total);
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            SavedCourse = await _courseService.UpdateMetadataAsync(
                _original.Id,
                Name.Trim(),
                Platform.Trim(),
                total,
                CompletedDate.HasValue
                    ? DateTimeHelper.ToUtcFromLocalDate(CompletedDate.Value)
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
        if (SavedCourse is null)
            _cover.DiscardPending();
    }
}
