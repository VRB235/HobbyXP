using System.Windows.Media;
using HobbyXP.Helpers;

namespace HobbyXP.ViewModels.Entertainment;

public sealed class PuzzlePhotoItem
{
    private PuzzlePhotoItem(string filePath, ImageSource preview)
    {
        FilePath = filePath;
        Preview = preview;
    }

    public string FilePath { get; }

    public ImageSource Preview { get; }

    public static PuzzlePhotoItem? TryCreate(string filePath) =>
        LocalImageLoader.TryLoad(filePath) is { } preview
            ? new PuzzlePhotoItem(filePath, preview)
            : null;
}
