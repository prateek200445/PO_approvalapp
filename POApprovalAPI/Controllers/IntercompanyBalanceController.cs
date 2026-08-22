using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/Intercompany")]
public class IntercompanyBalanceController : ControllerBase
{
    private readonly IntercompanyBalanceService _service;

    public IntercompanyBalanceController(IntercompanyBalanceService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] DateTime? asOf = null,
        [FromQuery] bool refresh = false)
    {
        try
        {
            var through = asOf ?? DateTime.Today;
            var data = await _service.GetDashboardAsync(through, refresh);
            return Ok(data);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
