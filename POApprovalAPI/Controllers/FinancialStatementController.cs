using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Models;
using POApprovalAPI.Services.FinancialStatements;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/financial-statements")]
public class FinancialStatementController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly FinancialStatementService _service;

    public FinancialStatementController(FinancialStatementService service)
    {
        _service = service;
    }

    [HttpGet("companies")]
    public IActionResult ListCompanies()
    {
        try
        {
            return Ok(_service.ListCompanies());
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("schedule-groups")]
    public IActionResult GetScheduleGroups()
    {
        try
        {
            return Ok(_service.GetScheduleGroups());
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("mappings/{companyKey}")]
    public IActionResult GetMappings(string companyKey)
    {
        try
        {
            return Ok(_service.GetMappings(companyKey));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("mappings")]
    public IActionResult SaveMappings([FromBody] SaveMappingRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.CompanyKey))
                return BadRequest(new { message = "Company key is required." });

            _service.SaveMappings(request);
            return Ok(new { message = "Mapping saved.", companyKey = request.CompanyKey.Trim() });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
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

            var preview = _service.Preview(ms, file.FileName, sheetName, headerRow);
            return Ok(preview);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("generate")]
    [RequestSizeLimit(30_000_000)]
    public async Task<IActionResult> Generate(
        IFormFile file,
        [FromForm] string requestJson)
    {
        try
        {
            ValidateExcel(file);
            var request = JsonSerializer.Deserialize<GenerateFinancialStatementRequest>(requestJson, JsonOptions)
                ?? throw new InvalidOperationException("Invalid request payload.");

            if (request.Mapping == null || string.IsNullOrWhiteSpace(request.Mapping.Particulars))
                throw new InvalidOperationException("Trial balance column mapping is required.");

            await using var stream = file.OpenReadStream();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            ms.Position = 0;

            var result = _service.GenerateFromStream(ms, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("export")]
    public IActionResult Export([FromBody] FinancialStatementResultDto result)
    {
        try
        {
            var bytes = _service.ExportExcel(result);
            var fileName = $"financial-statements-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static void ValidateExcel(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new InvalidOperationException("Excel file is required.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not ".xlsx" and not ".xls")
            throw new InvalidOperationException("Only .xlsx or .xls files are supported.");
    }
}
