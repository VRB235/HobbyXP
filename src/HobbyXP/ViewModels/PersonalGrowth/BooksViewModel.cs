using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using HobbyXP.Helpers;
using HobbyXP.Models.Enums;
using HobbyXP.Models.PersonalGrowth;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Messaging;
using HobbyXP.ViewModels.Common;
using HobbyXP.ViewModels.Messaging;
using HobbyXP.Views.Dialogs;

namespace HobbyXP.ViewModels.PersonalGrowth;

public sealed class BooksViewModel : AchievementAwareViewModel
{
    private readonly IBookService _bookService;
    private readonly IFileDialogService _fileDialogService;
    private readonly IProfileRefreshMessenger _profileRefreshMessenger;
    private readonly CoverImageDraft _cover;
    private string _title = string.Empty;
    private string _author = string.Empty;
    private string _totalPages = "300";
    private DateTime? _completedFromDate;
    private DateTime? _completedToDate;
    private List<Book> _allReading = [];
    private List<Book> _allCompleted = [];

    public BooksViewModel(
        IBookService bookService,
        IXpService xpService,
        IWeeklyQuotaService weeklyQuotaService,
        IFileDialogService fileDialogService,
        IProfileRefreshMessenger profileRefreshMessenger,
        IAchievementMessenger achievementMessenger,
        IAchievementProgressService achievementProgress)
        : base(achievementMessenger)
    {
        _bookService = bookService;
        _fileDialogService = fileDialogService;
        _profileRefreshMessenger = profileRefreshMessenger;
        _cover = new CoverImageDraft(HobbyCoverPhotoStorage.Folders.Books);
        _cover.Changed += OnCoverChanged;

        HobbyXp = new HobbyProgressPresenter(xpService, MilestoneSourceType.Book, weeklyQuotaService, achievementProgress);
        ReadingRows = new ObservableCollection<BookReadingRowViewModel>();
        CompletedBooks = new ObservableCollection<Book>();
        RegisterCommand = new AsyncRelayCommand(RegisterAsync, CanRegister);
        ClearCompletedDateFilterCommand = new RelayCommand(ClearCompletedDateFilter);
        PickImageCommand = new RelayCommand(() => _cover.Pick(_fileDialogService));
        ClearImageCommand = new RelayCommand(() => _cover.Clear(), () => _cover.HasPreview);
        OpenDetailCommand = new RelayCommand(OpenDetail);
        RefreshRegisterValidation();
    }

    public HobbyProgressPresenter HobbyXp { get; }

    public ObservableCollection<BookReadingRowViewModel> ReadingRows { get; }

    public ObservableCollection<Book> CompletedBooks { get; }

    public string Title
    {
        get => _title;
        set
        {
            if (SetProperty(ref _title, value))
                RefreshRegisterValidation();
        }
    }

    public string Author
    {
        get => _author;
        set
        {
            if (SetProperty(ref _author, value))
                RefreshRegisterValidation();
        }
    }

    public string TotalPages
    {
        get => _totalPages;
        set
        {
            if (SetProperty(ref _totalPages, value))
                RefreshRegisterValidation();
        }
    }

    public string? PreviewImagePath => _cover.PreviewPath;

    public bool HasPreviewImage => _cover.HasPreview;

    public DateTime? CompletedFromDate
    {
        get => _completedFromDate;
        set
        {
            if (SetProperty(ref _completedFromDate, value))
                ApplyFilter();
        }
    }

    public DateTime? CompletedToDate
    {
        get => _completedToDate;
        set
        {
            if (SetProperty(ref _completedToDate, value))
                ApplyFilter();
        }
    }

    public AsyncRelayCommand RegisterCommand { get; }

    public RelayCommand ClearCompletedDateFilterCommand { get; }

    public RelayCommand PickImageCommand { get; }

    public RelayCommand ClearImageCommand { get; }

    public RelayCommand OpenDetailCommand { get; }

    protected override Task LoadCoreAsync() => ReloadAsync();

    private void OnCoverChanged()
    {
        OnPropertyChanged(nameof(PreviewImagePath));
        OnPropertyChanged(nameof(HasPreviewImage));
        CommandManager.InvalidateRequerySuggested();
    }

    private void ResetCover()
    {
        _cover.MarkSaved();
        _cover.Clear();
        OnCoverChanged();
    }

    private async Task ReloadAsync()
    {
        await HobbyXp.RefreshAsync();
        _allReading = (await _bookService.GetReadingAsync()).ToList();
        _allCompleted = (await _bookService.GetCompletedAsync()).ToList();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        ReadingRows.Clear();
        foreach (var book in _allReading)
            ReadingRows.Add(new BookReadingRowViewModel(
                book,
                ApplyPagesAsync,
                UpdateMetadataAsync,
                UpdateBookImageAsync,
                _fileDialogService));

        CompletedBooks.Clear();
        foreach (var book in _allCompleted.Where(MatchesCompletedDateFilter))
            CompletedBooks.Add(book);
    }

    private bool MatchesCompletedDateFilter(Book book) =>
        book.CompletedAt.HasValue
            ? DateRangeFilter.Matches(book.CompletedAt.Value, CompletedFromDate, CompletedToDate)
            : !CompletedFromDate.HasValue && !CompletedToDate.HasValue;

    private void ClearCompletedDateFilter()
    {
        _completedFromDate = null;
        _completedToDate = null;
        OnPropertyChanged(nameof(CompletedFromDate));
        OnPropertyChanged(nameof(CompletedToDate));
        ApplyFilter();
    }

    private ValidationResult ValidateRegisterForm() =>
        FormValidation.FirstFailure(
            FormValidation.RequireText(Title, "el título"),
            FormValidation.RequireText(Author, "el autor"),
            FormValidation.RequirePositiveInt(TotalPages, "Las páginas totales", out _));

    private void RefreshRegisterValidation() =>
        RefreshValidation(ValidateRegisterForm(), RegisterCommand);

    private bool CanRegister() => ValidateRegisterForm().IsValid;

    private async Task RegisterAsync()
    {
        if (!ValidateRegisterForm().IsValid)
        {
            RefreshRegisterValidation();
            return;
        }

        var pages = int.Parse(TotalPages);
        await RunBusyAsync(async () =>
        {
            var book = await _bookService.RegisterAsync(Title, Author, pages, _cover.PendingSourcePath);
            ResetCover();
            _allReading.Insert(0, book);
            ApplyFilter();

            Title = string.Empty;
            Author = string.Empty;
            TotalPages = "300";
            ClearValidation();
            StatusMessage = $"Libro '{book.Title}' agregado.";
        }, "Registrando libro...");
    }

    private void OpenDetail(object? parameter)
    {
        if (parameter is not Book book)
            return;

        var detailVm = new BookDetailViewModel(book, _bookService, _fileDialogService);
        var dialog = new BookDetailWindow(detailVm)
        {
            Owner = Application.Current.MainWindow
        };

        var accepted = dialog.ShowDialog() == true;
        if (!accepted || detailVm.SavedBook is null)
            return;

        _ = ReloadAsync();
        StatusMessage = $"Libro actualizado: {detailVm.SavedBook.Title}";
    }

    private async Task ApplyPagesAsync(Book book, int targetPagesRead, DateTime readingDate)
    {
        await RunBusyAsync(async () =>
        {
            var result = await _bookService.UpdatePagesReadAsync(book.Id, targetPagesRead, readingDate);
            PublishAchievements(result.Events);
            await ReloadAsync();
            _profileRefreshMessenger.RequestRefresh();
            StatusMessage = $"{result.Value.Title}: {result.Value.PagesRead}/{result.Value.TotalPages} páginas";
        }, "Actualizando lectura...");
    }

    private async Task UpdateMetadataAsync(Book book, string title, string author)
    {
        await RunBusyAsync(async () =>
        {
            var updated = await _bookService.UpdateMetadataAsync(book.Id, title, author);
            if (updated is null)
                return;

            book.Title = updated.Title;
            book.Author = updated.Author;
            StatusMessage = $"Datos actualizados: «{updated.Title}» · {updated.Author}";
        }, "Actualizando libro...");
    }

    private async Task<Book> UpdateBookImageAsync(Book book, string? imageSourcePath, bool clearImage)
    {
        var updated = await _bookService.UpdateImageAsync(book.Id, imageSourcePath, clearImage);
        var index = _allReading.FindIndex(b => b.Id == updated.Id);
        if (index >= 0)
            _allReading[index] = updated;

        StatusMessage = clearImage
            ? $"Portada quitada de «{updated.Title}»."
            : $"Portada actualizada: «{updated.Title}».";
        return updated;
    }
}
