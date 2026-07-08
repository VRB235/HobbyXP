using HobbyXP.ViewModels.Navigation;

namespace HobbyXP.ViewModels.Common;

public abstract class BusyViewModelBase : ViewModelBase
{
    private bool _isLoading;
    private string? _statusMessage;
    private string? _errorMessage;

    public bool IsLoading
    {
        get => _isLoading;
        protected set => SetProperty(ref _isLoading, value);
    }

    public string? StatusMessage
    {
        get => _statusMessage;
        protected set => SetProperty(ref _statusMessage, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        protected set => SetProperty(ref _errorMessage, value);
    }

    protected async Task RunBusyAsync(Func<Task> action, string? busyMessage = null)
    {
        try
        {
            IsLoading = true;
            StatusMessage = busyMessage;
            ErrorMessage = null;
            await action();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            StatusMessage = null;
        }
    }
}

public abstract class LoadableViewModelBase : BusyViewModelBase, INavigatableViewModel
{
    private bool _isLoaded;

    public bool IsLoaded
    {
        get => _isLoaded;
        protected set => SetProperty(ref _isLoaded, value);
    }

    public async Task LoadAsync()
    {
        if (IsLoading)
            return;

        try
        {
            IsLoading = true;
            ErrorMessage = null;
            await LoadCoreAsync();
            IsLoaded = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected abstract Task LoadCoreAsync();
}
