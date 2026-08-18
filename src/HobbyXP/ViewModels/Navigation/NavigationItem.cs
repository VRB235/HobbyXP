namespace HobbyXP.ViewModels.Navigation;

public sealed class NavigationItem : Common.ViewModelBase
{
    private bool _isActive;
    private int _badgeCount;

    public NavigationItem(NavigationSection section, string title, string iconGlyph)
    {
        Section = section;
        Title = title;
        IconGlyph = iconGlyph;
    }

    public NavigationSection Section { get; }

    public string Title { get; }

    public string IconGlyph { get; }

    public int BadgeCount
    {
        get => _badgeCount;
        set
        {
            if (SetProperty(ref _badgeCount, value))
            {
                OnPropertyChanged(nameof(HasBadge));
                OnPropertyChanged(nameof(BadgeText));
            }
        }
    }

    public bool HasBadge => BadgeCount > 0;

    public string BadgeText => BadgeCount > 9 ? "9+" : BadgeCount.ToString();

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }
}
