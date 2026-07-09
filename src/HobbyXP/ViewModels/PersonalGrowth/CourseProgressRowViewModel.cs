using HobbyXP.Models.PersonalGrowth;
using HobbyXP.ViewModels.Common;

namespace HobbyXP.ViewModels.PersonalGrowth;

public sealed class CourseProgressRowViewModel : ViewModelBase
{
    private readonly Func<Course, DateTime, int, Task> _logSessionsAsync;
    private DateTime? _sessionDate = DateTime.Today;
    private int _sessionsToLog = 1;

    public CourseProgressRowViewModel(Course course, Func<Course, DateTime, int, Task> logSessionsAsync)
    {
        Course = course;
        _logSessionsAsync = logSessionsAsync;

        LogSessionsCommand = new AsyncRelayCommand(LogSessionsAsync, CanLogSessions);
        BumpSessionsCommand = new RelayCommand(BumpSessions);
        CompleteAllCommand = new RelayCommand(CompleteAll);
    }

    public Course Course { get; }

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

    public DateTime? SessionDate
    {
        get => _sessionDate;
        set
        {
            if (SetProperty(ref _sessionDate, value))
                LogSessionsCommand.RaiseCanExecuteChanged();
        }
    }

    public int SessionsToLog
    {
        get => _sessionsToLog;
        set
        {
            var clamped = Math.Clamp(value, 1, Math.Max(1, RemainingSessions));
            if (!SetProperty(ref _sessionsToLog, clamped))
                return;

            LogSessionsCommand.RaiseCanExecuteChanged();
        }
    }

    public AsyncRelayCommand LogSessionsCommand { get; }

    public RelayCommand BumpSessionsCommand { get; }

    public RelayCommand CompleteAllCommand { get; }

    private bool CanLogSessions() =>
        RemainingSessions > 0 &&
        SessionDate.HasValue &&
        SessionsToLog > 0 &&
        SessionsToLog <= RemainingSessions;

    private void BumpSessions(object? parameter)
    {
        var amount = parameter switch
        {
            int value => value,
            string text when int.TryParse(text, out var parsed) => parsed,
            _ => 1
        };

        SessionsToLog = Math.Min(RemainingSessions, SessionsToLog + amount);
    }

    private void CompleteAll() => SessionsToLog = RemainingSessions;

    private async Task LogSessionsAsync()
    {
        if (!CanLogSessions() || !SessionDate.HasValue)
            return;

        await _logSessionsAsync(Course, SessionDate.Value, SessionsToLog);
        SessionsToLog = Math.Min(1, RemainingSessions);
        OnPropertyChanged(nameof(RemainingSessions));
        LogSessionsCommand.RaiseCanExecuteChanged();
    }
}
