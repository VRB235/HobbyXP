using System.Globalization;
using System.Windows.Input;
using HobbyXP.Helpers;
using HobbyXP.Models.Enums;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Results;
using HobbyXP.ViewModels.Common;

namespace HobbyXP.ViewModels.Achievements;

public sealed class RewardDetailViewModel : ViewModelBase
{
    private readonly IRewardService _rewardService;
    private readonly IFileDialogService _fileDialogService;
    private readonly IImagePreviewService _imagePreviewService;
    private readonly RewardRowViewModel? _original;
    private readonly IReadOnlyDictionary<MilestoneSourceType, int> _moduleBalances;
    private readonly int _currentLevel;
    private readonly int? _equippedRewardId;

    private string _name = string.Empty;
    private string _costInPoints = "500";
    private string? _description;
    private string _priceText = string.Empty;
    private string? _purchaseUrl;
    private string? _pendingImageSourcePath;
    private string? _previewImagePath;
    private bool _clearImageOnSave;
    private HobbyModuleOption? _selectedModule;
    private bool _isEquipped;
    private string? _validationMessage;
    private string? _errorMessage;
    private bool _isBusy;

    public RewardDetailViewModel(
        RewardRowViewModel? reward,
        IRewardService rewardService,
        IFileDialogService fileDialogService,
        IImagePreviewService imagePreviewService,
        int currentLevel,
        IReadOnlyDictionary<MilestoneSourceType, int> moduleBalances,
        int? equippedRewardId)
    {
        _original = reward;
        _rewardService = rewardService;
        _fileDialogService = fileDialogService;
        _imagePreviewService = imagePreviewService;
        _currentLevel = currentLevel;
        _moduleBalances = moduleBalances;
        _equippedRewardId = equippedRewardId;
        _selectedModule = ModuleOptions[0];
        _isEquipped = reward?.IsEquipped ?? false;

        SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsBusy && CanSave());
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => !IsBusy && CanDelete());
        RedeemCommand = new AsyncRelayCommand(RedeemAsync, () => !IsBusy && CanRedeem());
        EquipCommand = new AsyncRelayCommand(EquipAsync, () => !IsBusy && CanEquip());
        UnequipCommand = new AsyncRelayCommand(UnequipAsync, () => !IsBusy && CanUnequip());
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(false), () => !IsBusy);
        PickImageCommand = new RelayCommand(PickImage, () => !IsBusy);
        ClearImageCommand = new RelayCommand(ClearImage, () => !IsBusy && CanClearImage());
        OpenPhotoCommand = new RelayCommand(OpenPhoto, () => !IsBusy && HasPreviewImage);
        OpenPurchaseUrlCommand = new RelayCommand(OpenPurchaseUrl, CanOpenPurchaseUrl);

        if (reward is not null)
        {
            _name = reward.Name;
            _costInPoints = reward.BaseCost.ToString(CultureInfo.InvariantCulture);
            _description = reward.Description;
            _priceText = reward.Price?.ToString("0.##", CultureInfo.CurrentCulture) ?? string.Empty;
            _purchaseUrl = reward.PurchaseUrl;
            _previewImagePath = reward.ImageDisplayPath;
            SyncModuleFromReward(reward);
        }

        RefreshValidation();
    }

    public event Action<bool>? RequestClose;

    public bool IsCreateMode => _original is null;

    public bool IsAvailable => _original?.IsAvailable ?? true;

    public bool IsRedeemed => _original?.IsRedeemed ?? false;

    public string WindowTitle => IsCreateMode ? "Nuevo premio" : _original!.Name;

    public string SaveButtonLabel => IsCreateMode ? "Crear" : "Guardar";

    public IReadOnlyList<HobbyModuleOption> ModuleOptions => HobbyModuleOption.Catalog;

    public HobbyModuleOption? SelectedModule
    {
        get => _selectedModule;
        set
        {
            if (SetProperty(ref _selectedModule, value))
            {
                RefreshValidation();
                OnPropertyChanged(nameof(RedeemHint));
                OnPropertyChanged(nameof(CanAfford));
                RedeemCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
                RefreshValidation();
        }
    }

    public string CostInPoints
    {
        get => _costInPoints;
        set
        {
            if (SetProperty(ref _costInPoints, value))
                RefreshValidation();
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
                RefreshValidation();
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

    public bool IsEquipped
    {
        get => _isEquipped;
        private set
        {
            if (SetProperty(ref _isEquipped, value))
            {
                EquipCommand.RaiseCanExecuteChanged();
                UnequipCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string? ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value))
                return;

            SaveCommand.RaiseCanExecuteChanged();
            DeleteCommand.RaiseCanExecuteChanged();
            RedeemCommand.RaiseCanExecuteChanged();
            EquipCommand.RaiseCanExecuteChanged();
            UnequipCommand.RaiseCanExecuteChanged();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public int EffectiveCost
    {
        get
        {
            if (!int.TryParse(CostInPoints, NumberStyles.Integer, CultureInfo.CurrentCulture, out var baseCost)
                && !int.TryParse(CostInPoints, NumberStyles.Integer, CultureInfo.InvariantCulture, out baseCost))
                return 0;

            return RewardCostCalculator.GetEffectiveCost(baseCost, _currentLevel);
        }
    }

    public bool CanAfford =>
        IsAvailable
        && SelectedModule is not null
        && GetModuleBalance(SelectedModule.Value) >= EffectiveCost;

    public string RedeemHint
    {
        get
        {
            if (!IsAvailable)
                return IsEquipped
                    ? "Esta reliquia está equipada en su perfil."
                    : "Premio canjeado. Puede equiparlo como reliquia.";

            if (SelectedModule is null)
                return "Seleccione un módulo antes de canjear.";

            var module = SelectedModule.Value;
            var balance = GetModuleBalance(module);
            if (CanAfford)
                return $"Puede canjearlo por {EffectiveCost:N0} XP de {HobbyProgressCatalog.GetDisplayName(module)}.";

            var missing = EffectiveCost - balance;
            return
                $"Necesita {EffectiveCost:N0} XP de {HobbyProgressCatalog.GetDisplayName(module)} (faltan {missing:N0}; saldo: {balance:N0}).";
        }
    }

    public AchievementEvent[] CompletionEvents { get; private set; } = [];

    public string? ResultMessage { get; private set; }

    public bool NeedsProfileRefresh { get; private set; }

    public bool WasActionCompleted { get; private set; }

    public AsyncRelayCommand SaveCommand { get; }

    public AsyncRelayCommand DeleteCommand { get; }

    public AsyncRelayCommand RedeemCommand { get; }

    public AsyncRelayCommand EquipCommand { get; }

    public AsyncRelayCommand UnequipCommand { get; }

    public RelayCommand CancelCommand { get; }

    public RelayCommand PickImageCommand { get; }

    public RelayCommand ClearImageCommand { get; }

    public RelayCommand OpenPhotoCommand { get; }

    public RelayCommand OpenPurchaseUrlCommand { get; }

    private bool CanSave() => ValidateForm().IsValid;

    private bool CanDelete() => !IsCreateMode && IsAvailable;

    private bool CanRedeem() => !IsCreateMode && IsAvailable && CanAfford;

    private bool CanEquip() => IsRedeemed && !IsEquipped;

    private bool CanUnequip() => IsRedeemed && IsEquipped;

    private bool CanClearImage() =>
        !IsBusy && (HasPreviewImage || _pendingImageSourcePath is not null || _clearImageOnSave);

    private bool CanOpenPurchaseUrl() =>
        !IsBusy && !string.IsNullOrWhiteSpace(PurchaseUrl);

    private ValidationResult ValidateForm()
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

    private void RefreshValidation()
    {
        var result = ValidateForm();
        ValidationMessage = result.IsValid ? null : result.Message;
        SaveCommand.RaiseCanExecuteChanged();
        RedeemCommand.RaiseCanExecuteChanged();
        OnPropertyChanged(nameof(RedeemHint));
        OnPropertyChanged(nameof(CanAfford));
        OnPropertyChanged(nameof(EffectiveCost));
    }

    private int GetModuleBalance(MilestoneSourceType module) =>
        _moduleBalances.GetValueOrDefault(module, 0);

    private void SyncModuleFromReward(RewardRowViewModel row)
    {
        if (row.SourceType is not { } source)
            return;

        var match = ModuleOptions.FirstOrDefault(option => option.Value == source);
        if (match is not null)
            SelectedModule = match;
    }

    private decimal? ParseOptionalPrice()
    {
        if (string.IsNullOrWhiteSpace(PriceText))
            return null;

        FormValidation.RequireNonNegativeDecimal(PriceText, "El precio", out var parsed);
        return parsed;
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

        ErrorMessage = null;
        _pendingImageSourcePath = persisted;
        _clearImageOnSave = false;
        PreviewImagePath = persisted;
        CommandManager.InvalidateRequerySuggested();
    }

    private void ClearImage()
    {
        DiscardPendingStagingImage();
        _pendingImageSourcePath = null;
        _clearImageOnSave = !IsCreateMode;
        PreviewImagePath = null;
        CommandManager.InvalidateRequerySuggested();
    }

    private void OpenPhoto()
    {
        if (PreviewImagePath is null)
            return;

        _imagePreviewService.Show(PreviewImagePath, Name);
    }

    private void OpenPurchaseUrl()
    {
        if (string.IsNullOrWhiteSpace(PurchaseUrl))
            return;

        var normalized = PurchaseUrl.Trim();
        if (!normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "https://" + normalized;
        }

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
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

    private void DiscardPendingStagingImage()
    {
        if (_pendingImageSourcePath is null)
            return;

        RewardPhotoStorage.DeleteStagingFile(_pendingImageSourcePath);
        _pendingImageSourcePath = null;
    }

    private async Task SaveAsync()
    {
        if (!ValidateForm().IsValid || SelectedModule is null)
        {
            RefreshValidation();
            return;
        }

        if (!int.TryParse(CostInPoints, NumberStyles.Integer, CultureInfo.CurrentCulture, out var cost)
            && !int.TryParse(CostInPoints, NumberStyles.Integer, CultureInfo.InvariantCulture, out cost))
        {
            RefreshValidation();
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var module = SelectedModule.Value;
            var price = ParseOptionalPrice();

            if (IsCreateMode)
            {
                var created = await _rewardService.CreateAsync(
                    Name,
                    cost,
                    module,
                    Description,
                    price,
                    PurchaseUrl,
                    _pendingImageSourcePath);
                _pendingImageSourcePath = null;
                ResultMessage = $"Premio creado en {HobbyProgressCatalog.GetDisplayName(module)}: {created.Name}.";
            }
            else
            {
                await _rewardService.UpdateAsync(
                    _original!.Id,
                    Name,
                    cost,
                    module,
                    Description,
                    price,
                    PurchaseUrl,
                    _pendingImageSourcePath,
                    _clearImageOnSave);
                _pendingImageSourcePath = null;
                ResultMessage = "Premio actualizado.";
            }

            WasActionCompleted = true;
            RequestClose?.Invoke(true);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DeleteAsync()
    {
        if (_original is null || !IsAvailable)
            return;

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            await _rewardService.DeleteAsync(_original.Id);
            ResultMessage = "Premio eliminado.";
            WasActionCompleted = true;
            RequestClose?.Invoke(true);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RedeemAsync()
    {
        if (_original is null || !CanRedeem())
            return;

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var result = await _rewardService.RedeemAsync(_original.Id);
            CompletionEvents = result.Events.ToArray();
            ResultMessage = $"Premio canjeado: {result.Value.Name}. Ya está en el inventario.";
            NeedsProfileRefresh = true;
            WasActionCompleted = true;
            RequestClose?.Invoke(true);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task EquipAsync()
    {
        if (_original is null || !CanEquip())
            return;

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            await _rewardService.EquipAsync(_original.Id);
            IsEquipped = true;
            ResultMessage = "Reliquia equipada. Se muestra en el perfil.";
            NeedsProfileRefresh = true;
            WasActionCompleted = true;
            RequestClose?.Invoke(true);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task UnequipAsync()
    {
        if (!CanUnequip())
            return;

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            await _rewardService.UnequipAsync();
            IsEquipped = false;
            ResultMessage = "Reliquia desequipada.";
            NeedsProfileRefresh = true;
            WasActionCompleted = true;
            RequestClose?.Invoke(true);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void OnClosedWithoutSave()
    {
        if (!WasActionCompleted)
            DiscardPendingStagingImage();
    }
}
