using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Documents;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

/// <summary>
/// ERP MM → Display → Other → Next Week Pending Payment (<c>ERP.PendingPayment_n</c>).
/// </summary>
[ApiController]
[Route("api/pending-payment")]
public class PendingPaymentController : ControllerBase
{
    private readonly PendingPaymentService _service;

    public PendingPaymentController(PendingPaymentService service)
    {
        _service = service;
    }

    [HttpGet("companies")]
    public async Task<IActionResult> Companies()
    {
        try
        {
            var names = await _service.GetCompaniesAsync();
            return Ok(new { names });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string company = "All Companies",
        [FromQuery] bool refresh = false)
    {
        try
        {
            var data = await _service.GetAsync(company, refresh);
            return Ok(data);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("excel")]
    public async Task<IActionResult> Excel(
        [FromQuery] string company = "All Companies",
        [FromQuery] bool refresh = false)
    {
        try
        {
            var data = await _service.GetAsync(company, refresh);
            if (data.Items.Count == 0)
                return BadRequest(new { message = "Nothing to export for this selection." });

            var bytes = PendingPaymentExcelBuilder.Build(data, company);
            var fileName = $"Next_Week_Pending_Payment_{data.DateFrom}_{data.DateTo}.xlsx";
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
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

    /// <summary>ERP "Punch Payment Request" for selected next-week rows.</summary>
    [HttpPost("punch")]
    public async Task<IActionResult> Punch([FromBody] PendingPaymentPunchRequest request)
    {
        try
        {
            if (request is null)
                return BadRequest(new { message = "Request body is required." });

            var login = (request.LoginName ?? "").Trim();
            if (login.Length == 0)
                return BadRequest(new { message = "LoginName is required." });

            var result = await _service.PunchAsync(request.Lines ?? new(), login);
            if (result.CreatedCount == 0 && result.Errors.Count > 0)
                return BadRequest(result);
            return Ok(result);
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
}

public class PendingPaymentPunchRequest
{
    public string LoginName { get; set; } = "";
    public List<PendingPaymentPunchLineDto> Lines { get; set; } = new();
}
