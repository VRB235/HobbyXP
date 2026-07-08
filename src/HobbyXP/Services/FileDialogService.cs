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
}
