namespace HobbyXP.Services.Messaging;

public interface IApplicationDataResetMessenger
{
    event EventHandler? ApplicationDataReset;

    void NotifyReset();
}
