using System.Collections.ObjectModel;
using HobbyXP.Services.Abstractions;
using HobbyXP.ViewModels.Common;

namespace HobbyXP.ViewModels.XpHistory;

public sealed class XpHistoryViewModel : ViewModelBase
{
    private readonly IXpService _xpService;
    private bool _isBusy;
    private bool _creditsOnly = true;
    private string? _statusMessage;
    private string _searchText = string.Empty;
    private XpModuleFilterOption _moduleFilterOption;
    private IReadOnlyList<XpHistoryRowViewModel> _allRows = [];

    public XpHistoryViewModel(IXpService xpService)
    {
        _xpService = xpService;
        Entries = new ObservableCollection<XpHistoryRowViewModel>();
        ModuleFilterOptions = XpModuleFilterOption.CreateDefault();
        _moduleFilterOption = ModuleFilterOptions[0];
        RefreshCommand = new AsyncRelayCommand(LoadAsync, () => !IsBusy);
        CloseCommand = new RelayCommand(() => RequestClose?.Invoke());
    }

    public event Action? RequestClose;

    public ObservableCollection<XpHistoryRowViewModel> Entries { get; }

    public IReadOnlyList<XpModuleFilterOption> ModuleFilterOptions { get; }

    public AsyncRelayCommand RefreshCommand { get; }

    public RelayCommand CloseCommand { get; }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
                RefreshCommand.RaiseCanExecuteChanged();
        }
    }

    public bool CreditsOnly
    {
        get => _creditsOnly;
        set
        {
            if (!SetProperty(ref _creditsOnly, value))
                return;

            _ = LoadAsync();
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetProperty(ref _searchText, value))
                return;

            ApplyFilter();
        }
    }

    public XpModuleFilterOption ModuleFilterOption
    {
        get => _moduleFilterOption;
        set
        {
            if (value is null || !SetProperty(ref _moduleFilterOption, value))
                return;

            ApplyFilter();
        }
    }

    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public async Task LoadAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        StatusMessage = "Cargando historial…";
        try
        {
            var ledger = await _xpService.GetLedgerEntriesAsync(take: 400, creditsOnly: CreditsOnly);
            _allRows = ledger.Select(e => new XpHistoryRowViewModel(e)).ToList();
            ApplyFilter();
            if (Entries.Count == 0 && !HasActiveClientFilters())
            {
                StatusMessage = CreditsOnly
                    ? "Aún no hay XP ganados registrados."
                    : "No hay movimientos de XP.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"No se pudo cargar el historial: {ex.Message}";
            _allRows = [];
            Entries.Clear();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyFilter()
    {
        Entries.Clear();
        var term = SearchText.Trim();
        var query = _allRows.Where(ModuleFilterOption.Matches);
        if (!string.IsNullOrEmpty(term))
        {
            query = query.Where(r =>
                r.Description.Contains(term, StringComparison.OrdinalIgnoreCase)
                || r.ActionLabel.Contains(term, StringComparison.OrdinalIgnoreCase)
                || r.HobbyLabel.Contains(term, StringComparison.OrdinalIgnoreCase)
                || r.AmountLabel.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var row in query)
            Entries.Add(row);

        if (_allRows.Count > 0)
        {
            StatusMessage = HasActiveClientFilters()
                ? $"{Entries.Count} de {_allRows.Count} movimiento(s)"
                : $"{Entries.Count} movimiento(s)";
        }
    }

    private bool HasActiveClientFilters() =>
        !ModuleFilterOption.IsAll || !string.IsNullOrWhiteSpace(SearchText);
}
