using HobbyXP.Models.Enums;

namespace HobbyXP.Services.Results;

public sealed record HobbyDistributionSlice(
    MilestoneSourceType Category,
    string Label,
    int Count,
    double Percentage);
