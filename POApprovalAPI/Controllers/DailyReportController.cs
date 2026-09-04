using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DailyReportController : ControllerBase
{
    private readonly DailyReportService _dailyReportService;
    private readonly HtmlParserService _htmlParserService;
    private readonly MessageFormatterService _messageFormatterService;

    public DailyReportController(
        DailyReportService dailyReportService,
        HtmlParserService htmlParserService,
        MessageFormatterService messageFormatterService)
    {
        _dailyReportService = dailyReportService;
        _htmlParserService = htmlParserService;
        _messageFormatterService = messageFormatterService;
    }

    [HttpGet("today")]
    public async Task<IActionResult> GetTodaysReports()
    {
        var reports = await _dailyReportService.GetTodaysReports();
        var parsedReports = reports.Select(r => ToView(_htmlParserService.Parse(r))).ToList();
        return Ok(parsedReports);
    }

    [HttpGet("people")]
    public async Task<IActionResult> GetPeople([FromQuery] int? year = null, [FromQuery] int? month = null)
    {
        try
        {
            var people = await _dailyReportService.GetPeopleAsync(year, month);
            return Ok(new { people });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("months")]
    public async Task<IActionResult> GetMonths()
    {
        try
        {
            var months = await _dailyReportService.GetMonthsAsync();
            return Ok(new { months });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetReports(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromQuery] string employee = "")
    {
        try
        {
            if (year < 2000 || year > 2100 || month is < 1 or > 12)
                return BadRequest(new { message = "Provide a valid year and month." });
            if (string.IsNullOrWhiteSpace(employee))
                return BadRequest(new { message = "Select a person." });

            var reports = await _dailyReportService.GetReportsAsync(year, month, employee);
            var parsed = reports.Select(r => ToView(_htmlParserService.Parse(r))).ToList();
            return Ok(new
            {
                year,
                month,
                employee = employee.Trim(),
                count = parsed.Count,
                reports = parsed,
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("message")]
    public async Task<IActionResult> GetFormattedMessage()
    {
        var reports = await _dailyReportService.GetTodaysReports();
        var parsedReport = reports.Select(r => _htmlParserService.Parse(r)).FirstOrDefault();
        if (parsedReport == null)
            return NotFound("No reports found.");

        var message = _messageFormatterService.Format(parsedReport);
        return Ok(message);
    }

    private static object ToView(Models.DailyReportModel report) => new
    {
        employeeName = report.EmployeeName,
        department = report.Department,
        submittedOn = report.SubmittedOn,
        submittedForDate = report.SubmittedForDate,
        firstHalf = report.FirstHalf,
        secondHalf = report.SecondHalf,
        tomorrowTasks = report.TomorrowTasks,
    };
}
