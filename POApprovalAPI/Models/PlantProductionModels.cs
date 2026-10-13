namespace POApprovalAPI.Models;

public class LoomRollRequest
{
    public int LoomNo { get; set; }
    public string RollNo { get; set; } = "";
    public string Quality { get; set; } = "";
    public string Mesh { get; set; } = "";
    public decimal Gsm { get; set; }
    public string Sector { get; set; } = "";
    public decimal Metre { get; set; }
    public decimal Gross { get; set; }
    public decimal Tare { get; set; }
    public decimal Cover { get; set; }
    public decimal Net { get; set; }
    public decimal Gpm { get; set; }
    public decimal ActualGsm { get; set; }
    public string Remarks { get; set; } = "";
    public string Party { get; set; } = "";
    public string Order { get; set; } = "";
    public decimal WarpQty { get; set; }
    public decimal WeftQty { get; set; }
    public string WarpCode { get; set; } = "";
    public string WeftCode { get; set; } = "";
    public string WarpName { get; set; } = "";
    public string WeftName { get; set; } = "";
    public string FgCode { get; set; } = "";
    public int Logic { get; set; }
}

public class LoomSaveRequest
{
    public string Company { get; set; } = "";
    public string UserName { get; set; } = "";
    public DateTime EntryDate { get; set; }
    public string Shift { get; set; } = "A";
    public int? RecordLogId { get; set; }
    public List<LoomRollRequest> Rolls { get; set; } = new();
}

public class NeedleRollRequest
{
    public int? TransId { get; set; }
    public int LoomNo { get; set; }
    public string RollNo { get; set; } = "";
    public decimal Size { get; set; }
    public decimal ReqGpm { get; set; }
    public string Colour { get; set; } = "";
    public string Marking { get; set; } = "";
    public string Quality { get; set; } = "";
    public decimal Net { get; set; }
    public string Party { get; set; } = "";
    public string Order { get; set; } = "";
    public string Remarks { get; set; } = "";
    public string FgCode { get; set; } = "";
    public string RollType { get; set; } = "PP Webbing & Tie";
}

public class NeedleSaveRequest
{
    public string Company { get; set; } = "";
    public string UserName { get; set; } = "";
    public DateTime EntryDate { get; set; }
    public string Shift { get; set; } = "A";
    public List<NeedleRollRequest> Rolls { get; set; } = new();
}

public class LaminationRollRequest
{
    public int? TransId { get; set; }
    public string SourceRoll { get; set; } = "";
    public decimal SourceMetre { get; set; }
    public decimal SourceGross { get; set; }
    public decimal SourceTare { get; set; }
    public decimal SourceNet { get; set; }
    public string LamRoll { get; set; } = "";
    public decimal LamSize { get; set; }
    public decimal LamGsm { get; set; }
    public string Colour { get; set; } = "";
    public string Quality { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal LamMetre { get; set; }
    public decimal LamGross { get; set; }
    public decimal LamTare { get; set; }
    public decimal LamNet { get; set; }
    public decimal ActualGpm { get; set; }
    public decimal ActualGsm { get; set; }
    public string Remarks { get; set; } = "";
    public string Sector { get; set; } = "";
    public string InternalRoll { get; set; } = "";
    public string Party { get; set; } = "";
    public string Order { get; set; } = "";
    public string UlCode { get; set; } = "";
    public string LamCode { get; set; } = "";
}

public class LaminationSaveRequest
{
    public string Company { get; set; } = "";
    public string UserName { get; set; } = "";
    public DateTime EntryDate { get; set; }
    public string Shift { get; set; } = "A";
    public List<LaminationRollRequest> Rolls { get; set; } = new();
}

public class ProductionDeleteRequest
{
    public string Company { get; set; } = "";
    public string UserName { get; set; } = "";
    public DateTime EntryDate { get; set; }
    public int Id { get; set; }
}
