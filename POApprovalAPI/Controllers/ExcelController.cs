using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Models;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExcelController : ControllerBase
{
    private readonly ExcelLedgerService _excel;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ExcelController(ExcelLedgerService excel)
    {
        _excel = excel;
    }

    [HttpPost("preview")]
    [RequestSizeLimit(30_000_000)]
    public async Task<IActionResult> Preview(
        IFormFile file,
        [FromForm] string? sheetName = null,
        [FromForm] int? headerRow = null)
    {
        try
        {
            ValidateExcel(file);
            await using var stream = file.OpenReadStream();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            ms.Position = 0;

            var preview = _excel.Preview(ms, file.FileName, sheetName, headerRow);
            return Ok(preview);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("compare")]
    [RequestSizeLimit(60_000_000)]
    public async Task<IActionResult> Compare(
        IFormFile fileA,
        IFormFile fileB,
        [FromForm] string mappingA,
        [FromForm] string mappingB,
        [FromForm] string? options = null)
    {
        try
        {
            ValidateExcel(fileA);
            ValidateExcel(fileB);

            var mapA = DeserializeOrThrow<LedgerColumnMapping>(mappingA, "mappingA");
            var mapB = DeserializeOrThrow<LedgerColumnMapping>(mappingB, "mappingB");
            var matchOptions = string.IsNullOrWhiteSpace(options)
                ? new LedgerMatchOptions()
                : DeserializeOrThrow<LedgerMatchOptions>(options, "options");

            await using var streamA = fileA.OpenReadStream();
            await using var streamB = fileB.OpenReadStream();
            using var msA = new MemoryStream();
            using var msB = new MemoryStream();
            await streamA.CopyToAsync(msA);
            await streamB.CopyToAsync(msB);
            msA.Position = 0;
            msB.Position = 0;

            var result = _excel.Compare(msA, fileA.FileName, msB, fileB.FileName, mapA, mapB, matchOptions);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Export the current comparison result (including any UI/manual edits) as Excel.
    /// </summary>
    [HttpPost("export")]
    [RequestSizeLimit(30_000_000)]
    public IActionResult Export([FromBody] ComparisonResultDto result)
    {
        try
        {
            if (result == null || result.Results == null)
                throw new InvalidOperationException("Comparison result is required.");

            // Keep summary in sync with whatever the client currently shows.
            result.Summary ??= new ComparisonSummary();
            result.Summary.Matched = result.Results.Count(r => r.Status == "Matched");
            result.Summary.AmountMismatch = result.Results.Count(r => r.Status == "AmountMismatch");
            result.Summary.MissingInA = result.Results.Count(r => r.Status == "MissingInA");
            result.Summary.MissingInB = result.Results.Count(r => r.Status == "MissingInB");
            result.Summary.Duplicates = result.Results.Count(r => r.Status == "Duplicate");
            result.Summary.PotentialMatches = result.Results.Count(r => r.Status == "PotentialMatch");
            result.Summary.PendingRecords = result.Results.Count(r => r.Status == "PendingRecord");

            var bytes = _excel.BuildExport(result);
            var fileName = $"ledger-reconciliation-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static void ValidateExcel(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            throw new InvalidOperationException("Excel file is required.");

        if (file.Length > 25_000_000)
            throw new InvalidOperationException("File exceeds the 25 MB limit.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not ".xlsx")
            throw new InvalidOperationException("Only .xlsx files are supported.");
    }

    private static T DeserializeOrThrow<T>(string json, string fieldName)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions)
                   ?? throw new InvalidOperationException($"Invalid {fieldName} payload.");
        }
        catch (JsonException)
        {
            throw new InvalidOperationException($"Invalid {fieldName} JSON.");
        }
    }
}
