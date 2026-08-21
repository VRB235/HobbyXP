using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HobbyXP.Views.Controls;

public partial class HistoryCoverCard : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(HistoryCoverCard));

    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(HistoryCoverCard));

    public static readonly DependencyProperty DateTextProperty =
        DependencyProperty.Register(nameof(DateText), typeof(string), typeof(HistoryCoverCard));

    public static readonly DependencyProperty ImagePathProperty =
        DependencyProperty.Register(nameof(ImagePath), typeof(string), typeof(HistoryCoverCard));

    public static readonly DependencyProperty HasImageProperty =
        DependencyProperty.Register(nameof(HasImage), typeof(bool), typeof(HistoryCoverCard));

    public static readonly DependencyProperty ItemProperty =
        DependencyProperty.Register(nameof(Item), typeof(object), typeof(HistoryCoverCard));

    public static readonly DependencyProperty OpenCommandProperty =
        DependencyProperty.Register(nameof(OpenCommand), typeof(ICommand), typeof(HistoryCoverCard));

    public static readonly DependencyProperty DeleteCommandProperty =
        DependencyProperty.Register(nameof(DeleteCommand), typeof(ICommand), typeof(HistoryCoverCard));

    public HistoryCoverCard()
    {
        InitializeComponent();
    }

    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Subtitle
    {
        get => (string?)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public string? DateText
    {
        get => (string?)GetValue(DateTextProperty);
        set => SetValue(DateTextProperty, value);
    }

    public string? ImagePath
    {
        get => (string?)GetValue(ImagePathProperty);
        set => SetValue(ImagePathProperty, value);
    }

    public bool HasImage
    {
        get => (bool)GetValue(HasImageProperty);
        set => SetValue(HasImageProperty, value);
    }

    public object? Item
    {
        get => GetValue(ItemProperty);
        set => SetValue(ItemProperty, value);
    }

    public ICommand? OpenCommand
    {
        get => (ICommand?)GetValue(OpenCommandProperty);
        set => SetValue(OpenCommandProperty, value);
    }

    public ICommand? DeleteCommand
    {
        get => (ICommand?)GetValue(DeleteCommandProperty);
        set => SetValue(DeleteCommandProperty, value);
    }
}
