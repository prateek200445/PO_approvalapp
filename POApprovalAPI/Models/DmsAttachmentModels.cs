namespace POApprovalAPI.Models;

public sealed class DmsAttachmentListResponse
{
    public string PurchaseCode { get; set; } = "";
    public string RefType { get; set; } = "";
    public string RefEntryNo { get; set; } = "";
    public IReadOnlyList<DmsAttachmentDto> Files { get; set; } = Array.Empty<DmsAttachmentDto>();
}

public sealed class DmsAttachmentDto
{
    public int FileId { get; set; }
    public string FileName { get; set; } = "";
    public long FileSize { get; set; }
    public string UploadedBy { get; set; } = "";
    public DateTime? UploadedOn { get; set; }
}
