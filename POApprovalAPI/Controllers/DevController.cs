using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

/// <summary>
/// Development-only utilities. No database writes.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DevController : ControllerBase
{
    private readonly EmailService _emailService;
    private readonly IWebHostEnvironment _environment;

    public DevController(EmailService emailService, IWebHostEnvironment environment)
    {
        _emailService = emailService;
        _environment = environment;
    }

    /// <summary>
    /// Send a sample rejection email (optional attachment). Does not touch PO/WO data.
    /// </summary>
    [HttpPost("test-reject-email")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> TestRejectEmail(
        [FromForm] string to,
        [FromForm] string remarks,
        [FromForm] IFormFile? attachment = null)
    {
        if (!_environment.IsDevelopment())
            return NotFound();

        to = (to ?? "").Trim();
        remarks = (remarks ?? "").Trim();

        if (string.IsNullOrWhiteSpace(to))
            return BadRequest(new { error = "Recipient email (to) is required." });
        if (string.IsNullOrWhiteSpace(remarks))
            return BadRequest(new { error = "Remarks are required." });

        var (attach, attachError) = await RejectRequestHelper.ReadOptionalAttachmentAsync(attachment);
        if (attachError != null)
            return BadRequest(new { error = attachError });

        var body =
            "Dear Sir,\n\n" +
            "This is a TEST rejection notification (no PO/WO was changed).\n\n" +
            $"PO Number: TEST/PO/0001\n" +
            $"Rejected By: Dev Test\n" +
            $"Remarks: {remarks}\n\n" +
            "Regards,\n" +
            "PO Approval API (dev test)";

        await _emailService.SendMail(
            to,
            "TEST — PO TEST/PO/0001 Rejected",
            body,
            attach != null ? [attach] : null);

        return Ok(new
        {
            success = true,
            message = "Test email queued. Check API console for EMAIL SENT / EMAIL ERROR.",
            attachmentIncluded = attach != null,
        });
    }
}
