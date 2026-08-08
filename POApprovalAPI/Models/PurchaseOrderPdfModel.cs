namespace POApprovalAPI.Models;

public class PurchaseOrderPdfModel
{
    /// <summary>Document title shown in header (e.g. "Purchase Order" or "Work Order").</summary>
    public string DocumentTitle { get; set; } = "Purchase Order";

    public string PoNo { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string FormerlyKnownAs { get; set; } = "";
    public string VendorName { get; set; } = "";

    public string HoAddress { get; set; } = "";
    public string CompanyAddress { get; set; } = "";
    public string VendorAddress { get; set; } = "";
    public string PostalAddress { get; set; } = "";

    public string CompanyGst { get; set; } = "";
    public string VendorGst { get; set; } = "";

    public string ContactName { get; set; } = "";
    public string ContactNo { get; set; } = "";
    /// <summary>Store contact email shown in term 3 (from loginrights).</summary>
    public string StoreEmail { get; set; } = "";
    /// <summary>Currency code from Vw_PurchaseOrder (e.g. Rs, USD).</summary>
    public string Currency { get; set; } = "";

    public DateTime? PoDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public DateTime? IndentDate { get; set; }
    public DateTime? ApprovalDate { get; set; }

    public string CreatedBy { get; set; } = "";
    public string DepttName { get; set; } = "";
    public string IndentNo { get; set; } = "";
    public string IndentName { get; set; } = "";
    public string PurchaseRefNo { get; set; } = "";

    public string DeliveryTerms { get; set; } = "";
    public string DeliverySchedule { get; set; } = "";
    public string PaymentTerms { get; set; } = "";
    public string ModeOfTransport { get; set; } = "";
    public string Note { get; set; } = "";
    public string SpecialNote { get; set; } = "";

    public decimal SubTotal { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal CGSTAmount { get; set; }
    public decimal SGSTAmount { get; set; }
    public decimal IGSTAmount { get; set; }
    public decimal TdsAmount { get; set; }

    public List<PurchaseOrderItem> Items { get; set; } = new();
}

public class PurchaseOrderItem
{
    public string ItemCode { get; set; } = "";
    public string ItemDesc { get; set; } = "";
    public string Unit { get; set; } = "";
    public string HsnCode { get; set; } = "";
    public int SerialNo { get; set; }

    public decimal Qty { get; set; }
    public decimal Rate { get; set; }
    public decimal Discount { get; set; }
    public decimal Amount { get; set; }

    public decimal GstPer { get; set; }
    public decimal CGSTAmount { get; set; }
    public decimal SGSTAmount { get; set; }
    public decimal IGSTAmount { get; set; }
}
