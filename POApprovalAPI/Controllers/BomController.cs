using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Models;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/bom")]
public class BomController : ControllerBase
{
    private readonly BomService _service;
    private readonly BomEmailBackgroundService _emailQueue;

    public BomController(BomService service, BomEmailBackgroundService emailQueue)
    {
        _service = service;
        _emailQueue = emailQueue;
    }

    [HttpPost("email")]
    public IActionResult SendEmail([FromBody] BomSendEmailRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.FilePoNo))
                return BadRequest(new { message = "Quotation number is required." });
            if (string.IsNullOrWhiteSpace(request.To))
                return BadRequest(new { message = "Recipient email (To) is required." });

            if (!_emailQueue.TryQueueSend(request))
                return StatusCode(503, new { message = "Email queue is busy. Please try again." });

            return Accepted(new { message = "BOM email is being sent. It may take a minute to arrive." });
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

    [HttpGet("parties")]
    public async Task<IActionResult> GetPartyNames()
    {
        try
        {
            return Ok(await _service.GetPartyNamesAsync());
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
            if (IsReservedBomRoute(filePoNo))
                return NotFound(new { message = "BOM not found for this quotation number." });

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

    private static bool IsReservedBomRoute(string filePoNo)
    {
        var first = filePoNo.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? filePoNo;
        return first.Equals("parties", StringComparison.OrdinalIgnoreCase)
            || first.Equals("users", StringComparison.OrdinalIgnoreCase)
            || first.Equals("customers", StringComparison.OrdinalIgnoreCase)
            || first.Equals("party-mapping", StringComparison.OrdinalIgnoreCase)
            || first.Equals("search", StringComparison.OrdinalIgnoreCase)
            || first.Equals("email", StringComparison.OrdinalIgnoreCase);
    }
}
