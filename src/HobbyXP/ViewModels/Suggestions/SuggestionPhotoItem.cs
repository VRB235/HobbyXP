using System.Windows.Media;
using HobbyXP.Helpers;

namespace HobbyXP.ViewModels.Suggestions;

public sealed class SuggestionPhotoItem
{
    private SuggestionPhotoItem(string filePath, ImageSource preview)
    {
        FilePath = filePath;
        Preview = preview;
    }

    public string FilePath { get; }

    public ImageSource Preview { get; }

    public static SuggestionPhotoItem? TryCreate(string filePath) =>
        LocalImageLoader.TryLoad(filePath) is { } preview
            ? new SuggestionPhotoItem(filePath, preview)
            : null;
}
