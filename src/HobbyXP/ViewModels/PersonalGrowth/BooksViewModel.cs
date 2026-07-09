using System.Collections.ObjectModel;
using HobbyXP.Helpers;
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
        IProfileRefreshMessenger profileRefreshMessenger,
        IAchievementMessenger achievementMessenger)
        : base(achievementMessenger)
    {
        _bookService = bookService;
        _profileRefreshMessenger = profileRefreshMessenger;
        ReadingRows = new ObservableCollection<BookReadingRowViewModel>();
        CompletedBooks = new ObservableCollection<Book>();
        RegisterCommand = new AsyncRelayCommand(RegisterAsync, CanRegister);
        ClearCompletedDateFilterCommand = new RelayCommand(ClearCompletedDateFilter);
    }

    public ObservableCollection<BookReadingRowViewModel> ReadingRows { get; }

    public ObservableCollection<Book> CompletedBooks { get; }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string Author
    {
        get => _author;
        set => SetProperty(ref _author, value);
    }

    public string TotalPages
    {
        get => _totalPages;
        set => SetProperty(ref _totalPages, value);
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
        _allReading = (await _bookService.GetReadingAsync()).ToList();
        _allCompleted = (await _bookService.GetCompletedAsync()).ToList();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        ReadingRows.Clear();
        foreach (var book in _allReading)
            ReadingRows.Add(new BookReadingRowViewModel(book, ApplyPagesAsync));

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

    private bool CanRegister() =>
        !string.IsNullOrWhiteSpace(Title) &&
        !string.IsNullOrWhiteSpace(Author) &&
        int.TryParse(TotalPages, out var pages) && pages > 0;

    private async Task RegisterAsync()
    {
        if (!CanRegister())
            return;

        var pages = int.Parse(TotalPages);
        await RunBusyAsync(async () =>
        {
            var book = await _bookService.RegisterAsync(Title, Author, pages);
            _allReading.Insert(0, book);
            ApplyFilter();

            Title = string.Empty;
            Author = string.Empty;
            TotalPages = "300";
            StatusMessage = $"Libro '{book.Title}' agregado.";
        }, "Registrando libro...");
    }

    private async Task ApplyPagesAsync(Book book, int targetPagesRead)
    {
        await RunBusyAsync(async () =>
        {
            var result = await _bookService.UpdatePagesReadAsync(book.Id, targetPagesRead);
            PublishAchievements(result.Events);
            await ReloadAsync();
            _profileRefreshMessenger.RequestRefresh();
            StatusMessage = $"{result.Value.Title}: {result.Value.PagesRead}/{result.Value.TotalPages} páginas";
        }, "Actualizando lectura...");
    }
}
