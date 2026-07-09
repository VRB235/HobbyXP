namespace HobbyXP.Services.Abstractions;

public interface IMessageDialogService
{
    bool Confirm(string message, string title = "Confirmar");
}
