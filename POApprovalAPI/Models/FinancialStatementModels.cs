namespace POApprovalAPI.Models;

public class TrialBalanceColumnMapping
{
    public string SheetName { get; set; } = "";
    public int HeaderRow { get; set; } = 1;
    public string Particulars { get; set; } = "Particulars";
    public string Opening { get; set; } = "Opening";
    public string Debit { get; set; } = "Debit";
    public string Credit { get; set; } = "Credit";
    public string Closing { get; set; } = "Closing";
    public string? AdjustedClosing { get; set; }
    public string? Group { get; set; }
}

public class TrialBalancePreviewResponse
{
    public string FileName { get; set; } = "";
    public List<string> SheetNames { get; set; } = [];
    public string SelectedSheet { get; set; } = "";
    public int HeaderRow { get; set; }
    public List<string> Headers { get; set; } = [];
    public TrialBalanceColumnMapping SuggestedMapping { get; set; } = new();
    public int DataRowCount { get; set; }
    public List<Dictionary<string, string?>> SampleRows { get; set; } = [];
}

public class TrialBalanceRowDto
{
    public string Ledger { get; set; } = "";
    public decimal Opening { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Closing { get; set; }
    public string Group { get; set; } = "";
    public string MappedGroup { get; set; } = "";
}

public class LedgerGroupMappingDto
{
    public string Ledger { get; set; } = "";
    public string Group { get; set; } = "";
}

public class ScheduleLineDto
{
    public string Group { get; set; } = "";
    public string Label { get; set; } = "";
    public decimal AmountLakhs { get; set; }
    public decimal RawClosing { get; set; }
    public int LedgerCount { get; set; }
}

public class ScheduleNoteDto
{
    public string Note { get; set; } = "";
    public string Title { get; set; } = "";
    public List<ScheduleLineDto> Lines { get; set; } = [];
    public decimal TotalLakhs { get; set; }
}

public class ReportLineDto
{
    public string Label { get; set; } = "";
    public string Note { get; set; } = "";
    public string LineType { get; set; } = "line";
    public decimal AmountLakhs { get; set; }
    public bool IsHeader { get; set; }
    public bool IsSubtotal { get; set; }
}

public class ReportSectionDto
{
    public string Title { get; set; } = "";
    public List<ReportLineDto> Lines { get; set; } = [];
    public decimal SectionTotalLakhs { get; set; }
}

public class UnmappedLedgerDto
{
    public string Ledger { get; set; } = "";
    public decimal Closing { get; set; }
    public decimal ClosingLakhs { get; set; }
}

public class FinancialStatementResultDto
{
    public string CompanyKey { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string PeriodLabel { get; set; } = "";
    public int TotalLedgers { get; set; }
    public int MappedLedgers { get; set; }
    public int UnmappedLedgers { get; set; }
    public List<ScheduleNoteDto> Schedules { get; set; } = [];
    public List<ReportSectionDto> BalanceSheet { get; set; } = [];
    public List<ReportLineDto> ProfitAndLoss { get; set; } = [];
    public List<UnmappedLedgerDto> Unmapped { get; set; } = [];
    public decimal BalanceSheetTotalLakhs { get; set; }
    public decimal TotalAssetsLakhs { get; set; }
    public decimal TotalLiabilitiesAndEquityLakhs { get; set; }
}

public class CompanyMappingSummaryDto
{
    public string CompanyKey { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public int MappingCount { get; set; }
    public bool UsesDefaultMapping { get; set; }
}

public class SaveMappingRequest
{
    public string CompanyKey { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public List<LedgerGroupMappingDto> Mappings { get; set; } = [];
}

public class GenerateFinancialStatementRequest
{
    public string CompanyKey { get; set; } = "default";
    public string CompanyName { get; set; } = "";
    public string PeriodLabel { get; set; } = "";
    public TrialBalanceColumnMapping Mapping { get; set; } = new();
    public List<LedgerGroupMappingDto>? OverrideMappings { get; set; }
}
