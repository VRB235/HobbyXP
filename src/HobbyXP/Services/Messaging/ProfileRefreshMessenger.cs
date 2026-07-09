namespace HobbyXP.Services.Messaging;

public sealed class ProfileRefreshMessenger : IProfileRefreshMessenger
{
    public event EventHandler? ProfileRefreshRequested;

    public void RequestRefresh() => ProfileRefreshRequested?.Invoke(this, EventArgs.Empty);
}
