using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HobbyXP.Models.Feedback;
using HobbyXP.Services.Abstractions;

namespace HobbyXP.Views.Dialogs;

public partial class SuggestionDetailWindow : Window
{
    private readonly IImagePreviewService _imagePreviewService;

    public SuggestionDetailWindow(Suggestion suggestion, IImagePreviewService imagePreviewService)
    {
        InitializeComponent();
        _imagePreviewService = imagePreviewService;
        DataContext = suggestion;
        ResolvedAtText.Text = FormatLocalDate(suggestion.ResolvedAt) ?? "—";
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        var hasPhotos = PhotosList.ItemsSource is IEnumerable items
            && items.Cast<object>().Any();
        NoPhotosText.Visibility = hasPhotos ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OnPhotoClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string path })
            _imagePreviewService.Show(path);
    }

    private void OnCopyDescriptionClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not Suggestion suggestion)
            return;

        var text = suggestion.Description ?? string.Empty;
        if (string.IsNullOrEmpty(text))
            return;

        Clipboard.SetText(text);
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => DialogResult = true;

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            DialogResult = true;
    }

    private static string? FormatLocalDate(DateTime? value)
    {
        if (value is null)
            return null;

        var utc = value.Value;
        var local = utc.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime()
            : utc.Kind == DateTimeKind.Utc
                ? utc.ToLocalTime()
                : utc;

        return local.ToString("dd/MM/yyyy");
    }
}
