using HobbyXP.ViewModels.Common;
using HobbyXP.ViewModels.Navigation;

namespace HobbyXP.ViewModels.PersonalGrowth;

public sealed class PersonalGrowthViewModel : LoadableViewModelBase
{
    private int _selectedTabIndex;

    public PersonalGrowthViewModel(BooksViewModel books, CoursesViewModel courses)
    {
        Books = books;
        Courses = courses;
    }

    public BooksViewModel Books { get; }

    public CoursesViewModel Courses { get; }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }

    protected override async Task LoadCoreAsync()
    {
        await Books.LoadAsync();
        await Courses.LoadAsync();
    }
}
