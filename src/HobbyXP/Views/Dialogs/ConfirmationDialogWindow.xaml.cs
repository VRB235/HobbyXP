using System.Windows;

namespace HobbyXP.Views.Dialogs;

public partial class ConfirmationDialogWindow : Window
{
    public ConfirmationDialogWindow(string message, string title)
    {
        InitializeComponent();
        TitleText.Text = title;
        MessageText.Text = message;
    }

    private void OnYesClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void OnNoClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
