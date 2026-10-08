using HobbyXP.Models.Enums;
using HobbyXP.Services.Results;

namespace HobbyXP.ViewModels.XpHistory;

public sealed class XpHistoryRowViewModel
{
    public XpHistoryRowViewModel(XpLedgerEntry entry)
    {
        Amount = entry.Amount;
        Description = entry.Description;
        ActionLabel = entry.ActionLabel;
        HobbyLabel = entry.HobbyLabel;
        SourceType = entry.SourceType;
        IsGlobal = entry.IsGlobal;
        EarnedAtLocal = entry.EarnedAtUtc.Kind switch
        {
            DateTimeKind.Utc => entry.EarnedAtUtc.ToLocalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(entry.EarnedAtUtc, DateTimeKind.Utc).ToLocalTime(),
            _ => entry.EarnedAtUtc
        };
    }

    public int Amount { get; }

    public string AmountLabel => Amount > 0 ? $"+{Amount:N0}" : Amount.ToString("N0");

    public bool IsCredit => Amount > 0;

    public bool IsDebit => Amount < 0;

    public string Description { get; }

    public string ActionLabel { get; }

    public string HobbyLabel { get; }

    public MilestoneSourceType? SourceType { get; }

    public bool IsGlobal { get; }

    public DateTime EarnedAtLocal { get; }

    public string EarnedAtLabel => EarnedAtLocal.ToString("dd/MM/yyyy HH:mm");
}
