using System.Collections.ObjectModel;
using HobbyXP.Models.Entertainment;
using HobbyXP.Models.Enums;
using HobbyXP.Services.Abstractions;
using HobbyXP.ViewModels.Common;
using HobbyXP.ViewModels.Messaging;

namespace HobbyXP.ViewModels.Entertainment;

public sealed class PuzzlesViewModel : AchievementAwareViewModel
{
    private readonly IPuzzleService _puzzleService;
    private string _name = string.Empty;
    private string _pieceCount = "500";
    private PuzzleCategory _category = PuzzleCategory.TwoD;
    private string? _photoPath;

    public PuzzlesViewModel(IPuzzleService puzzleService, IAchievementMessenger achievementMessenger)
        : base(achievementMessenger)
    {
        _puzzleService = puzzleService;
        Puzzles = new ObservableCollection<Puzzle>();
        RegisterCommand = new AsyncRelayCommand(RegisterAsync, CanRegister);
    }

    public ObservableCollection<Puzzle> Puzzles { get; }

    public Array Categories => Enum.GetValues(typeof(PuzzleCategory));

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string PieceCount
    {
        get => _pieceCount;
        set => SetProperty(ref _pieceCount, value);
    }

    public PuzzleCategory Category
    {
        get => _category;
        set => SetProperty(ref _category, value);
    }

    public string? PhotoPath
    {
        get => _photoPath;
        set => SetProperty(ref _photoPath, value);
    }

    public AsyncRelayCommand RegisterCommand { get; }

    protected override async Task LoadCoreAsync()
    {
        var items = await _puzzleService.GetAllAsync();
        Puzzles.Clear();
        foreach (var puzzle in items)
            Puzzles.Add(puzzle);
    }

    private bool CanRegister() =>
        !string.IsNullOrWhiteSpace(Name) && int.TryParse(PieceCount, out var count) && count > 0;

    private async Task RegisterAsync()
    {
        if (!CanRegister())
            return;

        var pieces = int.Parse(PieceCount);
        await RunBusyAsync(async () =>
        {
            var result = await _puzzleService.RegisterCompletedAsync(Name, pieces, Category, PhotoPath);
            PublishAchievements(result.Events);
            Puzzles.Insert(0, result.Value);

            Name = string.Empty;
            PieceCount = "500";
            PhotoPath = null;
            StatusMessage = $"Rompecabezas registrado · +{result.Value.XpEarned} XP";
        }, "Guardando rompecabezas...");
    }
}
