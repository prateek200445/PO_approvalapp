using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Documents;
using POApprovalAPI.Services;
using QuestPDF.Fluent;

namespace POApprovalAPI.Controllers;

/// <summary>Salary slips: HR (grouphr / plastenehr / prakash) for anyone; everyone else only their own.</summary>
[ApiController]
[Route("api/hr/payslip")]
public class HrPayslipController : ControllerBase
{
    private readonly HrPayslipService _payslips;
    private readonly HrAccessService _access;

    public HrPayslipController(HrPayslipService payslips, HrAccessService access)
    {
        _payslips = payslips;
        _access = access;
    }

    [HttpGet("months")]
    public async Task<IActionResult> Months([FromQuery] string empCode, [FromQuery] string username = "")
    {
        try
        {
            await EnsureAccessAsync(username, empCode);
            return Ok(await _payslips.GetMonthsAsync(empCode));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ForSlip(ex.Message) });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string empCode, [FromQuery] string yearMonth, [FromQuery] string username = "")
    {
        try
        {
            await EnsureAccessAsync(username, empCode);
            return Ok(await _payslips.GetPayslipAsync(empCode, yearMonth));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ForSlip(ex.Message) });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
        }
    }

    [HttpGet("pdf")]
    public async Task<IActionResult> Pdf([FromQuery] string empCode, [FromQuery] string yearMonth, [FromQuery] string username = "")
    {
        try
        {
            await EnsureAccessAsync(username, empCode);
            var slip = await _payslips.GetPayslipAsync(empCode, yearMonth);
            var photo = await _payslips.GetPhotoBytesAsync(empCode);
            var bytes = new HrPayslipPdfDocument(slip, photo).GeneratePdf();
            return File(bytes, "application/pdf", $"SalarySlip_{slip.Employee.EmpCode}_{slip.YearMonth}.pdf");
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ForSlip(ex.Message) });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
        }
    }

    private Task<HrAccessDto> EnsureAccessAsync(string username, string empCode)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new UnauthorizedAccessException("Please log in to view salary slips.");
        return _access.EnsureCanAccessEmployeeAsync(username, empCode);
    }

    private static string ForSlip(string message) =>
        message.Contains("your own", StringComparison.OrdinalIgnoreCase)
            ? "You can only view your own salary slip."
            : message;
}
