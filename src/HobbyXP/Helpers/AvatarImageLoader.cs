using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HobbyXP.Helpers;

public static class AvatarImageLoader
{
    private static readonly ImageSource DefaultAvatar;

    static AvatarImageLoader()
    {
        DefaultAvatar = new DrawingImage(
            new GeometryDrawing(
                new SolidColorBrush(Color.FromRgb(42, 51, 71)),
                new Pen(new SolidColorBrush(Color.FromRgb(124, 77, 255)), 2),
                new EllipseGeometry(new System.Windows.Point(24, 24), 22, 22)));
    }

    public static ImageSource LoadOrDefault(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return DefaultAvatar;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return DefaultAvatar;
        }
    }
}
