using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HobbyXP.Views.Controls;

public partial class ProgressCoverThumb : UserControl
{
    public static readonly DependencyProperty ImagePathProperty =
        DependencyProperty.Register(
            nameof(ImagePath),
            typeof(string),
            typeof(ProgressCoverThumb),
            new PropertyMetadata(null, OnImagePathChanged));

    public static readonly DependencyProperty HasImageProperty =
        DependencyProperty.Register(nameof(HasImage), typeof(bool), typeof(ProgressCoverThumb));

    public static readonly DependencyProperty ImageActionLabelProperty =
        DependencyProperty.Register(
            nameof(ImageActionLabel),
            typeof(string),
            typeof(ProgressCoverThumb),
            new PropertyMetadata("Imagen"));

    public static readonly DependencyProperty PickCommandProperty =
        DependencyProperty.Register(nameof(PickCommand), typeof(ICommand), typeof(ProgressCoverThumb));

    public static readonly DependencyProperty ClearCommandProperty =
        DependencyProperty.Register(nameof(ClearCommand), typeof(ICommand), typeof(ProgressCoverThumb));

    public ProgressCoverThumb()
    {
        InitializeComponent();
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

    public string ImageActionLabel
    {
        get => (string)GetValue(ImageActionLabelProperty);
        set => SetValue(ImageActionLabelProperty, value);
    }

    public ICommand? PickCommand
    {
        get => (ICommand?)GetValue(PickCommandProperty);
        set => SetValue(PickCommandProperty, value);
    }

    public ICommand? ClearCommand
    {
        get => (ICommand?)GetValue(ClearCommandProperty);
        set => SetValue(ClearCommandProperty, value);
    }

    private static void OnImagePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (ProgressCoverThumb)d;
        var path = e.NewValue as string;
        var hasImage = !string.IsNullOrWhiteSpace(path);
        control.SetCurrentValue(HasImageProperty, hasImage);
        control.SetCurrentValue(ImageActionLabelProperty, hasImage ? "Cambiar" : "Imagen");
    }
}
