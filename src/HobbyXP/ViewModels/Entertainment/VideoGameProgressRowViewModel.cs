using HobbyXP.Models.Entertainment;
using HobbyXP.Models.Enums;
using HobbyXP.ViewModels.Common;

namespace HobbyXP.ViewModels.Entertainment;

public sealed class VideoGameProgressRowViewModel : ViewModelBase
{
    private readonly Func<VideoGame, int, DateTime, Task> _applyAsync;
    private int _targetCompletion;
    private DateTime? _progressDate = DateTime.Today;

    public VideoGameProgressRowViewModel(VideoGame game, Func<VideoGame, int, DateTime, Task> applyAsync)
    {
        Game = game;
        _applyAsync = applyAsync;
        _targetCompletion = game.CompletionPercentage;

        ApplyProgressCommand = new AsyncRelayCommand(ApplyProgressAsync, CanApply);
        BumpProgressCommand = new RelayCommand(BumpProgress);
        SetFullProgressCommand = new RelayCommand(SetFullProgress);
    }

    public VideoGame Game { get; }

    public string Title => Game.Title;

    public VideoGamePlatform Platform => Game.Platform;

    public string PlatformLabel => Game.PlatformLabel;

    public DateTime? StartedAt => Game.StartedAt;

    public int XpEarned => Game.XpEarned;

    public int CurrentCompletion => Game.CompletionPercentage;

    public DateTime? ProgressDate
    {
        get => _progressDate;
        set
        {
            if (SetProperty(ref _progressDate, value))
                ApplyProgressCommand.RaiseCanExecuteChanged();
        }
    }

    public int TargetCompletion
    {
        get => _targetCompletion;
        set
        {
            var clamped = Math.Clamp(value, CurrentCompletion, 100);
            if (!SetProperty(ref _targetCompletion, clamped))
                return;

            OnPropertyChanged(nameof(HasPendingChange));
            OnPropertyChanged(nameof(ProgressSummary));
            ApplyProgressCommand.RaiseCanExecuteChanged();
        }
    }

    public bool HasPendingChange => TargetCompletion > CurrentCompletion;

    public string ProgressSummary => HasPendingChange
        ? $"Actual: {CurrentCompletion}% → Nuevo: {TargetCompletion}%"
        : $"Progreso actual: {CurrentCompletion}%";

    public double ProgressPercent => CurrentCompletion;

    public AsyncRelayCommand ApplyProgressCommand { get; }

    public RelayCommand BumpProgressCommand { get; }

    public RelayCommand SetFullProgressCommand { get; }

    private bool CanApply() => HasPendingChange && ProgressDate.HasValue;

    private void BumpProgress(object? parameter)
    {
        var amount = parameter switch
        {
            int value => value,
            string text when int.TryParse(text, out var parsed) => parsed,
            _ => 10
        };

        TargetCompletion = Math.Min(100, TargetCompletion + amount);
    }

    private void SetFullProgress() => TargetCompletion = 100;

    private async Task ApplyProgressAsync()
    {
        if (!HasPendingChange || !ProgressDate.HasValue)
            return;

        await _applyAsync(Game, TargetCompletion, ProgressDate.Value);
    }
}
