using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/dms")]
public class DmsController : ControllerBase
{
    private readonly DmsAttachmentService _dms;
    private readonly DmsRemoteFileService _remoteDms;
    private readonly ILogger<DmsController> _logger;

    public DmsController(
        DmsAttachmentService dms,
        DmsRemoteFileService remoteDms,
        ILogger<DmsController> logger)
    {
        _dms = dms;
        _remoteDms = remoteDms;
        _logger = logger;
    }

    /// <summary>List ERP DMS attachments for a PO or work order (PurchaseCode).</summary>
    [HttpGet("attachments")]
    public async Task<IActionResult> ListAttachments([FromQuery] string poNo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(poNo))
            return BadRequest(new { error = "poNo is required." });

        var result = await _dms.GetAttachmentsAsync(poNo, ct);
        if (result is null)
            return NotFound(new { error = $"No purchase header found for {poNo}." });

        return Ok(result);
    }

    /// <summary>Download a DMS file by FileId (metadata from dms_files).</summary>
    [HttpGet("files/{fileId:int}")]
    public async Task<IActionResult> DownloadFile(int fileId, CancellationToken ct)
    {
        var row = await _dms.GetFileRowAsync(fileId, ct);
        if (row is null)
            return NotFound(new { error = "Attachment not found." });

        var contentType = ResolveContentType(row.FileName);

        var physicalPath = _dms.ResolvePhysicalPath(row);
        if (physicalPath is not null)
            return PhysicalFile(physicalPath, contentType, row.FileName);

        var remoteStream = await _remoteDms.TryGetFileStreamAsync(fileId, ct);
        if (remoteStream is not null)
            return File(remoteStream, contentType, row.FileName);

        _logger.LogWarning(
            "DMS file {FileId} ({Physical}) not found locally or via remote DMS service",
            fileId, row.PhysicalFileName);
        return NotFound(new
        {
            error = "File content is not available. The ERP DMS service may be unreachable from this machine.",
            fileId,
            fileName = row.FileName
        });
    }

    private static string ResolveContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }
}
