namespace POApprovalAPI.Models;

public class PurchaseOrderPdfModel
{
    public string PoNo { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string VendorName { get; set; } = "";
    public string DeliveryTerms { get; set; } = "";
    public string PaymentTerms { get; set; } = "";
    public string DispatchAddress { get; set; } = "";
    public string Note { get; set; } = "";

    public decimal TotalAmount { get; set; }

    public decimal CGSTAmount { get; set; }
    public decimal SGSTAmount { get; set; }
    public decimal IGSTAmount { get; set; }

    public List<PurchaseOrderItem> Items { get; set; } = new();
}

public class PurchaseOrderItem
{
    public string ItemDesc { get; set; } = "";
    public string Unit { get; set; } = "";
    public decimal Qty { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }

    public decimal Discount { get; set; }
}