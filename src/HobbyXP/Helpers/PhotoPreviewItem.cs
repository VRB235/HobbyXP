using System.Windows.Media;

namespace HobbyXP.Helpers;

/// <summary>
/// Miniatura de foto local con ruta para poder abrir/ampliar.
/// </summary>
public sealed class PhotoPreviewItem
{
    public PhotoPreviewItem(string filePath, ImageSource image)
    {
        FilePath = filePath;
        Image = image;
    }

    public string FilePath { get; }

    public ImageSource Image { get; }
}
