using System.IO;
using System.Windows;
using System.Windows.Threading;
using HobbyXP.Helpers;
using HobbyXP.Services.Abstractions;

namespace HobbyXP.ViewModels.Common;

/// <summary>
/// Lógica compartida de portada en filas de progreso (pick/clear + preview).
/// </summary>
public sealed class ProgressCoverController : ViewModelBase
{
    private readonly string _folderName;
    private readonly IFileDialogService _fileDialogService;
    private readonly Func<string?, bool, Task<string?>> _persistAsync;
    private string? _imageDisplayPath;
    private bool _isBusy;

    public ProgressCoverController(
        string folderName,
        string? initialDisplayPath,
        IFileDialogService fileDialogService,
        Func<string?, bool, Task<string?>> persistAsync)
    {
        _folderName = folderName;
        _fileDialogService = fileDialogService;
        _persistAsync = persistAsync;
        _imageDisplayPath = initialDisplayPath;

        PickCommand = new AsyncRelayCommand(PickAsync, () => !IsBusy);
        ClearCommand = new AsyncRelayCommand(ClearAsync, () => !IsBusy && HasImage);
    }

    public string? ImageDisplayPath
    {
        get => _imageDisplayPath;
        private set
        {
            if (SetProperty(ref _imageDisplayPath, value))
            {
                OnPropertyChanged(nameof(HasImage));
                OnPropertyChanged(nameof(ImageActionLabel));
                ClearCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool HasImage => !string.IsNullOrWhiteSpace(ImageDisplayPath);

    public string ImageActionLabel => HasImage ? "Cambiar" : "Imagen";

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value))
                return;
            PickCommand.RaiseCanExecuteChanged();
            ClearCommand.RaiseCanExecuteChanged();
        }
    }

    public AsyncRelayCommand PickCommand { get; }

    public AsyncRelayCommand ClearCommand { get; }

    public void SyncFrom(string? displayPath) => SetImageDisplayPath(displayPath);

    private async Task PickAsync()
    {
        var path = _fileDialogService.PickImageFile();
        if (string.IsNullOrWhiteSpace(path))
            return;

        var staged = HobbyCoverPhotoStorage.ImportToStaging(_folderName, path);
        if (staged is null || !File.Exists(staged))
            return;

        SetBusy(true);
        try
        {
            var displayPath = await _persistAsync(staged, false).ConfigureAwait(true);
            SetImageDisplayPath(displayPath);
        }
        finally
        {
            HobbyCoverPhotoStorage.DeleteStagingFile(_folderName, staged);
            SetBusy(false);
        }
    }

    private async Task ClearAsync()
    {
        SetBusy(true);
        try
        {
            var displayPath = await _persistAsync(null, true).ConfigureAwait(true);
            SetImageDisplayPath(displayPath);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool value) => RunOnUi(() => IsBusy = value);

    private void SetImageDisplayPath(string? path) => RunOnUi(() => ImageDisplayPath = path);

    private static void RunOnUi(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.Invoke(action, DispatcherPriority.DataBind);
    }
}
