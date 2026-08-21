using HobbyXP.Helpers;
using HobbyXP.Models.Entertainment;
using HobbyXP.Models.Enums;
using HobbyXP.Services.Abstractions;
using HobbyXP.ViewModels.Common;

namespace HobbyXP.ViewModels.Entertainment;

public sealed class VideoGameProgressRowViewModel : ViewModelBase
{
    private readonly Func<VideoGame, int, DateTime, Task> _applyAsync;
    private readonly Func<VideoGame, string?, bool, Task<VideoGame>> _updateImageAsync;
    private int _targetCompletion;
    private DateTime? _progressDate = DateTime.Today;

    public VideoGameProgressRowViewModel(
        VideoGame game,
        Func<VideoGame, int, DateTime, Task> applyAsync,
        Func<VideoGame, string?, bool, Task<VideoGame>> updateImageAsync,
        IFileDialogService fileDialogService)
    {
        Game = game;
        _applyAsync = applyAsync;
        _updateImageAsync = updateImageAsync;
        _targetCompletion = game.CompletionPercentage;

        Cover = new ProgressCoverController(
            HobbyCoverPhotoStorage.Folders.VideoGames,
            game.ImageDisplayPath,
            fileDialogService,
            PersistCoverAsync);

        ApplyProgressCommand = new AsyncRelayCommand(ApplyProgressAsync, CanApply);
        BumpProgressCommand = new RelayCommand(BumpProgress);
        SetFullProgressCommand = new RelayCommand(SetFullProgress);
    }

    public VideoGame Game { get; private set; }

    public ProgressCoverController Cover { get; }

    public string? ImageDisplayPath => Cover.ImageDisplayPath;

    public bool HasImage => Cover.HasImage;

    public string ImageActionLabel => Cover.ImageActionLabel;

    public AsyncRelayCommand PickImageCommand => Cover.PickCommand;

    public AsyncRelayCommand ClearImageCommand => Cover.ClearCommand;

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

    private async Task<string?> PersistCoverAsync(string? imageSourcePath, bool clearImage)
    {
        var updated = await _updateImageAsync(Game, imageSourcePath, clearImage);
        Game = updated;
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(PlatformLabel));
        OnPropertyChanged(nameof(XpEarned));
        OnPropertyChanged(nameof(CurrentCompletion));
        OnPropertyChanged(nameof(ProgressPercent));
        OnPropertyChanged(nameof(ProgressSummary));
        return updated.ImageDisplayPath;
    }

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
