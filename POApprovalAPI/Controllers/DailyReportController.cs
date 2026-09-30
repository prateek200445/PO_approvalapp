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
    private readonly DailyReportDigestService _digest;

    public DailyReportController(
        DailyReportService dailyReportService,
        HtmlParserService htmlParserService,
        MessageFormatterService messageFormatterService,
        DailyReportDigestService digest)
    {
        _dailyReportService = dailyReportService;
        _htmlParserService = htmlParserService;
        _messageFormatterService = messageFormatterService;
        _digest = digest;
    }

    /// <summary>Combined PDF of all reports submitted on the date before the send time (default 7:00 PM).</summary>
    [HttpGet("digest/pdf")]
    public async Task<IActionResult> DigestPdf([FromQuery] string? date = null)
    {
        try
        {
            var day = ParseDigestDate(date);
            var (pdf, _, _) = await _digest.BuildPdfAsync(day);
            return File(pdf, "application/pdf", DailyReportDigestService.FileNameFor(day));
        }
        catch (FormatException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>Public link WhatsApp downloads the sent PDF from; the random token is the access key.</summary>
    [HttpGet("digest/file/{token}/{fileName?}")]
    public async Task<IActionResult> DigestFile(string token, string? fileName = null)
    {
        var file = await _digest.GetPdfByTokenAsync(token);
        if (file is null)
            return NotFound();
        return File(file.Value.Pdf, "application/pdf", DailyReportDigestService.FileNameFor(file.Value.Date));
    }

    [HttpGet("digest/status")]
    public async Task<IActionResult> DigestStatus([FromQuery] string? date = null)
    {
        try
        {
            var day = ParseDigestDate(date);
            var status = await _digest.GetStatusAsync(day);
            return Ok(new
            {
                date = day.ToString("yyyy-MM-dd"),
                sendTime = _digest.SendTime.ToString(@"hh\:mm"),
                recipients = _digest.Recipients,
                reportCount = status?.ReportCount,
                sentAt = status?.SentAt,
                sentTo = status?.SentTo,
                lastError = status?.LastError,
                triggeredBy = status?.TriggeredBy,
            });
        }
        catch (FormatException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("digest/send")]
    public async Task<IActionResult> DigestSend(
        [FromQuery] string? date = null,
        [FromQuery] string username = "",
        [FromQuery] bool force = true)
    {
        try
        {
            if (!_digest.IsAdmin(username))
                return StatusCode(403, new { message = "You are not allowed to send the daily reports PDF." });
            var day = ParseDigestDate(date);
            var status = await _digest.SendAsync(day, force, username);
            if (status.SentAt is null)
                return BadRequest(new { message = status.LastError ?? "WhatsApp send failed." });
            return Ok(new { sentAt = status.SentAt, sentTo = status.SentTo, reportCount = status.ReportCount, lastError = status.LastError });
        }
        catch (FormatException ex)
        {
            return BadRequest(new { message = ex.Message });
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

    private DateTime ParseDigestDate(string? date)
    {
        if (string.IsNullOrWhiteSpace(date))
            return _digest.LocalNow.Date;
        if (!DateTime.TryParseExact(date.Trim(), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var day))
            throw new FormatException("date must be yyyy-MM-dd.");
        return day.Date;
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
