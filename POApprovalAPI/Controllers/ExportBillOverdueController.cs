using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExportBillOverdueController : ControllerBase
{
    private readonly ExportBillOverdueService _service;

    public ExportBillOverdueController(ExportBillOverdueService service)
    {
        _service = service;
    }

    [HttpGet("companies")]
    public async Task<IActionResult> GetCompanies()
    {
        try
        {
            var companies = await _service.GetCompaniesAsync();
            return Ok(new { companies });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Failed to load companies." });
        }
    }

    /// <summary>
    /// Export-relevant outstanding group names only (not the full ERP catalog).
    /// </summary>
    [HttpGet("groups")]
    public async Task<IActionResult> GetGroups()
    {
        try
        {
            var groups = await _service.GetGroupsAsync();
            return Ok(new
            {
                groups,
                defaultGroup = ExportBillOverdueService.DefaultGroupName,
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Failed to load groups." });
        }
    }

    /// <summary>
    /// Bill receivable lines — ERP FrmReceivable BindGrid, optional Group Name filter.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetOverdue(
        [FromQuery] string company = "All Companies",
        [FromQuery] DateTime? asOf = null,
        [FromQuery] string groupName = ExportBillOverdueService.DefaultGroupName,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = ExportBillOverdueService.DefaultPageSize,
        [FromQuery] bool refresh = false)
    {
        try
        {
            var through = asOf ?? DateTime.Today;
            var result = await _service.GetOverdueBillsAsync(
                company, through, groupName, page, pageSize, refresh);
            var totalPages = result.PageSize <= 0
                ? 0
                : (int)Math.Ceiling(result.TotalCount / (double)result.PageSize);

            return Ok(new
            {
                items = result.Items,
                company = result.Company,
                asOf = result.AsOf,
                groupName = result.GroupName,
                count = result.Items.Count,
                totalCount = result.TotalCount,
                page = result.Page,
                pageSize = result.PageSize,
                totalPages,
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Failed to load export bill overdue." });
        }
    }
}
