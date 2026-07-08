namespace HobbyXP.Services.Results;

public sealed record MediaYearlyCounters(
    int Year,
    int MoviesCount,
    int SeriesCount,
    int TotalCount);
