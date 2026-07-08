namespace HobbyXP.ViewModels.Navigation;

public sealed class NavigationItem : Common.ViewModelBase
{
    private bool _isActive;

    public NavigationItem(NavigationSection section, string title, string iconGlyph)
    {
        Section = section;
        Title = title;
        IconGlyph = iconGlyph;
    }

    public NavigationSection Section { get; }

    public string Title { get; }

    public string IconGlyph { get; }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }
}
