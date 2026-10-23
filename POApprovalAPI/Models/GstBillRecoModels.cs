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
    public int PotentialMatch { get; set; }
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
    public int AttachmentCount { get; set; }
    public string DmsRefType { get; set; } = "";
    public string DmsRefEntryNo { get; set; } = "";
    public Dictionary<string, decimal?> Ledgers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class GstBillRecoExportRequest
{
    public string? Username { get; set; }
    public List<GstBillRecoRow> Rows { get; set; } = [];
}

public class Gst2BPeriodRow
{
    public string BuyerGstin { get; set; } = "";
    public string GroupName { get; set; } = "";
    public DateTime ReturnPeriod { get; set; }
    public int RowCount { get; set; }
    public string FileName { get; set; } = "";
    public DateTime DownloadedAt { get; set; }
}

public class Gst2BDocumentRow
{
    public string BuyerGstin { get; set; } = "";
    public string GroupName { get; set; } = "";
    public DateTime ReturnPeriod { get; set; }
    public string Section { get; set; } = "";
    public string SupplierGstin { get; set; } = "";
    public string SupplierName { get; set; } = "";
    public string DocumentType { get; set; } = "";
    public string InvoiceType { get; set; } = "";
    public string DocumentNumber { get; set; } = "";
    public string DocumentNumberNorm { get; set; } = "";
    public DateTime DocumentDate { get; set; }
    public string OriginalDocumentNumber { get; set; } = "";
    public DateTime? OriginalDocumentDate { get; set; }
    public string PlaceOfSupply { get; set; } = "";
    public string ReverseCharge { get; set; } = "";
    public decimal DocumentValue { get; set; }
    public decimal Taxable { get; set; }
    public decimal Igst { get; set; }
    public decimal Cgst { get; set; }
    public decimal Sgst { get; set; }
    public decimal CgstSgst { get; set; }
    public decimal Cess { get; set; }
    public string ItcAvailability { get; set; } = "";
    public string ItcReason { get; set; } = "";
    public string ApplicableTaxRate { get; set; } = "";
    public string ItcReduced { get; set; } = "";
    public decimal ReductionIgst { get; set; }
    public decimal ReductionCgst { get; set; }
    public decimal ReductionSgst { get; set; }
    public decimal ReductionCess { get; set; }
    public string Source { get; set; } = "";
    public string Irn { get; set; } = "";
    public DateTime? IrnDate { get; set; }
    public string Gstr1Period { get; set; } = "";
    public DateTime? Gstr1FilingDate { get; set; }
    public string Remarks { get; set; } = "";
}

public class Gst2BSaveResult
{
    public bool AlreadyFrozen { get; set; }
    public int InsertedRows { get; set; }
    public string BuyerGstin { get; set; } = "";
    public string GroupName { get; set; } = "";
    public DateTime ReturnPeriod { get; set; }
    public string Sections { get; set; } = "";
}
