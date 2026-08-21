using HobbyXP.Helpers;
using HobbyXP.Models.Physical;
using HobbyXP.ViewModels.Common;

namespace HobbyXP.ViewModels.Physical;

public sealed class RunningSeriesDistanceUnitOption
{
    public static IReadOnlyList<RunningSeriesDistanceUnitOption> All { get; } =
    [
        new("m", isMeters: true),
        new("km", isMeters: false)
    ];

    private RunningSeriesDistanceUnitOption(string label, bool isMeters)
    {
        Label = label;
        IsMeters = isMeters;
    }

    public string Label { get; }

    public bool IsMeters { get; }

    public override string ToString() => Label;
}

/// <summary>
/// Fila editable de una serie de umbral en el formulario de nueva sesión.
/// </summary>
public sealed class RunningSeriesRowViewModel : ViewModelBase
{
    private string _distance = string.Empty;
    private RunningSeriesDistanceUnitOption _distanceUnit = RunningSeriesDistanceUnitOption.All[0];
    private string _durationMinutes = string.Empty;
    private string _durationSeconds = string.Empty;

    public RunningSeriesRowViewModel(int sortOrder, Action? onChanged = null)
    {
        SortOrder = sortOrder;
        OnChanged = onChanged;
    }

    public int SortOrder { get; }

    public string SeriesLabel => $"Serie {SortOrder}";

    public Action? OnChanged { get; set; }

    public IReadOnlyList<RunningSeriesDistanceUnitOption> DistanceUnits => RunningSeriesDistanceUnitOption.All;

    public string Distance
    {
        get => _distance;
        set
        {
            if (SetProperty(ref _distance, value))
                OnChanged?.Invoke();
        }
    }

    public RunningSeriesDistanceUnitOption DistanceUnit
    {
        get => _distanceUnit;
        set
        {
            if (SetProperty(ref _distanceUnit, value))
                OnChanged?.Invoke();
        }
    }

    public string DurationMinutes
    {
        get => _durationMinutes;
        set
        {
            if (SetProperty(ref _durationMinutes, value))
                OnChanged?.Invoke();
        }
    }

    public string DurationSeconds
    {
        get => _durationSeconds;
        set
        {
            if (SetProperty(ref _durationSeconds, value))
                OnChanged?.Invoke();
        }
    }

    public bool TryBuildDraft(out RunningSeriesDraft? draft, out string? error)
    {
        draft = null;
        error = null;

        var distanceResult = FormValidation.RequirePositiveDecimal(Distance, $"Serie {SortOrder}: la distancia", out var raw);
        if (!distanceResult.IsValid)
        {
            error = distanceResult.Message;
            return false;
        }

        var distanceKm = DistanceUnit.IsMeters ? raw / 1000m : raw;
        if (distanceKm <= 0)
        {
            error = $"Serie {SortOrder}: indique una distancia mayor que cero.";
            return false;
        }

        var minutesResult = FormValidation.RequireNonNegativeInt(DurationMinutes, $"Serie {SortOrder}: los minutos", out var min);
        if (!minutesResult.IsValid)
        {
            error = minutesResult.Message;
            return false;
        }

        var secondsResult = FormValidation.RequireIntInRange(DurationSeconds, $"Serie {SortOrder}: los segundos", 0, 59, out var sec);
        if (!secondsResult.IsValid)
        {
            error = secondsResult.Message;
            return false;
        }

        var duration = new TimeSpan(0, min, sec);
        if (duration <= TimeSpan.Zero)
        {
            error = $"Serie {SortOrder}: indique un tiempo mayor que cero.";
            return false;
        }

        draft = new RunningSeriesDraft(SortOrder, distanceKm, duration);
        return true;
    }

    public void Clear()
    {
        Distance = string.Empty;
        DurationMinutes = string.Empty;
        DurationSeconds = string.Empty;
        DistanceUnit = RunningSeriesDistanceUnitOption.All[0];
    }
}
