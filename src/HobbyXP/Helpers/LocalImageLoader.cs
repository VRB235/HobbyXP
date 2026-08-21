using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HobbyXP.Data;

namespace HobbyXP.Helpers;

public static class LocalImageLoader
{
    public static ImageSource? TryLoad(string? path)
    {
        var resolved = ResolveExistingPath(path);
        if (resolved is null)
            return null;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
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

    private static string? ResolveExistingPath(string? path)
    {
        var fromMedals = MedalIconPaths.ResolveAbsolutePath(path);
        if (fromMedals is not null)
            return fromMedals;

        var fromReward = RewardPhotoStorage.ResolveAbsolutePath(path);
        if (fromReward is not null)
            return fromReward;

        var fromRace = RacePhotoStorage.ResolveAbsolutePath(path);
        if (fromRace is not null)
            return fromRace;

        var fromCover = HobbyCoverPhotoStorage.ResolveAbsolutePath(path);
        if (fromCover is not null)
            return fromCover;

        if (string.IsNullOrWhiteSpace(path))
            return null;

        if (Path.IsPathRooted(path))
        {
            var absolute = Path.GetFullPath(path);
            return File.Exists(absolute) ? absolute : null;
        }

        var fromData = Path.Combine(DatabaseConstants.GetDatabaseDirectory(), path);
        return File.Exists(fromData) ? fromData : null;
    }
}
