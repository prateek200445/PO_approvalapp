namespace POApprovalAPI.Models;

public class GstBillRecoResult
{
    public string CompanyName { get; set; } = "";
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public decimal AmountTolerance { get; set; }
    public string? BuyerGstin { get; set; }
    public string? CompanyGstin { get; set; }
    public string? Warning { get; set; }
    public int TwoBRows { get; set; }
    public int SavedRows { get; set; }
    public int ErpRows { get; set; }
    public int Matched { get; set; }
    public int Mismatch { get; set; }
    public int MissingInErp { get; set; }
    public int MissingIn2B { get; set; }
    public List<GstBillRecoRow> Rows { get; set; } = [];
}

public class GstBillRecoRow
{
    public string Status { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string VoucherType { get; set; } = "";
    public string VoucherNo { get; set; } = "";
    public DateTime? VoucherDate { get; set; }
    public string RefNo { get; set; } = "";
    public DateTime? BillDate { get; set; }
    public string BillNo { get; set; } = "";
    public string Ledger { get; set; } = "";
    public string Party { get; set; } = "";
    public string GstNo { get; set; } = "";
    public decimal? GrossAmount { get; set; }
    public decimal? ErpTaxable { get; set; }
    public decimal? ValueAmount { get; set; }
    public decimal? TwoBTaxable { get; set; }
    public decimal Difference { get; set; }
    public decimal? TotalC { get; set; }
    public decimal? OtherAll { get; set; }
    public decimal? Cgst { get; set; }
    public decimal? Sgst { get; set; }
    public decimal? Igst { get; set; }
    public decimal? TwoBIgst { get; set; }
    public decimal? TwoBCgstSgst { get; set; }
    public string MonthAsPer2B { get; set; } = "";
    public string TwoBPeriod { get; set; } = "";
    public string DocumentType { get; set; } = "";
    public Dictionary<string, decimal?> Ledgers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class GstBillRecoExportRequest
{
    public List<GstBillRecoRow> Rows { get; set; } = [];
}
