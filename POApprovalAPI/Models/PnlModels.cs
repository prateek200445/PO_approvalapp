namespace POApprovalAPI.Models;

public class PnlCompanyOption
{
    public string Value { get; set; } = "";
    public string Label { get; set; } = "";
    public string Kind { get; set; } = "company";
}

public class PnlLedgerLine
{
    public string LedgerName { get; set; } = "";
    public double Amount { get; set; }
    public double AmountLacs { get; set; }
}

public class PnlHeadGroup
{
    public string Category { get; set; } = "";
    public string Head { get; set; } = "";
    public double Amount { get; set; }
    public double AmountLacs { get; set; }
    public List<PnlLedgerLine> Ledgers { get; set; } = new();
}

public class PnlIncomeExpenseResult
{
    public string Company { get; set; } = "";
    public string DateFrom { get; set; } = "";
    public string DateTo { get; set; } = "";
    public double IncomeLacs { get; set; }
    public double ExpenseLacs { get; set; }
    public List<PnlHeadGroup> Heads { get; set; } = new();
}

public class PnlProvisionRow
{
    public string LedgerName { get; set; } = "";
    public double Amount { get; set; }
    public double AmountLacs { get; set; }
}

public class PnlProvisionState
{
    public string Company { get; set; } = "";
    public string Month { get; set; } = "";
    public List<PnlProvisionRow> Rows { get; set; } = new();
    public List<string> LedgerOptions { get; set; } = new();
}

public class PnlProvisionSaveRequest
{
    public string Company { get; set; } = "";
    public string Month { get; set; } = "";
    public List<PnlProvisionRow> Rows { get; set; } = new();
}

public class PnlStockRow
{
    public string Category { get; set; } = "";
    public string Label { get; set; } = "";
    public double OpeningLacs { get; set; }
    public double ClosingLacs { get; set; }
}

public class PnlStockState
{
    public string Company { get; set; } = "";
    public string Month { get; set; } = "";
    public List<PnlStockRow> Rows { get; set; } = new();
}

public class PnlStockSaveRequest
{
    public string Company { get; set; } = "";
    public string Month { get; set; } = "";
    public List<PnlStockRow> Rows { get; set; } = new();
}

public class PnlStockYearColumn
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
}

public class PnlStockYearRow
{
    public string Category { get; set; } = "";
    public string Label { get; set; } = "";
    public double OpeningLacs { get; set; }
    public Dictionary<string, double> Months { get; set; } = new();
}

public class PnlStockYearState
{
    public string Company { get; set; } = "";
    public int FyStart { get; set; }
    public string FyLabel { get; set; } = "";
    public List<PnlStockYearColumn> Columns { get; set; } = new();
    public List<PnlStockYearRow> Rows { get; set; } = new();
}

public class PnlStockYearSaveRequest
{
    public string Company { get; set; } = "";
    public int FyStart { get; set; }
    public List<PnlStockYearRow> Rows { get; set; } = new();
}

public class PnlStatementRow
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public string Kind { get; set; } = "line";
    public double? MonthLacs { get; set; }
    public double? PctToSales { get; set; }
    public double? YtdLacs { get; set; }
    public double? YtdPctToSales { get; set; }
}

public class PnlStatementResult
{
    public string Company { get; set; } = "";
    public string Month { get; set; } = "";
    public string DateFrom { get; set; } = "";
    public string DateTo { get; set; } = "";
    public string YtdFrom { get; set; } = "";
    public double EbitdaLacs { get; set; }
    public double PbtLacs { get; set; }
    public bool StockIncomplete { get; set; }
    public List<PnlStatementRow> Rows { get; set; } = new();
    public List<PnlHeadGroup> Unmapped { get; set; } = new();
}
