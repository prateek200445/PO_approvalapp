using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/BankRequirements")]
public class BankRequirementsController : ControllerBase
{
    private readonly BankRequirementsService _service;
    private readonly BankDocumentPackService _pack;

    public BankRequirementsController(BankRequirementsService service, BankDocumentPackService pack)
    {
        _service = service;
        _pack = pack;
    }

    [HttpGet("document-pack/defaults")]
    public IActionResult GetDocumentPackDefaults()
    {
        var o = BankDocumentPackService.DefaultOptions();
        return Ok(new
        {
            companies = BankDocumentPackService.Companies.Select(c => new { c.Code, c.DisplayName }),
            today = o.Today.ToString("yyyy-MM-dd"),
            ageingDates = o.AgeingDates.Select(d => d.ToString("yyyy-MM-dd")),
            stockFrom = o.StockFrom.ToString("yyyy-MM"),
            stockTo = o.StockTo.ToString("yyyy-MM"),
        });
    }

    [HttpGet("document-pack")]
    public async Task<IActionResult> GetDocumentPack(
        [FromQuery] string company,
        [FromQuery] string? ageingDates = null,
        [FromQuery] string? stockFrom = null,
        [FromQuery] string? stockTo = null,
        [FromQuery] bool refresh = false)
    {
        try
        {
            return Ok(await _pack.BuildAsync(company, PackOptions(ageingDates, stockFrom, stockTo), refresh));
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

    [HttpGet("document-pack/excel")]
    public async Task<IActionResult> ExportDocumentPack(
        [FromQuery] string company,
        [FromQuery] string? ageingDates = null,
        [FromQuery] string? stockFrom = null,
        [FromQuery] string? stockTo = null,
        [FromQuery] bool refresh = false)
    {
        try
        {
            var options = PackOptions(ageingDates, stockFrom, stockTo);
            var bytes = await _pack.BuildExcelAsync(company, options, refresh);
            var code = BankDocumentPackService.ResolveCompany(company).Code;
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{code}_Bank_Document_Pack_{options.Today:yyyy-MM-dd}.xlsx");
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

    private static BankPackOptions PackOptions(string? ageingDates, string? stockFrom, string? stockTo)
    {
        var o = BankDocumentPackService.DefaultOptions();
        var dates = (ageingDates ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => DateTime.TryParseExact(s, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var d) ? d : (DateTime?)null)
            .Where(d => d.HasValue && d.Value <= o.Today)
            .Select(d => d!.Value)
            .Distinct()
            .OrderBy(d => d)
            .Take(4)
            .ToList();
        if (dates.Count > 0) o.AgeingDates = dates;
        if (TryMonth(stockFrom, out var sf)) o.StockFrom = sf;
        if (TryMonth(stockTo, out var st)) o.StockTo = st;
        if (o.StockTo < o.StockFrom || (o.StockTo.Year - o.StockFrom.Year) * 12 + o.StockTo.Month - o.StockFrom.Month > 23)
            throw new ArgumentException("Stock statement range must be 1 to 24 months.");
        return o;

        static bool TryMonth(string? s, out DateTime month) =>
            DateTime.TryParseExact(s, "yyyy-MM", null, System.Globalization.DateTimeStyles.None, out month);
    }

    [HttpGet("sales-profile")]
    public async Task<IActionResult> GetSalesProfile(
        [FromQuery] string company = "All Companies",
        [FromQuery] string? months = null,
        [FromQuery] bool refresh = false)
    {
        try
        {
            var monthList = BankRequirementsService.NormalizeMonths(months == null ? [] : [months]);
            var data = await _service.GetSalesProfileAsync(company, monthList, refresh);
            return Ok(data);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("sales-profile/excel")]
    public async Task<IActionResult> ExportExcel(
        [FromQuery] string company = "All Companies",
        [FromQuery] string? months = null,
        [FromQuery] bool refresh = false)
    {
        try
        {
            var monthList = BankRequirementsService.NormalizeMonths(months == null ? [] : [months]);
            var bytes = await _service.BuildExcelAsync(company, monthList, refresh);
            var data = await _service.GetSalesProfileAsync(company, monthList, false);
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"profile-of-sales-{data.PeriodLabel}.xlsx");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("sales-profile/pdf")]
    public async Task<IActionResult> ExportPdf(
        [FromQuery] string company = "All Companies",
        [FromQuery] string? months = null,
        [FromQuery] bool refresh = false)
    {
        try
        {
            var monthList = BankRequirementsService.NormalizeMonths(months == null ? [] : [months]);
            var bytes = await _service.BuildPdfAsync(company, monthList, refresh);
            var data = await _service.GetSalesProfileAsync(company, monthList, false);
            return File(bytes, "application/pdf", $"profile-of-sales-{data.PeriodLabel}.pdf");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
