namespace POApprovalAPI.Models;

public class GateAccess
{
    public bool CanOperate { get; init; }
    public bool CanViewRegister { get; init; }
    public bool CanCorrect { get; init; }
    public bool CanManageGates { get; init; }
    public string RoleLabel { get; init; } = "";

    public bool Any => CanOperate || CanViewRegister || CanCorrect || CanManageGates;
}

public class GateContextDto
{
    public string RoleLabel { get; set; } = "";
    public bool CanOperate { get; set; }
    public bool CanViewRegister { get; set; }
    public bool CanCorrect { get; set; }
    public bool CanManageGates { get; set; }
    public int LongStayHours { get; set; }
    public string ServerDate { get; set; } = "";
    public List<GateSiteDto> Gates { get; set; } = [];
    public List<GatePurposeDto> Purposes { get; set; } = [];
}

public class GateSiteDto
{
    public int GateId { get; set; }
    public string FactoryCode { get; set; } = "";
    public string FactoryName { get; set; } = "";
    public string GateCode { get; set; } = "";
    public string GateName { get; set; } = "";
}

public class GatePurposeDto
{
    public int PurposeId { get; set; }
    public string PurposeCode { get; set; } = "";
    public string PurposeName { get; set; } = "";
}

public class GateEntryDto
{
    public long EntryId { get; set; }
    public string GateEntryNumber { get; set; } = "";
    public string FactoryCode { get; set; } = "";
    public string FactoryName { get; set; } = "";
    public int GateId { get; set; }
    public string GateName { get; set; } = "";
    public string VehicleNumber { get; set; } = "";
    public string DriverName { get; set; } = "";
    public string? DriverMobile { get; set; }
    public string? PartyName { get; set; }
    public string? DocumentType { get; set; }
    public string? DocumentNo { get; set; }
    public string PurposeCode { get; set; } = "";
    public string PurposeName { get; set; } = "";
    public string? Remarks { get; set; }
    public string Status { get; set; } = "";
    public DateTime EntryAt { get; set; }
    public string EntryUserId { get; set; } = "";
    public DateTime? ExitAt { get; set; }
    public string? ExitUserId { get; set; }
    public int? ExitGateId { get; set; }
    public string? ExitGateName { get; set; }
    public string? ExitRemarks { get; set; }
    public int? DurationMinutes { get; set; }
    public bool IsLongStay { get; set; }
}

public class GateSummaryDto
{
    public int InsideCount { get; set; }
    public int TodayEntries { get; set; }
    public int TodayExits { get; set; }
    public decimal? AverageTurnaroundMinutes { get; set; }
    public int LongStayCount { get; set; }
    public int LongStayHours { get; set; }
    public string ServerDate { get; set; } = "";
    public List<GateEntryDto> LongStay { get; set; } = [];
}

public class GateRegisterPage
{
    public int Total { get; set; }
    public List<GateEntryDto> Rows { get; set; } = [];
}

public class GateAuditDto
{
    public long AuditId { get; set; }
    public long EntryId { get; set; }
    public string Action { get; set; } = "";
    public string ChangedBy { get; set; } = "";
    public DateTime ChangedAt { get; set; }
    public string? FieldName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Reason { get; set; }
}

public class GateEntryCreateRequest
{
    public string? Username { get; set; }
    public string? FactoryCode { get; set; }
    public int GateId { get; set; }
    public string? VehicleNumber { get; set; }
    public string? DriverName { get; set; }
    public string? DriverMobile { get; set; }
    public string? PartyName { get; set; }
    public string? DocumentType { get; set; }
    public string? DocumentNo { get; set; }
    public string? PurposeCode { get; set; }
    public string? Remarks { get; set; }
}

public class GateExitRequest
{
    public string? Username { get; set; }
    public int? ExitGateId { get; set; }
    public string? ExitRemarks { get; set; }
}

public class GateCorrectRequest
{
    public string? Username { get; set; }
    public string? Reason { get; set; }
    public string? VehicleNumber { get; set; }
    public string? DriverName { get; set; }
    public string? DriverMobile { get; set; }
    public string? PartyName { get; set; }
    public string? PurposeCode { get; set; }
    public string? Remarks { get; set; }
}

public class GateCreateRequest
{
    public string? Username { get; set; }
    public string? FactoryCode { get; set; }
    public string? FactoryName { get; set; }
    public string? GateCode { get; set; }
    public string? GateName { get; set; }
}

public class GateDocumentLineDto
{
    public string? ItemCode { get; set; }
    public string? ItemDesc { get; set; }
    public decimal? Qty { get; set; }
    public decimal? Rate { get; set; }
    public decimal? Total { get; set; }
}

public class GateDocumentDto
{
    public string DocumentType { get; set; } = "";
    public string DocumentNo { get; set; } = "";
    public string? SupplierName { get; set; }
    public string? CompanyName { get; set; }
    public string? Department { get; set; }
    public string? Currency { get; set; }
    public decimal? TotalAmount { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public List<GateDocumentLineDto> Lines { get; set; } = [];
}

public class GateDocumentLookupDto
{
    public bool NeedsChoice { get; set; }
    public List<string> Choices { get; set; } = [];
    public GateDocumentDto? Document { get; set; }
}

public class GateRegisterQuery
{
    public string? Username { get; set; }
    public string? FactoryCode { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? VehicleNumber { get; set; }
    public string? Driver { get; set; }
    public string? Party { get; set; }
    public string? PurposeCode { get; set; }
    public string? Status { get; set; }
    public bool LongStayOnly { get; set; }
}
