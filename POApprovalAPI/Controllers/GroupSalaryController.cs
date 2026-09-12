using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/group-salary")]
public class GroupSalaryController : ControllerBase
{
    private readonly GroupSalaryService _service;

    public GroupSalaryController(GroupSalaryService service)
    {
        _service = service;
    }

    [HttpGet("companies")]
    public async Task<IActionResult> Companies()
    {
        try
        {
            var list = await _service.GetCompaniesAsync();
            return Ok(list);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] string? company = null)
    {
        try
        {
            var (from, to) = ResolveRange(dateFrom, dateTo);
            var data = await _service.GetDashboardAsync(from, to, company);
            return Ok(data);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("excel")]
    public async Task<IActionResult> ExportExcel(
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] string? company = null)
    {
        try
        {
            var (from, to) = ResolveRange(dateFrom, dateTo);
            var bytes = await _service.BuildExcelAsync(from, to, company);
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"group-salary-{from:yyyyMM}-{to:yyyyMM}.xlsx");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    private static (DateTime From, DateTime To) ResolveRange(DateTime? dateFrom, DateTime? dateTo)
    {
        var today = DateTime.Today;
        var fyStart = today.Month >= 4
            ? new DateTime(today.Year, 4, 1)
            : new DateTime(today.Year - 1, 4, 1);
        var from = (dateFrom ?? fyStart).Date;
        var to = (dateTo ?? today).Date;
        return (from, to);
    }
}
