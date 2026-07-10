namespace HobbyXP.Services.Messaging;

public sealed class ApplicationDataResetMessenger : IApplicationDataResetMessenger
{
    public event EventHandler? ApplicationDataReset;

    public void NotifyReset() => ApplicationDataReset?.Invoke(this, EventArgs.Empty);
}
