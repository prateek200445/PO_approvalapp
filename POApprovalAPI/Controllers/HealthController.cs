using Microsoft.AspNetCore.Mvc;

namespace POApprovalAPI.Controllers;

/// <summary>
/// Application-level health check for POApprovalAPI (no DB / ERP dependency).
/// </summary>
[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "ok" });
    }
}
