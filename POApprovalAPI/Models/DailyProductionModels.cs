namespace POApprovalAPI.Models;

public sealed class DailyProductionProcessResponse
{
    public string DownloadToken { get; set; } = "";
    public string FileName { get; set; } = "";
    public string SheetName { get; set; } = "";
    public DailyProductionSummary Summary { get; set; } = new();
}

public sealed class DailyProductionSummary
{
    public int ProductionRows { get; set; }
    public int UniqueIcoCount { get; set; }
    public int PricedIcoCount { get; set; }
    public int PricedRowCount { get; set; }
    public int UnpricedRowCount { get; set; }
    public double TotalPcs { get; set; }
    public double TotalKgs { get; set; }
    public double TotalSalesValueInr { get; set; }
    public DailyProductionFxInfo Fx { get; set; } = new();
    public List<DailyProductionCurrencyTotal> CurrencyTotals { get; set; } = new();
    public List<DailyProductionLineRow> ByLine { get; set; } = new();
    public List<DailyProductionBandRow> ByWeightBand { get; set; } = new();
    public List<DailyProductionIcoRow> ByIco { get; set; } = new();
    public List<DailyProductionUnpricedRow> Unpriced { get; set; } = new();
    public List<string> Hints { get; set; } = new();
}

public sealed class DailyProductionCurrencyTotal
{
    public string Currency { get; set; } = "";
    public double SalesValue { get; set; }
    public double Pcs { get; set; }
    public double Kgs { get; set; }
}

public sealed class DailyProductionLineRow
{
    public string LineNo { get; set; } = "";
    public double Pcs { get; set; }
    public double Kgs { get; set; }
    public int PricedRows { get; set; }
    public int UnpricedRows { get; set; }
    public List<DailyProductionCurrencyTotal> Currencies { get; set; } = new();
}

public sealed class DailyProductionBandRow
{
    public string Band { get; set; } = "";
    public int SortOrder { get; set; }
    public double Pcs { get; set; }
    public double Kgs { get; set; }
    public List<DailyProductionCurrencyTotal> Currencies { get; set; } = new();
}

public sealed class DailyProductionIcoRow
{
    public string Ico { get; set; } = "";
    public string Consignee { get; set; } = "";
    public string PoNo { get; set; } = "";
    public string BagType { get; set; } = "";
    public string Size { get; set; } = "";
    public string LineNos { get; set; } = "";
    public double Pcs { get; set; }
    public double Kgs { get; set; }
    public double WeightPerPc { get; set; }
    public double? SalesPrice { get; set; }
    public string Currency { get; set; } = "";
    public double? OriginalPrice { get; set; }
    public string OriginalCurrency { get; set; } = "";
    public double? SalesValue { get; set; }
    public double? ValuePerKg { get; set; }
    public bool Priced { get; set; }
}

public sealed class DailyProductionFxInfo
{
    public string AsOf { get; set; } = "";
    public string RateFrom { get; set; } = "";
    public string RateTo { get; set; } = "";
    public bool UsedFallback { get; set; }
    public double Dollar { get; set; }
    public double Euro { get; set; }
    public double Pound { get; set; }
    public double Chf { get; set; }
}

public sealed class DailyProductionUnpricedRow
{
    public string Ico { get; set; } = "";
    public string PoNo { get; set; } = "";
    public string Consignee { get; set; } = "";
    public double Pcs { get; set; }
}
