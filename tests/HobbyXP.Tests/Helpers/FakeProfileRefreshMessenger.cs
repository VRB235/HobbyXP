using HobbyXP.Services.Messaging;

namespace HobbyXP.Tests.Helpers;

public sealed class FakeProfileRefreshMessenger : IProfileRefreshMessenger
{
    public event EventHandler? ProfileRefreshRequested;

    public int RefreshRequestCount { get; private set; }

    public void RequestRefresh()
    {
        RefreshRequestCount++;
        ProfileRefreshRequested?.Invoke(this, EventArgs.Empty);
    }
}
