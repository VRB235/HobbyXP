using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace HobbyXP.Views.Dialogs;

public partial class ImagePreviewWindow : Window
{
    private readonly string _filePath;

    public ImagePreviewWindow(string filePath, ImageSource image, string title)
    {
        InitializeComponent();
        _filePath = filePath;
        TitleText.Text = title;
        PreviewImage.Source = image;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => DialogResult = true;

    private void OnOpenExternallyClick(object sender, RoutedEventArgs e)
    {
        if (!File.Exists(_filePath))
            return;

        Process.Start(new ProcessStartInfo
        {
            FileName = _filePath,
            UseShellExecute = true
        });
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            DialogResult = true;
    }
}
