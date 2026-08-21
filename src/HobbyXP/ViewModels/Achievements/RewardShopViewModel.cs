using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Input;
using HobbyXP.Helpers;
using HobbyXP.Models.Enums;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Messaging;
using HobbyXP.ViewModels.Common;
using HobbyXP.ViewModels.Messaging;

namespace HobbyXP.ViewModels.Achievements;

public sealed class RewardShopViewModel : AchievementAwareViewModel
{
    private readonly IRewardService _rewardService;
    private readonly IPlayerProfileService _playerProfileService;
    private readonly IXpService _xpService;
    private readonly IFileDialogService _fileDialogService;
    private readonly IImagePreviewService _imagePreviewService;
    private readonly IProfileRefreshMessenger _profileRefreshMessenger;
    private readonly Dictionary<MilestoneSourceType, int> _moduleBalances = new();
    private string _name = string.Empty;
    private string _costInPoints = "500";
    private string? _description;
    private string _priceText = string.Empty;
    private string? _purchaseUrl;
    private string? _pendingImageSourcePath;
    private string? _previewImagePath;
    private bool _clearImageOnSave;
    private HobbyModuleOption? _selectedModule;
    private int _availableXp;
    private int _currentLevel = 1;
    private int? _equippedRewardId;
    private RewardRowViewModel? _selectedAvailable;
    private RewardRowViewModel? _selectedInventory;
    private bool _isEditing;

    public RewardShopViewModel(
        IRewardService rewardService,
        IPlayerProfileService playerProfileService,
        IXpService xpService,
        IFileDialogService fileDialogService,
        IImagePreviewService imagePreviewService,
        IProfileRefreshMessenger profileRefreshMessenger,
        IAchievementMessenger achievementMessenger)
        : base(achievementMessenger)
    {
        _rewardService = rewardService;
        _playerProfileService = playerProfileService;
        _xpService = xpService;
        _fileDialogService = fileDialogService;
        _imagePreviewService = imagePreviewService;
        _profileRefreshMessenger = profileRefreshMessenger;
        AvailableSections = new ObservableCollection<RewardShopSectionViewModel>();
        InventorySections = new ObservableCollection<RewardShopSectionViewModel>();
        _selectedModule = ModuleOptions[0];

        CreateRewardCommand = new AsyncRelayCommand(CreateRewardAsync, CanCreateReward);
        SaveRewardCommand = new AsyncRelayCommand(SaveRewardAsync, CanSaveReward);
        DeleteRewardCommand = new AsyncRelayCommand(DeleteRewardAsync, CanDeleteReward);
        ClearFormCommand = new RelayCommand(ClearForm);
        PickImageCommand = new RelayCommand(PickImage);
        ClearImageCommand = new RelayCommand(ClearImage, CanClearImage);
        OpenPhotoCommand = new RelayCommand(OpenPhoto, CanOpenPhoto);
        OpenPurchaseUrlCommand = new RelayCommand(OpenPurchaseUrl, CanOpenPurchaseUrl);
        AssignModuleCommand = new AsyncRelayCommand(AssignModuleAsync, CanAssignModule);
        RedeemRewardCommand = new AsyncRelayCommand(RedeemRewardAsync, CanRedeemSelected);
        EquipRewardCommand = new AsyncRelayCommand(EquipRewardAsync, CanEquipSelected);
        UnequipRewardCommand = new AsyncRelayCommand(UnequipRewardAsync, CanUnequip);
        RefreshCreateValidation();
    }

    public IReadOnlyList<HobbyModuleOption> ModuleOptions => HobbyModuleOption.Catalog;

    public ObservableCollection<RewardShopSectionViewModel> AvailableSections { get; }

    public ObservableCollection<RewardShopSectionViewModel> InventorySections { get; }

    public HobbyModuleOption? SelectedModule
    {
        get => _selectedModule;
        set
        {
            if (SetProperty(ref _selectedModule, value))
            {
                RefreshCreateValidation();
                AssignModuleCommand.RaiseCanExecuteChanged();
                SaveRewardCommand.RaiseCanExecuteChanged();
                OnPropertyChanged(nameof(BalanceText));
                OnPropertyChanged(nameof(CanAffordSelected));
                OnPropertyChanged(nameof(SelectedRewardRedeemHint));
                RedeemRewardCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
                RefreshCreateValidation();
        }
    }

    public string CostInPoints
    {
        get => _costInPoints;
        set
        {
            if (SetProperty(ref _costInPoints, value))
                RefreshCreateValidation();
        }
    }

    public string? Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public string PriceText
    {
        get => _priceText;
        set
        {
            if (SetProperty(ref _priceText, value))
                RefreshCreateValidation();
        }
    }

    public string? PurchaseUrl
    {
        get => _purchaseUrl;
        set
        {
            if (SetProperty(ref _purchaseUrl, value))
                CommandManager.InvalidateRequerySuggested();
        }
    }

    public string? PreviewImagePath
    {
        get => _previewImagePath;
        private set
        {
            if (SetProperty(ref _previewImagePath, value))
            {
                OnPropertyChanged(nameof(HasPreviewImage));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool HasPreviewImage => !string.IsNullOrWhiteSpace(PreviewImagePath);

    public bool IsEditing
    {
        get => _isEditing;
        private set
        {
            if (SetProperty(ref _isEditing, value))
            {
                OnPropertyChanged(nameof(FormTitle));
                OnPropertyChanged(nameof(PrimaryActionLabel));
                CreateRewardCommand.RaiseCanExecuteChanged();
                SaveRewardCommand.RaiseCanExecuteChanged();
                DeleteRewardCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string FormTitle => IsEditing ? "Editar premio" : "Crear premio (costo base)";

    public string PrimaryActionLabel => IsEditing ? "Guardar cambios" : "Crear";

    public int AvailableXp
    {
        get => _availableXp;
        private set
        {
            if (SetProperty(ref _availableXp, value))
            {
                OnPropertyChanged(nameof(CanAffordSelected));
                OnPropertyChanged(nameof(BalanceText));
                RedeemRewardCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string BalanceText
    {
        get
        {
            var module = SelectedAvailable?.SourceType ?? SelectedModule?.Value;
            if (module is null)
                return $"Saldo total: {AvailableXp:N0} XP · nivel {_currentLevel} (el costo = base × nivel)";

            return
                $"Saldo {HobbyProgressCatalog.GetDisplayName(module.Value)}: {GetModuleBalance(module.Value):N0} XP · nivel {_currentLevel} (el costo = base × nivel)";
        }
    }

    public RewardRowViewModel? SelectedAvailable
    {
        get => _selectedAvailable;
        set
        {
            if (SetProperty(ref _selectedAvailable, value))
            {
                OnPropertyChanged(nameof(CanAffordSelected));
                OnPropertyChanged(nameof(SelectedRewardRedeemHint));
                OnPropertyChanged(nameof(BalanceText));
                RedeemRewardCommand.RaiseCanExecuteChanged();
                AssignModuleCommand.RaiseCanExecuteChanged();
                DeleteRewardCommand.RaiseCanExecuteChanged();
                CommandManager.InvalidateRequerySuggested();
                if (value is not null)
                {
                    SelectedInventory = null;
                    LoadFormFromReward(value);
                }
            }
        }
    }

    public RewardRowViewModel? SelectedInventory
    {
        get => _selectedInventory;
        set
        {
            if (SetProperty(ref _selectedInventory, value))
            {
                EquipRewardCommand.RaiseCanExecuteChanged();
                UnequipRewardCommand.RaiseCanExecuteChanged();
                AssignModuleCommand.RaiseCanExecuteChanged();
                DeleteRewardCommand.RaiseCanExecuteChanged();
                CommandManager.InvalidateRequerySuggested();
                if (value is not null)
                {
                    SelectedAvailable = null;
                    LoadFormFromReward(value);
                }
            }
        }
    }

    public bool CanAffordSelected =>
        SelectedAvailable is { IsAvailable: true, SourceType: { } module } selected &&
        GetModuleBalance(module) >= selected.EffectiveCost;

    public string SelectedRewardRedeemHint
    {
        get
        {
            if (SelectedAvailable is null)
                return "Seleccione un premio disponible para editarlo o canjearlo.";

            if (SelectedAvailable.SourceType is not { } module)
                return "Asigne un módulo al premio antes de canjearlo.";

            var moduleBalance = GetModuleBalance(module);
            if (CanAffordSelected)
                return $"Puede canjearlo por {SelectedAvailable.EffectiveCost:N0} XP de {HobbyProgressCatalog.GetDisplayName(module)}.";

            var missing = SelectedAvailable.EffectiveCost - moduleBalance;
            return
                $"Necesita {SelectedAvailable.EffectiveCost:N0} XP de {HobbyProgressCatalog.GetDisplayName(module)} (faltan {missing:N0}; saldo del módulo: {moduleBalance:N0}).";
        }
    }

    public AsyncRelayCommand CreateRewardCommand { get; }

    public AsyncRelayCommand SaveRewardCommand { get; }

    public AsyncRelayCommand DeleteRewardCommand { get; }

    public RelayCommand ClearFormCommand { get; }

    public RelayCommand PickImageCommand { get; }

    public RelayCommand ClearImageCommand { get; }

    public RelayCommand OpenPhotoCommand { get; }

    public RelayCommand OpenPurchaseUrlCommand { get; }

    public AsyncRelayCommand AssignModuleCommand { get; }

    public AsyncRelayCommand RedeemRewardCommand { get; }

    public AsyncRelayCommand EquipRewardCommand { get; }

    public AsyncRelayCommand UnequipRewardCommand { get; }

    protected override async Task LoadCoreAsync()
    {
        var profile = await _playerProfileService.GetProfileAsync();
        AvailableXp = profile.SpendableXp;
        _currentLevel = profile.CurrentLevel;
        _equippedRewardId = profile.EquippedRewardId;

        _moduleBalances.Clear();
        foreach (var hobby in await _xpService.GetAllHobbyProgressAsync())
        {
            var balance = await _xpService.GetHobbySpendableXpAsync(hobby.SourceType);
            _moduleBalances[hobby.SourceType] = balance;
        }

        OnPropertyChanged(nameof(BalanceText));

        var rewards = await _rewardService.GetAllAsync();
        var available = new List<RewardRowViewModel>();
        var inventory = new List<RewardRowViewModel>();
        foreach (var reward in rewards)
        {
            var row = new RewardRowViewModel(reward, _currentLevel, _equippedRewardId ?? 0);
            if (row.IsAvailable)
                available.Add(row);
            else
                inventory.Add(row);
        }

        ReplaceSections(AvailableSections, available);
        ReplaceSections(InventorySections, inventory);

        UnequipRewardCommand.RaiseCanExecuteChanged();
        AssignModuleCommand.RaiseCanExecuteChanged();
        DeleteRewardCommand.RaiseCanExecuteChanged();
        CommandManager.InvalidateRequerySuggested();
    }

    private ValidationResult ValidateWritableForm()
    {
        var basics = FormValidation.FirstFailure(
            FormValidation.RequireValue(SelectedModule, "el módulo del premio"),
            FormValidation.RequireText(Name, "el nombre del premio"),
            FormValidation.RequirePositiveInt(CostInPoints, "El costo en XP", out _));

        if (!basics.IsValid)
            return basics;

        if (string.IsNullOrWhiteSpace(PriceText))
            return ValidationResult.Ok();

        return FormValidation.RequireNonNegativeDecimal(PriceText, "El precio", out _);
    }

    private void RefreshCreateValidation()
    {
        RefreshValidation(ValidateWritableForm(), CreateRewardCommand);
        SaveRewardCommand.RaiseCanExecuteChanged();
    }

    private bool CanCreateReward() => !IsEditing && ValidateWritableForm().IsValid;

    private bool CanSaveReward() => IsEditing && ValidateWritableForm().IsValid && GetSelectedForEdit() is not null;

    private bool CanDeleteReward() => GetSelectedForEdit() is { IsAvailable: true };

    private bool CanRedeemSelected() => CanAffordSelected;

    private bool CanEquipSelected() => SelectedInventory is { IsRedeemed: true, IsEquipped: false };

    private bool CanUnequip() => _equippedRewardId is > 0;

    private bool CanAssignModule()
    {
        var selected = GetSelectedForEdit();
        return SelectedModule is not null
            && selected is not null
            && selected.SourceType != SelectedModule.Value;
    }

    private bool CanClearImage() => HasPreviewImage || _pendingImageSourcePath is not null || _clearImageOnSave;

    private bool CanOpenPhoto(object? parameter) =>
        ResolvePhotoPath(parameter) is not null;

    private bool CanOpenPurchaseUrl()
    {
        var url = PurchaseUrl ?? GetSelectedForEdit()?.PurchaseUrl;
        return !string.IsNullOrWhiteSpace(url);
    }

    private void OpenPhoto(object? parameter)
    {
        var path = ResolvePhotoPath(parameter);
        if (path is null)
            return;

        var title = parameter switch
        {
            RewardRowViewModel row => row.Name,
            _ => GetSelectedForEdit()?.Name ?? "Premio"
        };

        _imagePreviewService.Show(path, title);
    }

    private string? ResolvePhotoPath(object? parameter) =>
        parameter switch
        {
            string filePath when !string.IsNullOrWhiteSpace(filePath) => filePath,
            RewardRowViewModel { ImageDisplayPath: { } path } => path,
            _ => PreviewImagePath
        };

    private RewardRowViewModel? GetSelectedForEdit() => SelectedAvailable ?? SelectedInventory;

    private void LoadFormFromReward(RewardRowViewModel row)
    {
        DiscardPendingStagingImage();
        IsEditing = true;
        Name = row.Name;
        CostInPoints = row.BaseCost.ToString(CultureInfo.InvariantCulture);
        Description = row.Description;
        PriceText = row.Price?.ToString("0.##", CultureInfo.CurrentCulture) ?? string.Empty;
        PurchaseUrl = row.PurchaseUrl;
        _pendingImageSourcePath = null;
        _clearImageOnSave = false;
        PreviewImagePath = row.ImageDisplayPath;
        SyncModuleFromSelection(row);
        ClearValidation();
        RefreshCreateValidation();
        CommandManager.InvalidateRequerySuggested();
    }

    private void ClearForm()
    {
        DiscardPendingStagingImage();
        IsEditing = false;
        SelectedAvailable = null;
        SelectedInventory = null;
        Name = string.Empty;
        CostInPoints = "500";
        Description = null;
        PriceText = string.Empty;
        PurchaseUrl = null;
        _pendingImageSourcePath = null;
        _clearImageOnSave = false;
        PreviewImagePath = null;
        ClearValidation();
        RefreshCreateValidation();
        StatusMessage = null;
    }

    private void PickImage()
    {
        var path = _fileDialogService.PickImageFile();
        if (string.IsNullOrWhiteSpace(path))
            return;

        DiscardPendingStagingImage();

        var persisted = RewardPhotoStorage.ImportToStaging(path);
        if (persisted is null)
        {
            ErrorMessage = "No se pudo copiar la imagen al almacén de la aplicación.";
            return;
        }

        _pendingImageSourcePath = persisted;
        _clearImageOnSave = false;
        PreviewImagePath = persisted;
        CommandManager.InvalidateRequerySuggested();
    }

    private void ClearImage()
    {
        DiscardPendingStagingImage();
        _pendingImageSourcePath = null;
        _clearImageOnSave = IsEditing;
        PreviewImagePath = null;
        CommandManager.InvalidateRequerySuggested();
    }

    private void DiscardPendingStagingImage()
    {
        if (_pendingImageSourcePath is null)
            return;

        RewardPhotoStorage.DeleteStagingFile(_pendingImageSourcePath);
    }

    private void OpenPurchaseUrl()
    {
        var url = PurchaseUrl ?? GetSelectedForEdit()?.PurchaseUrl;
        if (string.IsNullOrWhiteSpace(url))
            return;

        var normalized = url.Trim();
        if (!normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "https://" + normalized;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = normalized,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = $"No se pudo abrir el enlace: {ex.Message}";
        }
    }

    private void SyncModuleFromSelection(RewardRowViewModel? row)
    {
        if (row?.SourceType is not { } source)
            return;

        var match = ModuleOptions.FirstOrDefault(option => option.Value == source);
        if (match is not null)
            SelectedModule = match;
    }

    private int GetModuleBalance(MilestoneSourceType module) =>
        _moduleBalances.GetValueOrDefault(module, 0);

    private static void ReplaceSections(
        ObservableCollection<RewardShopSectionViewModel> target,
        IReadOnlyList<RewardRowViewModel> rows)
    {
        target.Clear();
        foreach (var section in RewardShopCatalog.Group(rows, row => row.SourceType))
            target.Add(new RewardShopSectionViewModel(section));
    }

    private decimal? ParseOptionalPrice()
    {
        if (string.IsNullOrWhiteSpace(PriceText))
            return null;

        FormValidation.RequireNonNegativeDecimal(PriceText, "El precio", out var parsed);
        return parsed;
    }

    private async Task CreateRewardAsync()
    {
        if (IsEditing || !ValidateWritableForm().IsValid || SelectedModule is null)
        {
            RefreshCreateValidation();
            return;
        }

        if (!int.TryParse(CostInPoints, NumberStyles.Integer, CultureInfo.CurrentCulture, out var cost)
            && !int.TryParse(CostInPoints, NumberStyles.Integer, CultureInfo.InvariantCulture, out cost))
        {
            RefreshCreateValidation();
            return;
        }

        var module = SelectedModule.Value;
        var price = ParseOptionalPrice();
        var imagePath = _pendingImageSourcePath;
        await RunBusyAsync(async () =>
        {
            await _rewardService.CreateAsync(
                Name,
                cost,
                module,
                Description,
                price,
                PurchaseUrl,
                imagePath);
            ClearForm();
            await LoadCoreAsync();
            StatusMessage = $"Premio creado en {HobbyProgressCatalog.GetDisplayName(module)}.";
        }, "Creando premio...");
    }

    private async Task SaveRewardAsync()
    {
        var selected = GetSelectedForEdit();
        if (selected is null || !ValidateWritableForm().IsValid || SelectedModule is null)
        {
            RefreshCreateValidation();
            return;
        }

        if (!int.TryParse(CostInPoints, NumberStyles.Integer, CultureInfo.CurrentCulture, out var cost)
            && !int.TryParse(CostInPoints, NumberStyles.Integer, CultureInfo.InvariantCulture, out cost))
        {
            RefreshCreateValidation();
            return;
        }

        var rewardId = selected.Id;
        var module = SelectedModule.Value;
        var price = ParseOptionalPrice();
        var imagePath = _pendingImageSourcePath;
        var clearImage = _clearImageOnSave;
        await RunBusyAsync(async () =>
        {
            await _rewardService.UpdateAsync(
                rewardId,
                Name,
                cost,
                module,
                Description,
                price,
                PurchaseUrl,
                imagePath,
                clearImage);
            await LoadCoreAsync();
            StatusMessage = "Premio actualizado.";
            ClearForm();
        }, "Guardando premio...");
    }

    private async Task DeleteRewardAsync()
    {
        var selected = GetSelectedForEdit();
        if (selected is null || !selected.IsAvailable)
            return;

        var rewardId = selected.Id;
        await RunBusyAsync(async () =>
        {
            await _rewardService.DeleteAsync(rewardId);
            ClearForm();
            await LoadCoreAsync();
            StatusMessage = "Premio eliminado.";
        }, "Eliminando premio...");
    }

    private async Task AssignModuleAsync()
    {
        var selected = GetSelectedForEdit();
        if (selected is null || SelectedModule is null)
            return;

        var rewardId = selected.Id;
        var module = SelectedModule.Value;
        await RunBusyAsync(async () =>
        {
            await _rewardService.UpdateSourceTypeAsync(rewardId, module);
            await LoadCoreAsync();
            StatusMessage = $"Premio asignado a {HobbyProgressCatalog.GetDisplayName(module)}.";
        }, "Asignando módulo...");
    }

    private async Task RedeemRewardAsync()
    {
        if (SelectedAvailable is null || !CanAffordSelected)
            return;

        var rewardId = SelectedAvailable.Id;
        await RunBusyAsync(async () =>
        {
            var result = await _rewardService.RedeemAsync(rewardId);
            PublishAchievements(result.Events);
            ClearForm();
            await LoadCoreAsync();
            _profileRefreshMessenger.RequestRefresh();
            StatusMessage = $"Premio canjeado: {result.Value.Name}. Ya está en el inventario.";
        }, "Canjeando premio...");
    }

    private async Task EquipRewardAsync()
    {
        if (SelectedInventory is null)
            return;

        var rewardId = SelectedInventory.Id;
        await RunBusyAsync(async () =>
        {
            await _rewardService.EquipAsync(rewardId);
            await LoadCoreAsync();
            _profileRefreshMessenger.RequestRefresh();
            StatusMessage = "Reliquia equipada. Se muestra en el perfil.";
        }, "Equipando premio...");
    }

    private async Task UnequipRewardAsync()
    {
        await RunBusyAsync(async () =>
        {
            await _rewardService.UnequipAsync();
            await LoadCoreAsync();
            _profileRefreshMessenger.RequestRefresh();
            StatusMessage = "Reliquia desequipada.";
        }, "Quitando reliquia...");
    }
}
