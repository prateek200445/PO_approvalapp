namespace POApprovalAPI.Models;

public class PlantConsumptionLineRequest
{
    public string Quality { get; set; } = "";
    public string Grade { get; set; } = "";
    public string ItemCode { get; set; } = "";
    public decimal Qty { get; set; }
    public decimal Percent { get; set; }
    public decimal PpBags { get; set; }
}

public class PlantConsumptionSaveRequest
{
    public string Company { get; set; } = "";
    public string UserName { get; set; } = "";
    public DateTime EntryDate { get; set; }
    public string Shift { get; set; } = "A";
    public string ProductionType { get; set; } = "Sell";
    public string TimeIn { get; set; } = "00:00:00";
    public string TimeOut { get; set; } = "00:00:00";
    public string Plant { get; set; } = "";
    public string PlantSub { get; set; } = "";
    public string Product { get; set; } = "";
    public string Sector { get; set; } = "";
    public string FromWarehouse { get; set; } = "";
    public string ToWarehouse { get; set; } = "";
    public string Buyer { get; set; } = "";
    public string BuyerOrder { get; set; } = "";
    public DateTime? BuyerOrderDate { get; set; }
    public string MarketingInvoice { get; set; } = "";
    public decimal Wastage { get; set; }
    public decimal TrimWastage { get; set; }
    public decimal SweepingWastage { get; set; }
    public decimal FabricTrim { get; set; }
    public decimal FabricWaste { get; set; }
    public decimal LumpsWastage { get; set; }
    public decimal Rolls { get; set; }
    public int? GroupSrNo { get; set; }
    public List<PlantConsumptionLineRequest> Lines { get; set; } = new();
}

public class PlantConsumptionDeleteRequest
{
    public string Company { get; set; } = "";
    public string Plant { get; set; } = "";
    public string FromWarehouse { get; set; } = "";
    public string ToWarehouse { get; set; } = "";
    public int GroupSrNo { get; set; }
}
