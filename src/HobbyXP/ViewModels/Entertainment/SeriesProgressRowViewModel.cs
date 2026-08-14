using HobbyXP.Helpers;
using HobbyXP.Models.Entertainment;
using HobbyXP.ViewModels.Common;

namespace HobbyXP.ViewModels.Entertainment;

public sealed class SeriesProgressRowViewModel : ViewModelBase
{
    private readonly Func<MediaSeries, DateTime, int, Task> _logChaptersAsync;
    private DateTime? _watchDate = DateTime.Today;
    private int _chaptersToLog = 1;
    private string? _validationMessage;

    public SeriesProgressRowViewModel(
        MediaSeries series,
        Func<MediaSeries, DateTime, int, Task> logChaptersAsync)
    {
        Series = series;
        _logChaptersAsync = logChaptersAsync;

        LogChaptersCommand = new AsyncRelayCommand(LogChaptersAsync, CanLogChapters);
        BumpChaptersCommand = new RelayCommand(BumpChapters);
        CompleteAllCommand = new RelayCommand(CompleteAll);
        RefreshValidation();
    }

    public MediaSeries Series { get; }

    public string Title => Series.Title;

    public int TotalChapters => Series.TotalChapters;

    public int ChaptersWatched => Series.ChaptersWatched;

    public int RemainingChapters => Math.Max(0, TotalChapters - ChaptersWatched);

    public int XpEarned => Series.XpEarned;

    public string XpEarnedDisplay => $"XP: {XpEarned}";

    public double ProgressPercent =>
        TotalChapters > 0 ? (double)ChaptersWatched / TotalChapters * 100d : 0d;

    public string ProgressSummary =>
        $"Progreso: {ChaptersWatched}/{TotalChapters} capítulos";

    public string? ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public DateTime? WatchDate
    {
        get => _watchDate;
        set
        {
            if (SetProperty(ref _watchDate, value))
                RefreshValidation();
        }
    }

    public int ChaptersToLog
    {
        get => _chaptersToLog;
        set
        {
            if (!SetProperty(ref _chaptersToLog, value))
                return;

            RefreshValidation();
        }
    }

    public AsyncRelayCommand LogChaptersCommand { get; }

    public RelayCommand BumpChaptersCommand { get; }

    public RelayCommand CompleteAllCommand { get; }

    private ValidationResult ValidateForm()
    {
        if (RemainingChapters <= 0)
            return ValidationResult.Fail("Esta serie ya está completada.");

        if (!WatchDate.HasValue)
            return ValidationResult.Fail("Indique la fecha del visionado.");

        if (ChaptersToLog <= 0)
            return ValidationResult.Fail("Los capítulos a registrar deben ser mayor que cero.");

        return FormValidation.RequireNotAbove(
            ChaptersToLog,
            RemainingChapters,
            "Los capítulos a registrar",
            "capítulos");
    }

    private void RefreshValidation()
    {
        var result = ValidateForm();
        ValidationMessage = result.IsValid ? null : result.Message;
        LogChaptersCommand.RaiseCanExecuteChanged();
    }

    private bool CanLogChapters() => ValidateForm().IsValid;

    private void BumpChapters(object? parameter)
    {
        var amount = parameter switch
        {
            int value => value,
            string text when int.TryParse(text, out var parsed) => parsed,
            _ => 1
        };

        ChaptersToLog = ChaptersToLog + amount;
    }

    private void CompleteAll() => ChaptersToLog = RemainingChapters;

    private async Task LogChaptersAsync()
    {
        if (!ValidateForm().IsValid)
        {
            RefreshValidation();
            return;
        }

        if (!WatchDate.HasValue)
            return;

        await _logChaptersAsync(Series, WatchDate.Value, ChaptersToLog);
        ChaptersToLog = Math.Min(1, RemainingChapters);
        OnPropertyChanged(nameof(RemainingChapters));
        RefreshValidation();
    }
}
