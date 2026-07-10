using System.Windows;
using System.Windows.Controls;

namespace HobbyXP.Views.Controls;

public partial class FormValidationBanner : UserControl
{
    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(
            nameof(Message),
            typeof(string),
            typeof(FormValidationBanner),
            new PropertyMetadata(null));

    public FormValidationBanner() => InitializeComponent();

    public string? Message
    {
        get => (string?)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }
}
