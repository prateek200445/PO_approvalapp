namespace POApprovalAPI.Models;

public sealed class ChatEntityContext
{
    public string Message { get; init; } = "";
    public ResolvedCompany? Company { get; init; }
    public ResolvedLedgerParty? LedgerParty { get; init; }
    public ResolvedVendorFirm? VendorFirm { get; init; }
}

public sealed class ResolvedCompany
{
    public string Name { get; init; } = "";
    public int CompanyId { get; init; }
    public string Source { get; init; } = "";
}

public sealed class ResolvedLedgerParty
{
    public string LedgerName { get; init; } = "";
    public string CompanyName { get; init; } = "";
    public string Source { get; init; } = "";
}

public sealed class ResolvedVendorFirm
{
    public string FirmName { get; init; } = "";
    public string Source { get; init; } = "";
}
