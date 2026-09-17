using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/bank-statement")]
public class BankStatementController : ControllerBase
{
    private readonly BankStatementService _service;

    public BankStatementController(BankStatementService service)
    {
        _service = service;
    }

    [HttpGet("template")]
    public IActionResult Template()
    {
        var bytes = _service.ExportCsv(Array.Empty<BankStatementRowDto>());
        return File(bytes, "text/csv; charset=utf-8", "BANKIMPORTFORMATE.csv");
    }

    /// <summary>Parse a file already in BANKIMPORTFORMATE.csv layout.</summary>
    [HttpPost("parse")]
    [RequestSizeLimit(30_000_000)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Parse(IFormFile file, CancellationToken ct)
    {
        try
        {
            ValidateUpload(file, allowPdf: false, allowExcel: false);
            await using var stream = file.OpenReadStream();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, ct);
            ms.Position = 0;
            var result = _service.Parse(ms, file.FileName);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Convert a raw bank statement (BOB PDF / bank CSV / Excel) into BANKIMPORTFORMATE rows.
    /// </summary>
    [HttpPost("convert")]
    [RequestSizeLimit(40_000_000)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Convert(
        IFormFile file,
        [FromForm] string? erpBankLedgerName,
        CancellationToken ct)
    {
        try
        {
            ValidateUpload(file, allowPdf: true, allowExcel: true);
            await using var stream = file.OpenReadStream();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, ct);
            ms.Position = 0;
            var result = _service.ConvertFromBankFile(ms, file.FileName, erpBankLedgerName);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("export")]
    public IActionResult Export([FromBody] BankStatementExportRequest request)
    {
        try
        {
            var rows = request.Rows ?? [];
            var bytes = _service.ExportCsv(rows, request.IncludeCategorization);
            var name = string.IsNullOrWhiteSpace(request.FileName)
                ? $"bank-import-{DateTime.Now:ddMMyyyy}.csv"
                : request.FileName!.Trim();
            if (!name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                name += ".csv";
            return File(bytes, "text/csv; charset=utf-8", name);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static void ValidateUpload(IFormFile? file, bool allowPdf, bool allowExcel)
    {
        if (file == null || file.Length == 0)
            throw new InvalidOperationException("File is required.");

        if (file.Length > 35_000_000)
            throw new InvalidOperationException("File exceeds the 35 MB limit.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var ok = ext is ".csv" or ".txt"
                 || (allowPdf && ext == ".pdf")
                 || (allowExcel && ext is ".xlsx" or ".xlsm");
        if (!ok)
        {
            var kinds = "CSV"
                        + (allowPdf ? ", PDF" : "")
                        + (allowExcel ? ", Excel (.xlsx)" : "");
            throw new InvalidOperationException($"Unsupported file type. Allowed: {kinds}.");
        }
    }
}
