using HobbyXP.ViewModels.Navigation;
using System.Windows.Input;

namespace HobbyXP.ViewModels.Common;

public abstract class BusyViewModelBase : ViewModelBase
{
    private bool _isLoading;
    private string? _statusMessage;
    private string? _errorMessage;
    private string? _validationMessage;

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

    public string? ValidationMessage
    {
        get => _validationMessage;
        protected set => SetProperty(ref _validationMessage, value);
    }

    protected void ApplyValidation(Helpers.ValidationResult result) =>
        ValidationMessage = result.IsValid ? null : result.Message;

    protected void ClearValidation() => ValidationMessage = null;

    protected void RefreshValidation(Helpers.ValidationResult result, params ICommand[] commands)
    {
        ApplyValidation(result);
        foreach (var command in commands)
        {
            if (command is AsyncRelayCommand asyncCommand)
                asyncCommand.RaiseCanExecuteChanged();
        }

        CommandManager.InvalidateRequerySuggested();
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
            if (StatusMessage == busyMessage)
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

    public void InvalidateLoaded() => IsLoaded = false;
}
