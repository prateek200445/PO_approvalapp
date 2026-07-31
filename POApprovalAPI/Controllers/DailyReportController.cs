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

    var parsedReports = reports
        .Select(r => _htmlParserService.Parse(r))
        .ToList();

    return Ok(parsedReports);
}

    [HttpGet("message")]
    public async Task<IActionResult> GetFormattedMessage()
    {
        var reports = await _dailyReportService.GetTodaysReports();

        var parsedReport = reports
            .Select(r => _htmlParserService.Parse(r))
            .FirstOrDefault();

        if (parsedReport == null)
        {
            return NotFound("No reports found.");
        }

        var message = _messageFormatterService.Format(parsedReport);

        return Ok(message);
    }
}