using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/hr/engagement")]
public class HrEngagementController : ControllerBase
{
    private readonly HrCelebrationService _celebrations;
    private readonly HrPolicyDocumentService _documents;
    private readonly HrAccessService _access;

    public HrEngagementController(HrCelebrationService celebrations, HrPolicyDocumentService documents, HrAccessService access)
    {
        _celebrations = celebrations;
        _documents = documents;
        _access = access;
    }

    /// <summary>Today's birthdays and work anniversaries in the user's company.</summary>
    [HttpGet("celebrations")]
    public async Task<IActionResult> Celebrations([FromQuery] string username = "")
    {
        try
        {
            return Ok(await _celebrations.GetTodayAsync(username));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
        }
    }

    [HttpGet("documents/categories")]
    public IActionResult DocumentCategories() => Ok(HrPolicyDocumentService.Categories);

    /// <summary>HR sees every document; others see group-wide documents and their own company's.</summary>
    [HttpGet("documents")]
    public async Task<IActionResult> Documents([FromQuery] string username = "")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(username))
                return StatusCode(401, new { message = "Login required." });
            var access = await _access.ResolveAsync(username);
            var company = access.HasFullAccess ? null : await _celebrations.CompanyOfAsync(access.EmpCode);
            return Ok(new
            {
                canManage = access.HasFullAccess,
                company,
                documents = await _documents.ListAsync(company, access.HasFullAccess),
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
        }
    }

    [HttpGet("documents/{docId:int}/file")]
    public async Task<IActionResult> DocumentFile(int docId, [FromQuery] string username = "", [FromQuery] bool download = false)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(username))
                return StatusCode(401, new { message = "Login required." });
            var doc = await _documents.GetFileAsync(docId);
            if (doc is null)
                return NotFound(new { message = "Document not found." });
            var access = await _access.ResolveAsync(username);
            if (!access.HasFullAccess && doc.Value.Company is { } docCompany
                && !string.Equals(docCompany, await _celebrations.CompanyOfAsync(access.EmpCode), StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, new { message = "This document is for another company." });
            return download
                ? File(doc.Value.Content, doc.Value.ContentType, doc.Value.FileName)
                : File(doc.Value.Content, doc.Value.ContentType);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
        }
    }

    [HttpPost("documents")]
    [RequestSizeLimit(HrPolicyDocumentService.MaxBytes + 64 * 1024)]
    public async Task<IActionResult> UploadDocument(
        IFormFile? file,
        [FromForm] string category,
        [FromForm] string title,
        [FromForm] string? company,
        [FromForm] string? description,
        [FromQuery] string username = "")
    {
        try
        {
            var access = await _access.ResolveAsync(username);
            if (!access.HasFullAccess)
                return StatusCode(403, new { message = "Only HR can upload HR documents." });
            if (file is null || file.Length == 0)
                return BadRequest(new { message = "Choose a file to upload." });
            await using var stream = file.OpenReadStream();
            using var buffer = new MemoryStream((int)Math.Min(file.Length, int.MaxValue));
            await stream.CopyToAsync(buffer);
            return Ok(await _documents.SaveAsync(category, title, company, description, file.FileName, buffer.ToArray(), username.Trim()));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
        }
    }

    [HttpDelete("documents/{docId:int}")]
    public async Task<IActionResult> DeleteDocument(int docId, [FromQuery] string username = "")
    {
        try
        {
            var access = await _access.ResolveAsync(username);
            if (!access.HasFullAccess)
                return StatusCode(403, new { message = "Only HR can remove HR documents." });
            return await _documents.DeleteAsync(docId, username.Trim())
                ? Ok(new { message = "Document removed." })
                : NotFound(new { message = "Document not found." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
        }
    }
}
