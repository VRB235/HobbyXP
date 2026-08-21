using System.Windows;
using HobbyXP.Models.Feedback;
using HobbyXP.Services.Abstractions;
using HobbyXP.Views.Dialogs;

namespace HobbyXP.Services;

public sealed class SuggestionDetailService : ISuggestionDetailService
{
    private readonly IImagePreviewService _imagePreviewService;

    public SuggestionDetailService(IImagePreviewService imagePreviewService)
    {
        _imagePreviewService = imagePreviewService;
    }

    public void Show(Suggestion suggestion)
    {
        ArgumentNullException.ThrowIfNull(suggestion);

        var dialog = new SuggestionDetailWindow(suggestion, _imagePreviewService)
        {
            Owner = Application.Current.MainWindow
        };
        dialog.ShowDialog();
    }
}
