using HobbyXP.Helpers;
using HobbyXP.Models.PersonalGrowth;
using HobbyXP.Services.Abstractions;
using HobbyXP.ViewModels.Common;

namespace HobbyXP.ViewModels.PersonalGrowth;

public sealed class CourseProgressRowViewModel : ViewModelBase
{
    private readonly Func<Course, DateTime, int, Task> _logSessionsAsync;
    private readonly Func<Course, string?, bool, Task<Course>> _updateImageAsync;
    private DateTime? _sessionDate = DateTime.Today;
    private int _sessionsToLog = 1;
    private string? _validationMessage;

    public CourseProgressRowViewModel(
        Course course,
        Func<Course, DateTime, int, Task> logSessionsAsync,
        Func<Course, string?, bool, Task<Course>> updateImageAsync,
        IFileDialogService fileDialogService)
    {
        Course = course;
        _logSessionsAsync = logSessionsAsync;
        _updateImageAsync = updateImageAsync;

        Cover = new ProgressCoverController(
            HobbyCoverPhotoStorage.Folders.Courses,
            course.ImageDisplayPath,
            fileDialogService,
            PersistCoverAsync);

        LogSessionsCommand = new AsyncRelayCommand(LogSessionsAsync, CanLogSessions);
        BumpSessionsCommand = new RelayCommand(BumpSessions);
        CompleteAllCommand = new RelayCommand(CompleteAll);
        RefreshValidation();
    }

    public Course Course { get; private set; }

    public ProgressCoverController Cover { get; }

    public string? ImageDisplayPath => Cover.ImageDisplayPath;

    public bool HasImage => Cover.HasImage;

    public string ImageActionLabel => Cover.ImageActionLabel;

    public AsyncRelayCommand PickImageCommand => Cover.PickCommand;

    public AsyncRelayCommand ClearImageCommand => Cover.ClearCommand;

    public string Name => Course.Name;

    public string Platform => Course.Platform;

    public int TotalSessions => Course.TotalSessions;

    public int SessionsCompleted => Course.SessionsCompleted;

    public int RemainingSessions => Math.Max(0, TotalSessions - SessionsCompleted);

    public int XpEarned => Course.XpEarned;

    public string XpEarnedDisplay => $"XP: {XpEarned}";

    public double ProgressPercent =>
        TotalSessions > 0 ? (double)SessionsCompleted / TotalSessions * 100d : 0d;

    public string ProgressSummary =>
        $"Progreso: {SessionsCompleted}/{TotalSessions} sesiones";

    public string? ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public DateTime? SessionDate
    {
        get => _sessionDate;
        set
        {
            if (SetProperty(ref _sessionDate, value))
                RefreshValidation();
        }
    }

    public int SessionsToLog
    {
        get => _sessionsToLog;
        set
        {
            if (!SetProperty(ref _sessionsToLog, value))
                return;

            RefreshValidation();
        }
    }

    public AsyncRelayCommand LogSessionsCommand { get; }

    public RelayCommand BumpSessionsCommand { get; }

    public RelayCommand CompleteAllCommand { get; }

    private async Task<string?> PersistCoverAsync(string? imageSourcePath, bool clearImage)
    {
        var updated = await _updateImageAsync(Course, imageSourcePath, clearImage);
        Course = updated;
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Platform));
        OnPropertyChanged(nameof(XpEarned));
        OnPropertyChanged(nameof(XpEarnedDisplay));
        return updated.ImageDisplayPath;
    }

    private ValidationResult ValidateForm()
    {
        if (RemainingSessions <= 0)
            return ValidationResult.Fail("Este curso ya está completado.");

        if (!SessionDate.HasValue)
            return ValidationResult.Fail("Indique la fecha de las sesiones.");

        if (SessionsToLog <= 0)
            return ValidationResult.Fail("Las sesiones a registrar deben ser mayor que cero.");

        return FormValidation.RequireNotAbove(
            SessionsToLog,
            RemainingSessions,
            "Las sesiones a registrar",
            "sesiones");
    }

    private void RefreshValidation()
    {
        var result = ValidateForm();
        ValidationMessage = result.IsValid ? null : result.Message;
        LogSessionsCommand.RaiseCanExecuteChanged();
    }

    private bool CanLogSessions() => ValidateForm().IsValid;

    private void BumpSessions(object? parameter)
    {
        var amount = parameter switch
        {
            int value => value,
            string text when int.TryParse(text, out var parsed) => parsed,
            _ => 1
        };

        SessionsToLog = SessionsToLog + amount;
    }

    private void CompleteAll() => SessionsToLog = RemainingSessions;

    private async Task LogSessionsAsync()
    {
        if (!ValidateForm().IsValid)
        {
            RefreshValidation();
            return;
        }

        if (!SessionDate.HasValue)
            return;

        await _logSessionsAsync(Course, SessionDate.Value, SessionsToLog);
        SessionsToLog = Math.Min(1, RemainingSessions);
        OnPropertyChanged(nameof(RemainingSessions));
        RefreshValidation();
    }
}
