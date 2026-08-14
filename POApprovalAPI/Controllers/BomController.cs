using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Documents;
using POApprovalAPI.Models;
using POApprovalAPI.Services;
using QuestPDF.Fluent;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/bom")]
public class BomController : ControllerBase
{
    private readonly BomService _service;
    private readonly EmailService _emailService;

    public BomController(BomService service, EmailService emailService)
    {
        _service = service;
        _emailService = emailService;
    }

    [HttpPost("email")]
    public async Task<IActionResult> SendEmail([FromBody] BomSendEmailRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.FilePoNo))
                return BadRequest(new { message = "Quotation number is required." });
            if (string.IsNullOrWhiteSpace(request.To))
                return BadRequest(new { message = "Recipient email (To) is required." });

            var model = await _service.BuildPdfModelAsync(request.FilePoNo);
            if (model is null)
                return NotFound(new { message = "BOM not found for this quotation number." });

            var pdfBytes = new BillOfMaterialDocument(model).GeneratePdf();
            var fileName = $"{SanitizeFileName(model.QtnNo)}.pdf";
            var subject = string.IsNullOrWhiteSpace(request.Subject)
                ? $"BOM - {model.QtnNo} - {model.PartyName}"
                : request.Subject.Trim();
            var body = string.IsNullOrWhiteSpace(request.Body)
                ? "Please find attached Bill of Material (BOM) PDF."
                : request.Body.Trim();

            await _emailService.SendMailAndWaitAsync(
                request.To.Trim(),
                subject,
                body,
                [new EmailAttachmentData(fileName, "application/pdf", pdfBytes)],
                cc: request.Cc?.Trim(),
                bcc: request.Bcc?.Trim());

            return Ok(new { message = "BOM email sent successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("party-mapping")]
    public async Task<IActionResult> GetPartyMapping()
    {
        try
        {
            return Ok(await _service.GetPartyMappingAsync());
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomers()
    {
        try
        {
            return Ok(await _service.GetCustomersAsync());
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("customers/{*companyName}")]
    public async Task<IActionResult> GetCustomer(string companyName)
    {
        try
        {
            var customer = await _service.GetCustomerAsync(Uri.UnescapeDataString(companyName));
            if (customer is null)
                return NotFound(new { message = "Customer not found in Company Master." });
            return Ok(customer);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPut("customers/{*companyName}")]
    public async Task<IActionResult> UpdateCustomer(string companyName, [FromBody] BomCustomerUpdateRequest request)
    {
        try
        {
            var updated = await _service.UpdateCustomerAsync(Uri.UnescapeDataString(companyName), request);
            if (!updated)
                return NotFound(new { message = "Customer not found in Company Master." });
            return Ok(new { message = "Customer updated." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        try
        {
            return Ok(await _service.GetUsersAsync());
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] BomSearchRequest request)
    {
        try
        {
            return Ok(await _service.SearchAsync(request));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("{*filePoNo}")]
    public async Task<IActionResult> GetDetail(string filePoNo)
    {
        try
        {
            var detail = await _service.GetDetailAsync(Uri.UnescapeDataString(filePoNo));
            if (detail is null)
                return NotFound(new { message = "BOM not found for this quotation number." });
            return Ok(detail);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(value.Select(ch => invalid.Contains(ch) ? '-' : ch).ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "BOM" : cleaned;
    }
}
