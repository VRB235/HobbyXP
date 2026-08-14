using HobbyXP.Helpers;
using HobbyXP.Models.PersonalGrowth;
using HobbyXP.ViewModels.Common;

namespace HobbyXP.ViewModels.PersonalGrowth;

public sealed class BookCompletedRowViewModel : ViewModelBase
{
    private readonly Func<Book, string, string, Task> _updateMetadataAsync;
    private string _editTitle;
    private string _editAuthor;
    private string? _validationMessage;

    public BookCompletedRowViewModel(Book book, Func<Book, string, string, Task> updateMetadataAsync)
    {
        Book = book;
        _updateMetadataAsync = updateMetadataAsync;
        _editTitle = book.Title;
        _editAuthor = book.Author;

        SaveMetadataCommand = new AsyncRelayCommand(SaveMetadataAsync, CanSaveMetadata);
        RefreshValidation();
    }

    public Book Book { get; }

    public DateTime? CompletedAt => Book.CompletedAt;

    public int XpEarned => Book.XpEarned;

    public string EditTitle
    {
        get => _editTitle;
        set
        {
            if (SetProperty(ref _editTitle, value))
                RefreshValidation();
        }
    }

    public string EditAuthor
    {
        get => _editAuthor;
        set
        {
            if (SetProperty(ref _editAuthor, value))
                RefreshValidation();
        }
    }

    public string? ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public bool HasPendingChange =>
        !string.Equals(EditTitle.Trim(), Book.Title, StringComparison.Ordinal) ||
        !string.Equals(EditAuthor.Trim(), Book.Author, StringComparison.Ordinal);

    public AsyncRelayCommand SaveMetadataCommand { get; }

    private ValidationResult ValidateForm() =>
        FormValidation.FirstFailure(
            FormValidation.RequireText(EditTitle, "el título"),
            FormValidation.RequireText(EditAuthor, "el autor"));

    private void RefreshValidation()
    {
        var result = ValidateForm();
        ValidationMessage = result.IsValid ? null : result.Message;
        OnPropertyChanged(nameof(HasPendingChange));
        SaveMetadataCommand.RaiseCanExecuteChanged();
    }

    private bool CanSaveMetadata() => HasPendingChange && ValidateForm().IsValid;

    private async Task SaveMetadataAsync()
    {
        if (!ValidateForm().IsValid)
        {
            RefreshValidation();
            return;
        }

        if (!HasPendingChange)
            return;

        await _updateMetadataAsync(Book, EditTitle.Trim(), EditAuthor.Trim());
        OnPropertyChanged(nameof(HasPendingChange));
        SaveMetadataCommand.RaiseCanExecuteChanged();
        ValidationMessage = null;
    }
}
