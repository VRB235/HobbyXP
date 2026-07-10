using HobbyXP.Helpers;
using HobbyXP.Models.PersonalGrowth;
using HobbyXP.ViewModels.Common;

namespace HobbyXP.ViewModels.PersonalGrowth;

public sealed class BookReadingRowViewModel : ViewModelBase
{
    private readonly Func<Book, int, Task> _applyAsync;
    private int _targetPagesRead;
    private string? _validationMessage;

    public BookReadingRowViewModel(Book book, Func<Book, int, Task> applyAsync)
    {
        Book = book;
        _applyAsync = applyAsync;
        _targetPagesRead = book.PagesRead;

        ApplyPagesCommand = new AsyncRelayCommand(ApplyPagesAsync, CanApplyPages);
        BumpPagesCommand = new RelayCommand(BumpPages);
        SetCompleteCommand = new RelayCommand(SetComplete);
        RefreshValidation();
    }

    public Book Book { get; }

    public string Title => Book.Title;

    public string Author => Book.Author;

    public int TotalPages => Book.TotalPages;

    public int XpEarned => Book.XpEarned;

    public string XpEarnedDisplay => $"XP: {XpEarned}";

    public int CurrentPagesRead => Book.PagesRead;

    public string? ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    /// <summary>Progreso actual (0–100) para barra y slider con máximo literal.</summary>
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

    public string ProgressSummary => HasPendingChange
        ? $"Actual: {CurrentPagesRead}/{TotalPages} → Nuevo: {TargetPagesRead}/{TotalPages}"
        : $"Progreso actual: {CurrentPagesRead}/{TotalPages} páginas";

    public AsyncRelayCommand ApplyPagesCommand { get; }

    public RelayCommand BumpPagesCommand { get; }

    public RelayCommand SetCompleteCommand { get; }

    private double ToPercent(int pages) =>
        TotalPages > 0 ? (double)pages / TotalPages * 100d : 0d;

    private ValidationResult ValidateForm()
    {
        if (TotalPages <= 0)
            return ValidationResult.Fail("El libro no tiene páginas totales definidas.");

        if (TargetPagesRead < CurrentPagesRead)
            return ValidationResult.Fail("Las páginas leídas no pueden ser menores al progreso actual.");

        return FormValidation.RequireNotAbove(
            TargetPagesRead,
            TotalPages,
            "Las páginas leídas",
            "páginas");
    }

    private void RefreshValidation()
    {
        var result = ValidateForm();
        ValidationMessage = result.IsValid ? null : result.Message;
        ApplyPagesCommand.RaiseCanExecuteChanged();
    }

    private bool CanApplyPages() => HasPendingChange && ValidateForm().IsValid;

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
        if (!ValidateForm().IsValid)
        {
            RefreshValidation();
            return;
        }

        if (!HasPendingChange)
            return;

        await _applyAsync(Book, TargetPagesRead);
        ValidationMessage = null;
    }
}
