using System.Diagnostics;
using System.IO;
using System.Windows;
using HobbyXP.Helpers;
using HobbyXP.Services.Abstractions;
using HobbyXP.Views.Dialogs;

namespace HobbyXP.Services;

public sealed class ImagePreviewService : IImagePreviewService
{
    public void Show(string filePath, string? title = null)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return;

        var image = LocalImageLoader.TryLoad(filePath);
        if (image is null)
            return;

        var dialog = new ImagePreviewWindow(filePath, image, title ?? Path.GetFileName(filePath))
        {
            Owner = Application.Current.MainWindow
        };
        dialog.ShowDialog();
    }

    public void OpenExternally(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return;

        Process.Start(new ProcessStartInfo
        {
            FileName = filePath,
            UseShellExecute = true
        });
    }
}
