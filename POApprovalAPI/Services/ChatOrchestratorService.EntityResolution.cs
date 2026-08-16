using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

public partial class ChatOrchestratorService
{
    private static readonly AsyncLocal<ChatEntityContext?> CurrentEntities = new();

    /// <summary>
    /// Company: DB-resolved entity context first, then alias/regex fallback.
    /// </summary>
    private static string? ResolveCompanyForChat(string message)
    {
        if (CurrentEntities.Value?.Company is { } c)
            return c.Name;

        return ChatEntityResolutionService.ResolveOutwardCompanyAlias(message)
               ?? CanonicalizeCompanyName(TryExtractCompanyName(message) ?? "");
    }

    /// <summary>
    /// Ledger/party name: DB-resolved first, then regex fallback.
    /// </summary>
    private static string? ResolveLedgerPartyForChat(string message)
    {
        if (CurrentEntities.Value?.LedgerParty is { } p
            && !ChatEntityResolutionService.IsGarbagePartyHint(p.LedgerName))
            return p.LedgerName;

        var extracted = TryExtractLedgerPartyName(message);
        if (!string.IsNullOrWhiteSpace(extracted)
            && !ChatEntityResolutionService.IsGarbagePartyHint(extracted))
            return extracted;

        return null;
    }

    /// <summary>
    /// Vendor firm: DB-resolved first, then regex/alias fallback.
    /// </summary>
    private static string? ResolveVendorFirmForChat(string message)
    {
        if (CurrentEntities.Value?.VendorFirm is { } v)
            return v.FirmName;

        foreach (var cand in new[]
                 {
                     ResolveVendorFirmAlias(message),
                     TryExtractFirmNameBeforeProfileFields(message),
                     TryExtractVendorFirmName(message),
                     TryExtractVendorFirmFromMessage(message)
                 })
        {
            if (!string.IsNullOrWhiteSpace(cand)) return cand.Trim();
        }
        return null;
    }
}
