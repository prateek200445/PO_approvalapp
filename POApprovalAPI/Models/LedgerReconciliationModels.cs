namespace POApprovalAPI.Models;

public class LedgerColumnMapping
{
    public string? SheetName { get; set; }
    public int HeaderRow { get; set; } = 1;
    public string? Company { get; set; }
    public string? Date { get; set; }
    public string? Particulars { get; set; }
    public string? VoucherNo { get; set; }
    public string? VoucherRef { get; set; }
    public string? Debit { get; set; }
    public string? Credit { get; set; }
}

public class LedgerMatchOptions
{
    /// <summary>Match on transaction date (default true).</summary>
    public bool MatchOnDate { get; set; } = true;
    /// <summary>Allow ±N days when matching dates.</summary>
    public int DateToleranceDays { get; set; } = 0;
    /// <summary>Match on absolute amount (default true).</summary>
    public bool MatchOnAmount { get; set; } = true;
    /// <summary>Accepted absolute amount difference (e.g. rounding).</summary>
    public decimal AmountTolerance { get; set; } = 0m;
    /// <summary>Use voucher / bank reference as a strong primary match key when present.</summary>
    public bool PreferVoucherRef { get; set; } = true;
    /// <summary>Also match on voucher number when both ledgers share the same number (optional).</summary>
    public bool MatchOnVoucherNo { get; set; } = false;
}

public class LedgerEntryDto
{
    public int RowIndex { get; set; }
    public string Company { get; set; } = "";
    public DateTime? Date { get; set; }
    public string Particulars { get; set; } = "";
    public string VoucherNo { get; set; } = "";
    public string VoucherRef { get; set; } = "";
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Amount => Debit > 0 ? Debit : Credit;
    public string Side => Debit > 0 ? "Debit" : Credit > 0 ? "Credit" : "None";
}

public class ExcelPreviewResponse
{
    public string FileName { get; set; } = "";
    public List<string> SheetNames { get; set; } = new();
    public string SelectedSheet { get; set; } = "";
    public int HeaderRow { get; set; } = 1;
    public List<string> Headers { get; set; } = new();
    public LedgerColumnMapping SuggestedMapping { get; set; } = new();
    public int DataRowCount { get; set; }
    public List<Dictionary<string, string>> SampleRows { get; set; } = new();
}

public class ComparisonSummary
{
    public int TotalA { get; set; }
    public int TotalB { get; set; }
    public int Matched { get; set; }
    public int AmountMismatch { get; set; }
    public int MissingInA { get; set; }
    public int MissingInB { get; set; }
    public int Duplicates { get; set; }
    public int PotentialMatches { get; set; }
}

public class ComparisonPairDto
{
    public string Id { get; set; } = "";
    public string Status { get; set; } = "";
    public string Message { get; set; } = "";
    public decimal? Difference { get; set; }
    public LedgerEntryDto? EntryA { get; set; }
    public LedgerEntryDto? EntryB { get; set; }
}

public class ComparisonResultDto
{
    public string CompanyNameA { get; set; } = "Company A";
    public string CompanyNameB { get; set; } = "Company B";
    public ComparisonSummary Summary { get; set; } = new();
    public List<ComparisonPairDto> Results { get; set; } = new();
}
