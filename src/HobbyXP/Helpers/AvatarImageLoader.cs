using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HobbyXP.Helpers;

public readonly record struct AvatarLoadResult(ImageSource? Image, bool HasCustomAvatar)
{
    public static AvatarLoadResult Empty { get; } = new(null, false);
}

public static class AvatarImageLoader
{
    public static AvatarLoadResult Load(string? storedPath)
    {
        var path = AvatarStorage.ResolvePath(storedPath);
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return AvatarLoadResult.Empty;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            return new AvatarLoadResult(bitmap, true);
        }
        catch
        {
            return AvatarLoadResult.Empty;
        }
    }
}
