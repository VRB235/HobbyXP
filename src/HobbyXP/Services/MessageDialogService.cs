using System.Windows;
using HobbyXP.Services.Abstractions;
using HobbyXP.Views.Dialogs;

namespace HobbyXP.Services;

public sealed class MessageDialogService : IMessageDialogService
{
    public bool Confirm(string message, string title = "Confirmar")
    {
        var dialog = new ConfirmationDialogWindow(message, title)
        {
            Owner = Application.Current.MainWindow
        };

        return dialog.ShowDialog() == true;
    }
}
