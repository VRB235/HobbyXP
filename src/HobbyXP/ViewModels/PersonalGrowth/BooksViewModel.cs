using System.Collections.ObjectModel;
using HobbyXP.Helpers;
using HobbyXP.Models.Enums;
using HobbyXP.Models.PersonalGrowth;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Messaging;
using HobbyXP.ViewModels.Common;
using HobbyXP.ViewModels.Messaging;

namespace HobbyXP.ViewModels.PersonalGrowth;

public sealed class BooksViewModel : AchievementAwareViewModel
{
    private readonly IBookService _bookService;
    private readonly IProfileRefreshMessenger _profileRefreshMessenger;
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
        IProfileRefreshMessenger profileRefreshMessenger,
        IAchievementMessenger achievementMessenger)
        : base(achievementMessenger)
    {
        _bookService = bookService;
        _profileRefreshMessenger = profileRefreshMessenger;
        HobbyXp = new HobbyProgressPresenter(xpService, MilestoneSourceType.Book, weeklyQuotaService);
        ReadingRows = new ObservableCollection<BookReadingRowViewModel>();
        CompletedRows = new ObservableCollection<BookCompletedRowViewModel>();
        RegisterCommand = new AsyncRelayCommand(RegisterAsync, CanRegister);
        ClearCompletedDateFilterCommand = new RelayCommand(ClearCompletedDateFilter);
        RefreshRegisterValidation();
    }

    public HobbyProgressPresenter HobbyXp { get; }

    public ObservableCollection<BookReadingRowViewModel> ReadingRows { get; }

    public ObservableCollection<BookCompletedRowViewModel> CompletedRows { get; }

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

    protected override Task LoadCoreAsync() => ReloadAsync();

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
            ReadingRows.Add(new BookReadingRowViewModel(book, ApplyPagesAsync, UpdateMetadataAsync));

        CompletedRows.Clear();
        foreach (var book in _allCompleted.Where(MatchesCompletedDateFilter))
            CompletedRows.Add(new BookCompletedRowViewModel(book, UpdateMetadataAsync));
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
            var book = await _bookService.RegisterAsync(Title, Author, pages);
            _allReading.Insert(0, book);
            ApplyFilter();

            Title = string.Empty;
            Author = string.Empty;
            TotalPages = "300";
            ClearValidation();
            StatusMessage = $"Libro '{book.Title}' agregado.";
        }, "Registrando libro...");
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
}
