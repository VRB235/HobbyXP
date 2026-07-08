using System.Collections.ObjectModel;
using HobbyXP.Models.PersonalGrowth;
using HobbyXP.Services.Abstractions;
using HobbyXP.ViewModels.Common;
using HobbyXP.ViewModels.Messaging;

namespace HobbyXP.ViewModels.PersonalGrowth;

public sealed class BooksViewModel : AchievementAwareViewModel
{
    private readonly IBookService _bookService;
    private string _title = string.Empty;
    private string _author = string.Empty;
    private string _totalPages = "300";

    public BooksViewModel(IBookService bookService, IAchievementMessenger achievementMessenger)
        : base(achievementMessenger)
    {
        _bookService = bookService;
        ReadingBooks = new ObservableCollection<Book>();
        CompletedBooks = new ObservableCollection<Book>();
        RegisterCommand = new AsyncRelayCommand(RegisterAsync, CanRegister);
        UpdatePagesCommand = new AsyncRelayCommand(UpdatePagesAsync);
    }

    public ObservableCollection<Book> ReadingBooks { get; }

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

    public AsyncRelayCommand RegisterCommand { get; }

    public AsyncRelayCommand UpdatePagesCommand { get; }

    protected override async Task LoadCoreAsync()
    {
        var reading = await _bookService.GetReadingAsync();
        var completed = await _bookService.GetCompletedAsync();

        ReadingBooks.Clear();
        foreach (var book in reading)
            ReadingBooks.Add(book);

        CompletedBooks.Clear();
        foreach (var book in completed)
            CompletedBooks.Add(book);
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
            ReadingBooks.Insert(0, book);

            Title = string.Empty;
            Author = string.Empty;
            TotalPages = "300";
            StatusMessage = $"Libro '{book.Title}' agregado.";
        }, "Registrando libro...");
    }

    private async Task UpdatePagesAsync(object? parameter)
    {
        if (parameter is not Book book)
            return;

        await RunBusyAsync(async () =>
        {
            var result = await _bookService.UpdatePagesReadAsync(book.Id, book.PagesRead);
            PublishAchievements(result.Events);
            await LoadAsync();
            StatusMessage = $"{result.Value.Title}: {result.Value.PagesRead}/{result.Value.TotalPages} páginas";
        }, "Actualizando lectura...");
    }
}
