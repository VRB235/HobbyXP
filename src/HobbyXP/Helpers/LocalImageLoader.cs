using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HobbyXP.Helpers;

public static class LocalImageLoader
{
    public static ImageSource? TryLoad(string? path)
    {
        var resolved = MedalIconPaths.ResolveAbsolutePath(path);
        if (resolved is null)
            return null;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(resolved, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }
}
