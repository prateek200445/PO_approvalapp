namespace POApprovalAPI.Models;

public class ExportCurrencyAuditResultDto
{
    public string CompanyLabel { get; set; } = "All companies";
    public string DateFrom { get; set; } = "";
    public string DateTo { get; set; } = "";
    public decimal MinInrAmount { get; set; }
    public int TotalCount { get; set; }
    public int CreditNoteCount { get; set; }
    public int DebitNoteCount { get; set; }
    public decimal TotalInrAmount { get; set; }
    public IReadOnlyList<ExportCurrencyAuditItemDto> Items { get; set; } = Array.Empty<ExportCurrencyAuditItemDto>();
}

public class ExportCurrencyAuditItemDto
{
    public string DocumentType { get; set; } = "";
    public string DocumentNo { get; set; } = "";
    public string DocumentDate { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string PartyName { get; set; } = "";
    public string LedgerName { get; set; } = "";
    public decimal InrAmount { get; set; }
    public string StoredFc { get; set; } = "";
    public string Currency { get; set; } = "";
    public decimal ExchangeRate { get; set; }
    public decimal CalculatedFc { get; set; }
    public string Issue { get; set; } = "";
}
