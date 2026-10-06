using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/hr/master")]
public class HrMasterController : ControllerBase
{
    private readonly HrMasterService _service;
    private readonly HrAccessService _access;

    public HrMasterController(HrMasterService service, HrAccessService access)
    {
        _service = service;
        _access = access;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] HrMasterQuery query, [FromQuery] string username = "", [FromQuery] int limit = 1000)
    {
        try
        {
            var denied = await DenyUnlessFullAccess(username);
            if (denied != null) return denied;
            return Ok(await _service.GetReportAsync(query, Math.Clamp(limit, 1, 5000)));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
        }
    }

    [HttpGet("companies")]
    public async Task<IActionResult> Companies([FromQuery] string username = "")
    {
        try
        {
            var denied = await DenyUnlessFullAccess(username);
            if (denied != null) return denied;
            return Ok(await _service.GetCompaniesAsync());
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
        }
    }

    [HttpGet("excel")]
    public async Task<IActionResult> Excel([FromQuery] HrMasterQuery query, [FromQuery] string username = "")
    {
        try
        {
            var denied = await DenyUnlessFullAccess(username);
            if (denied != null) return denied;
            var bytes = await _service.BuildExcelAsync(query);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"hr-master-{DateTime.Now:yyyy-MM-dd}.xlsx");
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
        }
    }

    private async Task<IActionResult?> DenyUnlessFullAccess(string username)
    {
        var access = await _access.ResolveAsync(username);
        return access.HasFullAccess
            ? null
            : StatusCode(403, new { message = "HR master data is available to HR full-access users only." });
    }
}
