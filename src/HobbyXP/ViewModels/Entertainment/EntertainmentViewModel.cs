using HobbyXP.ViewModels.Common;
using HobbyXP.ViewModels.Navigation;

namespace HobbyXP.ViewModels.Entertainment;

public sealed class EntertainmentViewModel : LoadableViewModelBase
{
    private int _selectedTabIndex;

    public EntertainmentViewModel(
        PuzzlesViewModel puzzles,
        MediaViewModel media,
        VideoGamesViewModel videoGames)
    {
        Puzzles = puzzles;
        Media = media;
        VideoGames = videoGames;
    }

    public PuzzlesViewModel Puzzles { get; }

    public MediaViewModel Media { get; }

    public VideoGamesViewModel VideoGames { get; }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }

    protected override async Task LoadCoreAsync()
    {
        await Puzzles.LoadAsync();
        await Media.LoadAsync();
        await VideoGames.LoadAsync();
    }
}
