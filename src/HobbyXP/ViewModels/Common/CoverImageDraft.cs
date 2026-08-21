using HobbyXP.Helpers;
using HobbyXP.Services.Abstractions;

namespace HobbyXP.ViewModels.Common;

/// <summary>
/// Borrador de portada para formularios de alta/edición (staging + preview).
/// </summary>
public sealed class CoverImageDraft
{
    private readonly string _folderName;
    private string? _previewPath;
    private string? _pendingSourcePath;
    private bool _clearOnSave;

    public CoverImageDraft(string folderName, string? existingAbsolutePath = null)
    {
        _folderName = folderName;
        _previewPath = existingAbsolutePath;
    }

    public string? PreviewPath => _previewPath;

    public string? PendingSourcePath => _pendingSourcePath;

    public bool ClearOnSave => _clearOnSave;

    public bool HasPreview => !string.IsNullOrWhiteSpace(_previewPath);

    public event Action? Changed;

    public void Pick(IFileDialogService fileDialogService)
    {
        var path = fileDialogService.PickImageFile();
        if (string.IsNullOrWhiteSpace(path))
            return;

        DiscardPending();

        var persisted = HobbyCoverPhotoStorage.ImportToStaging(_folderName, path);
        if (persisted is null)
            return;

        _pendingSourcePath = persisted;
        _clearOnSave = false;
        _previewPath = persisted;
        Changed?.Invoke();
    }

    public void Clear()
    {
        DiscardPending();
        _pendingSourcePath = null;
        _clearOnSave = true;
        _previewPath = null;
        Changed?.Invoke();
    }

    public void DiscardPending()
    {
        if (_pendingSourcePath is null)
            return;

        HobbyCoverPhotoStorage.DeleteStagingFile(_folderName, _pendingSourcePath);
        _pendingSourcePath = null;
    }

    public void MarkSaved()
    {
        _pendingSourcePath = null;
        _clearOnSave = false;
    }
}
