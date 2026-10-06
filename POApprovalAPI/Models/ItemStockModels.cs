namespace POApprovalAPI.Models;

public class ItemStockQueryRequest
{
    public string CompanyName { get; set; } = "";
    public string ItemCode { get; set; } = "";
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
}

public class ItemStockSummaryLine
{
    public string LineType { get; set; } = "";
    public string MovementType { get; set; } = "";
    public decimal InwardQty { get; set; }
    public decimal OutwardQty { get; set; }
    public decimal Balance { get; set; }
}

public class ItemStockTxnLine
{
    public DateTime TxnDate { get; set; }
    public string MovementType { get; set; } = "";
    public string DocNo { get; set; } = "";
    public decimal InwardQty { get; set; }
    public decimal OutwardQty { get; set; }
    public decimal Balance { get; set; }
}

public class ItemRollLine
{
    public string Godown { get; set; } = "";
    public string RollNo { get; set; } = "";
    public string ItemName { get; set; } = "";
    public decimal NetWt { get; set; }
    public DateTime? ProducedOn { get; set; }
}

public class ItemStockResult
{
    public string CompanyName { get; set; } = "";
    public string ItemCode { get; set; } = "";
    public string ItemName { get; set; } = "";
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public List<ItemStockSummaryLine> Summary { get; set; } = new();
    public List<ItemStockTxnLine> Transactions { get; set; } = new();
    public List<ItemRollLine> Rolls { get; set; } = new();
    public int RollCount { get; set; }
    public decimal RollNetWt { get; set; }
    public string? RollNote { get; set; }
}
