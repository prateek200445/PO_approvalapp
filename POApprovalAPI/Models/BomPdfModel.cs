namespace POApprovalAPI.Models;

public sealed class BomPdfModel
{
    public string QtnNo { get; set; } = "";
    public string PartyName { get; set; } = "";
    public DateTime? Date { get; set; }
    public string User { get; set; } = "";
    public string RefNo { get; set; } = "";
    public string PoNo { get; set; } = "";
    public string PoNos { get; set; } = "";
    public string MarketingInvNo { get; set; } = "";

    public string BagType { get; set; } = "";
    public string SizeType { get; set; } = "";
    public double? SizeL { get; set; }
    public double? SizeW { get; set; }
    public double? SizeH { get; set; }
    public string Swl { get; set; } = "";
    public string SfRatio { get; set; } = "";
    public string Qty { get; set; } = "";
    public string QtyUnit { get; set; } = "";
    public string PrintType { get; set; } = "";

    public string Doc { get; set; } = "";
    public string Doc1 { get; set; } = "";
    public string Doc2 { get; set; } = "";
    public string LoopSpec { get; set; } = "";
    public string LinerSpec { get; set; } = "";
    public string TopSpoutType { get; set; } = "";
    public string BottomType { get; set; } = "";
    public string FabColor { get; set; } = "";
    public string IsDropLoop { get; set; } = "";
    public string RpFabric { get; set; } = "";
    public string KnotType { get; set; } = "";

    public double? TotalKgPerBag { get; set; }
    public string Instruction { get; set; } = "";
    public string PrintingRemarks { get; set; } = "";
    public string BodyRemarks { get; set; } = "";

    public List<BomPdfLine> Lines { get; set; } = new();
}

public sealed class BomPdfLine
{
    public int SortOrder { get; set; }
    public string Heading { get; set; } = "";
    public string Gsm { get; set; } = "";
    public string Lami { get; set; } = "";
    public string Color { get; set; } = "";
    public string FabricSize { get; set; } = "";
    public string CutSize { get; set; } = "";
    public double? TotalMtr { get; set; }
    public double? TotalKg { get; set; }
    public string Gpm { get; set; } = "";
    public string Remarks { get; set; } = "";
}
