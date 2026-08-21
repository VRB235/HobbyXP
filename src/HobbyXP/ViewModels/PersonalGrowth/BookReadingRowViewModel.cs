using HobbyXP.Helpers;
using HobbyXP.Models.PersonalGrowth;
using HobbyXP.Services.Abstractions;
using HobbyXP.ViewModels.Common;

namespace HobbyXP.ViewModels.PersonalGrowth;

public sealed class BookReadingRowViewModel : ViewModelBase
{
    private readonly Func<Book, int, DateTime, Task> _applyAsync;
    private readonly Func<Book, string, string, Task> _updateMetadataAsync;
    private readonly Func<Book, string?, bool, Task<Book>> _updateImageAsync;
    private int _targetPagesRead;
    private DateTime? _readingDate = DateTime.Today;
    private string _editTitle;
    private string _editAuthor;
    private string? _validationMessage;
    private string? _metadataValidationMessage;

    public BookReadingRowViewModel(
        Book book,
        Func<Book, int, DateTime, Task> applyAsync,
        Func<Book, string, string, Task> updateMetadataAsync,
        Func<Book, string?, bool, Task<Book>> updateImageAsync,
        IFileDialogService fileDialogService)
    {
        Book = book;
        _applyAsync = applyAsync;
        _updateMetadataAsync = updateMetadataAsync;
        _updateImageAsync = updateImageAsync;
        _targetPagesRead = book.PagesRead;
        _editTitle = book.Title;
        _editAuthor = book.Author;

        Cover = new ProgressCoverController(
            HobbyCoverPhotoStorage.Folders.Books,
            book.ImageDisplayPath,
            fileDialogService,
            PersistCoverAsync);

        ApplyPagesCommand = new AsyncRelayCommand(ApplyPagesAsync, CanApplyPages);
        BumpPagesCommand = new RelayCommand(BumpPages);
        SetCompleteCommand = new RelayCommand(SetComplete);
        SaveMetadataCommand = new AsyncRelayCommand(SaveMetadataAsync, CanSaveMetadata);
        RefreshValidation();
        RefreshMetadataValidation();
    }

    public Book Book { get; private set; }

    public ProgressCoverController Cover { get; }

    public string? ImageDisplayPath => Cover.ImageDisplayPath;

    public bool HasImage => Cover.HasImage;

    public string ImageActionLabel => Cover.ImageActionLabel;

    public AsyncRelayCommand PickImageCommand => Cover.PickCommand;

    public AsyncRelayCommand ClearImageCommand => Cover.ClearCommand;

    public string Title => Book.Title;

    public string Author => Book.Author;

    public string EditTitle
    {
        get => _editTitle;
        set
        {
            if (SetProperty(ref _editTitle, value))
                RefreshMetadataValidation();
        }
    }

    public string EditAuthor
    {
        get => _editAuthor;
        set
        {
            if (SetProperty(ref _editAuthor, value))
                RefreshMetadataValidation();
        }
    }

    public DateTime? ReadingDate
    {
        get => _readingDate;
        set
        {
            if (SetProperty(ref _readingDate, value))
                RefreshValidation();
        }
    }

    public int TotalPages => Book.TotalPages;

    public int XpEarned => Book.XpEarned;

    public string XpEarnedDisplay => $"XP: {XpEarned}";

    public int CurrentPagesRead => Book.PagesRead;

    public string? ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public string? MetadataValidationMessage
    {
        get => _metadataValidationMessage;
        private set => SetProperty(ref _metadataValidationMessage, value);
    }

    public double ProgressPercent => ToPercent(CurrentPagesRead);

    public double TargetPercent
    {
        get => ToPercent(TargetPagesRead);
        set
        {
            if (TotalPages <= 0)
                return;

            var minPercent = ToPercent(CurrentPagesRead);
            var clamped = Math.Clamp(value, minPercent, 100d);
            var pages = (int)Math.Round(TotalPages * clamped / 100d);
            TargetPagesRead = pages;
        }
    }

    public int TargetPagesRead
    {
        get => _targetPagesRead;
        set
        {
            if (!SetProperty(ref _targetPagesRead, value))
                return;

            OnPropertyChanged(nameof(TargetPercent));
            OnPropertyChanged(nameof(HasPendingChange));
            OnPropertyChanged(nameof(ProgressSummary));
            RefreshValidation();
        }
    }

    public bool HasPendingChange => TargetPagesRead > CurrentPagesRead;

    public bool HasPendingMetadataChange =>
        !string.Equals(EditTitle.Trim(), Book.Title, StringComparison.Ordinal) ||
        !string.Equals(EditAuthor.Trim(), Book.Author, StringComparison.Ordinal);

    public string ProgressSummary => HasPendingChange
        ? $"Actual: {CurrentPagesRead}/{TotalPages} → Nuevo: {TargetPagesRead}/{TotalPages}"
        : $"Progreso actual: {CurrentPagesRead}/{TotalPages} páginas";

    public AsyncRelayCommand ApplyPagesCommand { get; }

    public RelayCommand BumpPagesCommand { get; }

    public RelayCommand SetCompleteCommand { get; }

    public AsyncRelayCommand SaveMetadataCommand { get; }

    private async Task<string?> PersistCoverAsync(string? imageSourcePath, bool clearImage)
    {
        var updated = await _updateImageAsync(Book, imageSourcePath, clearImage);
        Book = updated;
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Author));
        OnPropertyChanged(nameof(XpEarned));
        OnPropertyChanged(nameof(XpEarnedDisplay));
        return updated.ImageDisplayPath;
    }

    private double ToPercent(int pages) =>
        TotalPages > 0 ? (double)pages / TotalPages * 100d : 0d;

    private ValidationResult ValidateForm()
    {
        if (TotalPages <= 0)
            return ValidationResult.Fail("El libro no tiene páginas totales definidas.");

        if (!ReadingDate.HasValue)
            return ValidationResult.Fail("Indique la fecha de la lectura.");

        if (TargetPagesRead < CurrentPagesRead)
            return ValidationResult.Fail("Las páginas leídas no pueden ser menores al progreso actual.");

        return FormValidation.RequireNotAbove(
            TargetPagesRead,
            TotalPages,
            "Las páginas leídas",
            "páginas");
    }

    private ValidationResult ValidateMetadataForm() =>
        FormValidation.FirstFailure(
            FormValidation.RequireText(EditTitle, "el título"),
            FormValidation.RequireText(EditAuthor, "el autor"));

    private void RefreshValidation()
    {
        var result = ValidateForm();
        ValidationMessage = result.IsValid ? null : result.Message;
        ApplyPagesCommand.RaiseCanExecuteChanged();
    }

    private void RefreshMetadataValidation()
    {
        var result = ValidateMetadataForm();
        MetadataValidationMessage = result.IsValid ? null : result.Message;
        OnPropertyChanged(nameof(HasPendingMetadataChange));
        SaveMetadataCommand.RaiseCanExecuteChanged();
    }

    private bool CanApplyPages() => HasPendingChange && ValidateForm().IsValid;

    private bool CanSaveMetadata() => HasPendingMetadataChange && ValidateMetadataForm().IsValid;

    private void BumpPages(object? parameter)
    {
        var amount = parameter switch
        {
            int value => value,
            string text when int.TryParse(text, out var parsed) => parsed,
            _ => 10
        };

        TargetPagesRead = TargetPagesRead + amount;
    }

    private void SetComplete() => TargetPagesRead = TotalPages;

    private async Task ApplyPagesAsync()
    {
        if (!ValidateForm().IsValid || !ReadingDate.HasValue)
        {
            RefreshValidation();
            return;
        }

        if (!HasPendingChange)
            return;

        await _applyAsync(Book, TargetPagesRead, ReadingDate.Value);
        ValidationMessage = null;
    }

    private async Task SaveMetadataAsync()
    {
        if (!ValidateMetadataForm().IsValid)
        {
            RefreshMetadataValidation();
            return;
        }

        if (!HasPendingMetadataChange)
            return;

        await _updateMetadataAsync(Book, EditTitle.Trim(), EditAuthor.Trim());
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Author));
        OnPropertyChanged(nameof(HasPendingMetadataChange));
        SaveMetadataCommand.RaiseCanExecuteChanged();
        MetadataValidationMessage = null;
    }
}
