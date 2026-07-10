using HobbyXP.Services.Abstractions;
using Microsoft.Win32;

namespace HobbyXP.Services;

public sealed class FileDialogService : IFileDialogService
{
    public string? PickImageFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Seleccionar avatar",
            Filter = "Imágenes|*.png;*.jpg;*.jpeg;*.webp;*.bmp|Todos los archivos|*.*",
            Multiselect = false
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public IReadOnlyList<string> PickImageFiles()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Seleccionar fotos del rompecabezas",
            Filter = "Imágenes|*.png;*.jpg;*.jpeg;*.webp;*.bmp|Todos los archivos|*.*",
            Multiselect = true
        };

        return dialog.ShowDialog() == true
            ? dialog.FileNames
            : [];
    }

    public string? PickSaveFilePath(string suggestedFileName, string filter, string title)
    {
        var dialog = new SaveFileDialog
        {
            Title = title,
            Filter = filter,
            FileName = suggestedFileName,
            AddExtension = true,
            OverwritePrompt = true
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
