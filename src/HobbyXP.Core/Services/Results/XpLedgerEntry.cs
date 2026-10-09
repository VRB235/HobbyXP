using HobbyXP.Models.Enums;

namespace HobbyXP.Services.Results;

/// <summary>
/// Entrada del libro mayor de XP para historial en UI.
/// </summary>
public sealed record XpLedgerEntry(
    int Id,
    int Amount,
    string Description,
    AchievementActionType ActionType,
    string ActionLabel,
    MilestoneSourceType? SourceType,
    string HobbyLabel,
    bool IsGlobal,
    DateTime EarnedAtUtc);
