namespace POApprovalAPI.Models;

public sealed class BomSearchRequest
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? PartyName { get; set; }
    public string? UserName { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    /// <summary>Sort direction for SysDate: "asc" (oldest first) or "desc" (newest first).</summary>
    public string SortDirection { get; set; } = "desc";
    /// <summary>Legacy flag kept for compatibility; ignored when SortDirection is set explicitly.</summary>
    public bool DateSortDesc { get; set; } = true;
}

public sealed class BomListItem
{
    public string QtnNo { get; set; } = "";
    public string PartyName { get; set; } = "";
    public double? SizeL { get; set; }
    public double? SizeW { get; set; }
    public double? SizeH { get; set; }
    public DateTime? Date { get; set; }
    public string User { get; set; } = "";
    public string BagType { get; set; } = "";
    public string Swl { get; set; } = "";
    public string Qty { get; set; } = "";
    public double? TotalKg { get; set; }
    public string SrNo { get; set; } = "";
}

public sealed class BomSearchResult
{
    public IReadOnlyList<BomListItem> Items { get; set; } = Array.Empty<BomListItem>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public sealed class BomCustomerOption
{
    public string CompanyName { get; set; } = "";
    public string? Email { get; set; }
    public string? Email1 { get; set; }
    public string? Email2 { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public bool FromMaster { get; set; }
    public int AliasCount { get; set; } = 1;
    public string? MappingType { get; set; }
    public string? OfficialName { get; set; }
}

public sealed class BomCustomerUpdateRequest
{
    public string? Email { get; set; }
    public string? Email1 { get; set; }
    public string? Email2 { get; set; }
    public string? CnctPerson { get; set; }
    public string? TelNo1 { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
}

public sealed class BomHeader
{
    public string QtnNo { get; set; } = "";
    public string PartyName { get; set; } = "";
    public DateTime? Date { get; set; }
    public string User { get; set; } = "";
    public string BagType { get; set; } = "";
    public double? SizeL { get; set; }
    public double? SizeW { get; set; }
    public double? SizeH { get; set; }
    public string SizeType { get; set; } = "";
    public string Swl { get; set; } = "";
    public string SfRatio { get; set; } = "";
    public string Qty { get; set; } = "";
    public string QtyUnit { get; set; } = "";
    public double? TotalKg { get; set; }
    public string PrintType { get; set; } = "";
    public string PoNo { get; set; } = "";
    public string PoNos { get; set; } = "";
    public string SrNo { get; set; } = "";
    public string Instruction { get; set; } = "";
    public string RefNo { get; set; } = "";
    public string Doc { get; set; } = "";
    public string Doc1 { get; set; } = "";
    public string Doc2 { get; set; } = "";
    public string LoopSpec { get; set; } = "";
    public string LinerSpec { get; set; } = "";
    public string TopSpoutType { get; set; } = "";
    public string BottomType { get; set; } = "";
    public string FabColor { get; set; } = "";
    public string PrintingRemarks { get; set; } = "";
    public string BodyRemarks { get; set; } = "";
    public string MarketingInvNo { get; set; } = "";
    public string IsDropLoop { get; set; } = "";
    public string RpFabric { get; set; } = "";
    public string KnotType { get; set; } = "";
}

public sealed class BomLineItem
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

public sealed class BomDetailResult
{
    public BomHeader Header { get; set; } = new();
    public IReadOnlyList<BomLineItem> Lines { get; set; } = Array.Empty<BomLineItem>();
    public IReadOnlyList<BomReportLine> ReportLines { get; set; } = Array.Empty<BomReportLine>();
}

public sealed class BomSendEmailRequest
{
    public string FilePoNo { get; set; } = "";
    public string To { get; set; } = "";
    public string? Cc { get; set; }
    public string? Bcc { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
}

public sealed class BomEmailJobStatus
{
    public string JobId { get; set; } = "";
    public string FilePoNo { get; set; } = "";
    public string To { get; set; } = "";
    public string State { get; set; } = "queued";
    public string? Error { get; set; }
    public DateTime QueuedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public sealed class BomReportLine
{
    public string Heading { get; set; } = "";
    public string Gsm { get; set; } = "";
    public string Lami { get; set; } = "";
    public string Color { get; set; } = "";
    public string FabricSize { get; set; } = "";
    public string CutSize { get; set; } = "";
    public double? TotalMtr { get; set; }
    public double? HeadTotalKg { get; set; }
    public string Remarks { get; set; } = "";
}
