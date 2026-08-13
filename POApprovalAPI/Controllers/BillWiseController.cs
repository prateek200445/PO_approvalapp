using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Models;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/bill-wise")]
public class BillWiseController : ControllerBase
{
    private readonly BillWiseTransactionService _billWise;

    public BillWiseController(BillWiseTransactionService billWise)
    {
        _billWise = billWise;
    }

    /// <summary>
    /// Typeahead company search (FactoryInfo). Pass q with at least 1 character.
    /// </summary>
    [HttpGet("companies")]
    public async Task<IActionResult> SearchCompanies([FromQuery] string? q = null, [FromQuery] int take = 40)
    {
        try
        {
            var companies = await _billWise.SearchCompaniesAsync(q, take);
            return Ok(companies);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Distinct ledger names for a company (cached per company).</summary>
    [HttpGet("ledgers")]
    public async Task<IActionResult> GetLedgers([FromQuery] string company)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(company))
                return BadRequest(new { message = "company query parameter is required." });

            var ledgers = await _billWise.GetLedgersAsync(company);
            return Ok(ledgers);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Compare two companies — ledgers are auto-matched. Optional ledgerA/ledgerB override.
    /// </summary>
    [HttpPost("compare")]
    public async Task<IActionResult> Compare([FromBody] BillWiseCompareRequest request)
    {
        try
        {
            if (request == null)
                throw new InvalidOperationException("Request body is required.");

            var options = request.Options ?? new LedgerMatchOptions();
            ComparisonResultDto result;

            if (string.IsNullOrWhiteSpace(request.LedgerA) || string.IsNullOrWhiteSpace(request.LedgerB))
            {
                result = await _billWise.CompareFromCompaniesAsync(
                    request.CompanyA,
                    request.CompanyB,
                    options);
            }
            else
            {
                result = await _billWise.CompareFromSelectionAsync(
                    request.CompanyA,
                    request.LedgerA,
                    request.CompanyB,
                    request.LedgerB,
                    options);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public class BillWiseCompareRequest
{
    public string CompanyA { get; set; } = "";
    public string? LedgerA { get; set; }
    public string CompanyB { get; set; } = "";
    public string? LedgerB { get; set; }
    public LedgerMatchOptions? Options { get; set; }
}
