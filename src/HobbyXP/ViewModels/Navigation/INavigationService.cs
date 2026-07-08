namespace HobbyXP.ViewModels.Navigation;

public interface INavigationService
{
    object? CurrentViewModel { get; }

    NavigationSection CurrentSection { get; }

    event EventHandler? CurrentViewModelChanged;

    Task NavigateAsync(NavigationSection section);
}
