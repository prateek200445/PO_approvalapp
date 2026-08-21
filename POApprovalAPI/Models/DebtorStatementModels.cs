namespace POApprovalAPI.Models;

public class DebtorStatementQueryRequest
{
    public string Company { get; set; } = DebtorStatementDefaults.CompanyGroup;
    public DateTime AsOn { get; set; }
    public bool IncludeCurrentAssets { get; set; } = true;
}

public static class DebtorStatementDefaults
{
    public const string CompanyGroup = "G-Plastene India Limited";
    public const string ExportGstin = "99EXPOR0000E9Z9";
    public const string DomesticGstin = "88INDIG0000I8Z8";
}

public class DebtorStatementResultDto
{
    public string Company { get; set; } = "";
    public string CompanyLabel { get; set; } = "";
    public string AsOn { get; set; } = "";
    public bool IncludeCurrentAssets { get; set; }
    public string FreezeRule { get; set; } = "as-on";
    public string AllocationRule { get; set; } = "LIFO";
    public DebtorStatementKpisDto Kpis { get; set; } = new();
    public List<DebtorBillRowDto> Bills { get; set; } = new();
    public List<DebtorPivotRowDto> Pivot { get; set; } = new();
    public List<DebtorBookDebtRowDto> BookDebts { get; set; } = new();
}

public class DebtorStatementKpisDto
{
    public int CompanyCount { get; set; }
    public int PartyCount { get; set; }
    public int BillCount { get; set; }
    public int OpenBillCount { get; set; }
    public decimal OriginalTotal { get; set; }
    public decimal AllocatedTotal { get; set; }
    public decimal NetTotal { get; set; }
    public decimal BookTotal { get; set; }
    public decimal DiffTotal { get; set; }
    public int LifoPartyCount { get; set; }
    public int NonBillGapPartyCount { get; set; }
}

public class DebtorBillRowDto
{
    public string Type { get; set; } = "";
    public string Category { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string PartyName { get; set; } = "";
    public string Gstin { get; set; } = "";
    public string InvoiceNo { get; set; } = "";
    public string InvoiceDate { get; set; } = "";
    public decimal OriginalAmount { get; set; }
    public decimal AllocatedAmount { get; set; }
    public decimal NetAmount { get; set; }
    public int Days { get; set; }
    public string Ageing { get; set; } = "";
    public string Ageing2 { get; set; } = "";
    public string Status { get; set; } = "";
    public string Under { get; set; } = "";
}

public class DebtorPivotRowDto
{
    public string PartyName { get; set; } = "";
    public string Gstin { get; set; } = "";
    public string Type { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal ZeroTo120 { get; set; }
    public decimal OneTo90 { get; set; }
    public decimal NinetyOneTo120 { get; set; }
    public decimal OneTwentyOneTo180 { get; set; }
    public decimal Over180 { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal AsPerBook { get; set; }
    public decimal Diff { get; set; }
    public decimal OriginalTotal { get; set; }
    public decimal AllocatedTotal { get; set; }
    public string Status { get; set; } = "";
}

public class DebtorBookDebtRowDto
{
    public string Bucket { get; set; } = "";
    public decimal Government { get; set; }
    public decimal Associates { get; set; }
    public decimal Other { get; set; }
    public decimal Total { get; set; }
}
