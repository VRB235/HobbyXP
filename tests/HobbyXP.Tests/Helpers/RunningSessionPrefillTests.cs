using HobbyXP.Helpers;
using HobbyXP.Models.Enums;
using HobbyXP.Models.Physical;

namespace HobbyXP.Tests.Helpers;

public sealed class RunningSessionPrefillTests
{
    [Fact]
    public void FindLatest_PrefersNewerDateThenHigherId()
    {
        var older = CreateSession(1, RunningSessionType.Umbral, new DateTime(2026, 9, 1), 8);
        var sameDayFirst = CreateSession(2, RunningSessionType.Umbral, new DateTime(2026, 9, 10), 6);
        var sameDayLatest = CreateSession(3, RunningSessionType.Umbral, new DateTime(2026, 9, 10), 5);
        var otherType = CreateSession(4, RunningSessionType.TiradaLarga, new DateTime(2026, 9, 12), 20);

        var latest = RunningSessionPrefill.FindLatest(
            [older, sameDayLatest, sameDayFirst, otherType],
            RunningSessionType.Umbral);

        Assert.Same(sameDayLatest, latest);
    }

    [Fact]
    public void FindLatest_ReturnsNullWhenTypeHasNoMatch()
    {
        var regen = CreateSession(1, RunningSessionType.Regenerativa, DateTime.UtcNow, 5);

        var latest = RunningSessionPrefill.FindLatest([regen], RunningSessionType.Umbral);

        Assert.Null(latest);
    }

    [Fact]
    public void SplitDuration_UsesTotalMinutesAndSeconds()
    {
        var (minutes, seconds) = RunningSessionPrefill.SplitDuration(new TimeSpan(1, 5, 10));

        Assert.Equal(65, minutes);
        Assert.Equal(10, seconds);
    }

    [Fact]
    public void FormatDistanceKm_RoundTripsThroughFormValidation()
    {
        var text = RunningSessionPrefill.FormatDistanceKm(5.5m);
        var result = FormValidation.RequirePositiveDecimal(text, "distancia", out var parsed);

        Assert.True(result.IsValid);
        Assert.Equal(5.5m, parsed);
    }

    [Fact]
    public void FormatSeriesDistanceInput_UsesMetersBelowOneKilometer()
    {
        var (text, isMeters) = RunningSessionPrefill.FormatSeriesDistanceInput(0.8m);

        Assert.True(isMeters);
        Assert.Equal("800", text);
        Assert.Equal("800 m", RunningSessionPrefill.FormatSeriesDistanceLabel(0.8m));
    }

    [Fact]
    public void FormatSeriesDistanceInput_UsesKilometersFromOneKm()
    {
        var (text, isMeters) = RunningSessionPrefill.FormatSeriesDistanceInput(1m);

        Assert.False(isMeters);
        var parsed = FormValidation.RequirePositiveDecimal(text, "distancia", out var km);
        Assert.True(parsed.IsValid);
        Assert.Equal(1m, km);
    }

    [Fact]
    public void TryFormatPaceMinPerKm_ComputesMinutesPerKilometer()
    {
        var pace = RunningSessionPrefill.TryFormatPaceMinPerKm(10m, TimeSpan.FromMinutes(50));

        Assert.Equal($"{5d:0.00} min/km", pace);
    }

    [Fact]
    public void BuildHint_IncludesDistanceDurationPaceAndSeries()
    {
        var session = CreateSession(8, RunningSessionType.Umbral, new DateTime(2026, 9, 10), 8);
        session.Duration = TimeSpan.FromMinutes(40);
        session.PaceMinPerKm = 5;
        session.Series.Add(new RunningSessionSeries
        {
            SortOrder = 1,
            DistanceKm = 1m,
            Duration = TimeSpan.FromMinutes(4)
        });
        session.Series.Add(new RunningSessionSeries
        {
            SortOrder = 2,
            DistanceKm = 1m,
            Duration = TimeSpan.FromMinutes(4)
        });

        var hint = RunningSessionPrefill.BuildHint(session);

        Assert.Contains("Última Umbral", hint, StringComparison.Ordinal);
        Assert.Contains($"{8m:0.##} km", hint, StringComparison.Ordinal);
        Assert.Contains("40:00", hint, StringComparison.Ordinal);
        Assert.Contains($"{5d:0.00} min/km", hint, StringComparison.Ordinal);
        Assert.Contains("Series:", hint, StringComparison.Ordinal);
        Assert.Contains("Puede editarlos antes de guardar.", hint, StringComparison.Ordinal);
    }

    private static RunningSession CreateSession(
        int id,
        RunningSessionType type,
        DateTime recordedAt,
        decimal distanceKm) =>
        new()
        {
            Id = id,
            SessionType = type,
            RecordedAt = recordedAt,
            DistanceKm = distanceKm,
            Duration = TimeSpan.FromMinutes(30),
            PaceMinPerKm = 5
        };
}
