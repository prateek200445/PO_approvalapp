namespace POApprovalAPI.Models;

public class LedgerCompanyOption
{
    public string Value { get; set; } = "";
    public string Label { get; set; } = "";
    public int CompanyType { get; set; }
    public string CompanyName { get; set; } = "";
    public int CompanyId { get; set; }
}

public class LedgerNameOption
{
    public string LedgerId { get; set; } = "";
    public string LedgerName { get; set; } = "";
}

public class LedgerSummaryQueryRequest
{
    public int CompanyType { get; set; } = 2;
    public string? CompanyName { get; set; }
    public int CompanyId { get; set; }
    public string LedgerName { get; set; } = "";
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public string? Currency { get; set; }
    public decimal InterestCal { get; set; }
}

public class LedgerSummaryBatchQueryRequest
{
    public List<LedgerSummaryCompanyRef> Companies { get; set; } = new();
    public List<string> LedgerNames { get; set; } = new();
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public string? Currency { get; set; }
    public decimal InterestCal { get; set; }
}

public class LedgerSummaryCompanyRef
{
    public string Value { get; set; } = "";
    public string Label { get; set; } = "";
    public int CompanyType { get; set; } = 2;
    public string? CompanyName { get; set; }
    public int CompanyId { get; set; }
}

public class LedgerSummaryRowDto
{
    public string CompanyName { get; set; } = "";
    public string LedgerName { get; set; } = "";
    public DateTime? Date { get; set; }
    public string Particulars { get; set; } = "";
    public string VoucherType { get; set; } = "";
    public string VoucherNo { get; set; } = "";
    public string VoucherRef { get; set; } = "";
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Currency { get; set; }
    public decimal? DebitFc { get; set; }
    public decimal? CreditFc { get; set; }
    public decimal ExcRate { get; set; }
    public decimal Closing { get; set; }
    public decimal ClosingFc { get; set; }
    public int Days { get; set; }
    public decimal Interest { get; set; }
    public bool IsOpening { get; set; }
    public string? ApprovalStatus { get; set; }
}

public class LedgerSummaryResultDto
{
    public decimal OpeningBalance { get; set; }
    public decimal DebitTotal { get; set; }
    public decimal CreditTotal { get; set; }
    public decimal ClosingBalance { get; set; }
    public int CompanyCount { get; set; }
    public int LedgerCount { get; set; }
    public int PairCount { get; set; }
    public List<LedgerSummaryRowDto> Rows { get; set; } = new();
}

public class LedgerSummaryExportRequest
{
    public LedgerSummaryResultDto Result { get; set; } = new();
    public string? DateFrom { get; set; }
    public string? DateTo { get; set; }
    public List<string>? CompanyLabels { get; set; }
    public List<string>? LedgerNames { get; set; }
}
