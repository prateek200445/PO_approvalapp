using System.Text.Json;

namespace POApprovalAPI.Models;

public static class ChatExportKinds
{
    public const string ErpAgeing = "erp_ageing";
    public const string ErpFinance = "erp_finance";
    public const string ErpInventory = "erp_inventory";
    public const string ErpLedgerStatement = "erp_ledger_statement";
}

/// <summary>
/// Serializable handle so /api/chat/export can re-run ERP EXEC reports at export row cap.
/// </summary>
public class ChatExportContext
{
    public string Kind { get; set; } = "";
    public JsonElement Plan { get; set; }
}
