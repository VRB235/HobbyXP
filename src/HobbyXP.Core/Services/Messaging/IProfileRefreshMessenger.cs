namespace HobbyXP.Services.Messaging;

public interface IProfileRefreshMessenger
{
    event EventHandler? ProfileRefreshRequested;

    void RequestRefresh();
}
