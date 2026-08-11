namespace POApprovalAPI.Models;

public class LedgerColumnMapping
{
    public string? SheetName { get; set; }
    public int HeaderRow { get; set; } = 1;
    public string? Company { get; set; }
    /// <summary>Voucher date column (primary date when BillNo is missing).</summary>
    public string? Date { get; set; }
    public string? Particulars { get; set; }
    public string? VoucherNo { get; set; }
    public string? VoucherRef { get; set; }
    public string? BillNo { get; set; }
    public string? BillDate { get; set; }
    /// <summary>Signed amount column (− = debit, + = credit). Preferred over Debit/Credit.</summary>
    public string? Amount { get; set; }
    /// <summary>Legacy debit column (optional if Amount is mapped).</summary>
    public string? Debit { get; set; }
    /// <summary>Legacy credit column (optional if Amount is mapped).</summary>
    public string? Credit { get; set; }
}

public class LedgerMatchOptions
{
    /// <summary>Allow ±N days when matching BillDate / VoucherDate.</summary>
    public int DateToleranceDays { get; set; } = 0;
    /// <summary>Accepted absolute amount difference (e.g. rounding).</summary>
    public decimal AmountTolerance { get; set; } = 0m;
}

public class LedgerEntryDto
{
    public int RowIndex { get; set; }
    public string Company { get; set; } = "";
    /// <summary>Voucher date.</summary>
    public DateTime? Date { get; set; }
    public DateTime? BillDate { get; set; }
    public string Particulars { get; set; } = "";
    public string VoucherNo { get; set; } = "";
    public string VoucherRef { get; set; } = "";
    public string BillNo { get; set; } = "";
    /// <summary>Signed amount: negative = debit, positive = credit.</summary>
    public decimal SignedAmount { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Amount => Math.Abs(SignedAmount) > 0 ? Math.Abs(SignedAmount) : (Debit > 0 ? Debit : Credit);
    public string Side =>
        SignedAmount < 0 || Debit > 0 ? "Debit" :
        SignedAmount > 0 || Credit > 0 ? "Credit" : "None";
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
    public int PendingRecords { get; set; }
}

public class ComparisonPairDto
{
    public string Id { get; set; } = "";
    public string Status { get; set; } = "";
    public string Message { get; set; } = "";
    public decimal? Difference { get; set; }
    /// <summary>Summary / primary entry for side A (aggregated when bill group).</summary>
    public LedgerEntryDto? EntryA { get; set; }
    /// <summary>Summary / primary entry for side B (aggregated when bill group).</summary>
    public LedgerEntryDto? EntryB { get; set; }
    /// <summary>All rows in a bill group on side A (1..n).</summary>
    public List<LedgerEntryDto> EntriesA { get; set; } = new();
    /// <summary>All rows in a bill group on side B (1..n).</summary>
    public List<LedgerEntryDto> EntriesB { get; set; } = new();
    /// <summary>bill-group | row</summary>
    public string MatchKind { get; set; } = "row";
}

public class ComparisonResultDto
{
    public string CompanyNameA { get; set; } = "Company A";
    public string CompanyNameB { get; set; } = "Company B";
    public ComparisonSummary Summary { get; set; } = new();
    public List<ComparisonPairDto> Results { get; set; } = new();
}
