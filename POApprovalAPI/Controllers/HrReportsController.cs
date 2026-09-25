using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/hr/reports")]
public class HrReportsController : ControllerBase
{
    private readonly HrReportsService _service;
    private readonly HrSelfServiceService _selfService;
    private readonly HrAccessService _access;

    public HrReportsController(
        HrReportsService service,
        HrSelfServiceService selfService,
        HrAccessService access)
    {
        _service = service;
        _selfService = selfService;
        _access = access;
    }

    [HttpGet("access")]
    public async Task<IActionResult> Access([FromQuery] string username = "")
    {
        try
        {
            return Ok(await _access.ResolveAsync(username));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("companies")]
    public async Task<IActionResult> Companies([FromQuery] string username = "")
    {
        try
        {
            var access = await _access.ResolveAsync(username);
            if (access.Mode == "none")
                return StatusCode(403, new { message = access.Message });
            if (!access.HasFullAccess)
                return Ok(Array.Empty<string>());
            return Ok(await _service.GetCompaniesAsync());
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("branches")]
    public async Task<IActionResult> Branches([FromQuery] string? company, [FromQuery] string username = "")
    {
        try
        {
            var access = await _access.ResolveAsync(username);
            if (access.Mode == "none")
                return StatusCode(403, new { message = access.Message });
            if (!access.HasFullAccess)
                return Ok(Array.Empty<string>());
            return Ok(await _service.GetBranchesAsync(company));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("employees")]
    public async Task<IActionResult> SearchEmployees(
        [FromQuery] string username = "",
        [FromQuery] string? q = null,
        [FromQuery] string? company = null,
        [FromQuery] string? branch = null,
        [FromQuery] bool officeOnly = false,
        [FromQuery] int take = 200)
    {
        try
        {
            var access = await _access.ResolveAsync(username);
            if (access.Mode == "none")
                return StatusCode(403, new { message = access.Message });

            if (!access.HasFullAccess)
            {
                // Self users only see themselves
                var rows = await _service.SearchEmployeesAsync(access.EmpCode, null, null, false, 5);
                var self = rows
                    .Where(r => string.Equals(r.EmpCode, access.EmpCode, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                return Ok(self);
            }

            var all = await _service.SearchEmployeesAsync(q, company, branch, officeOnly, take);
            return Ok(all);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("attendance")]
    public async Task<IActionResult> Attendance(
        [FromQuery] string empCode,
        [FromQuery] string yearMonth,
        [FromQuery] string username = "",
        [FromQuery] bool applyHalfDayRule = true)
    {
        try
        {
            await _access.EnsureCanAccessEmployeeAsync(username, empCode);
            var report = await _service.GetAttendanceReportAsync(empCode, yearMonth, applyHalfDayRule);
            return Ok(report);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("salary")]
    public async Task<IActionResult> Salary(
        [FromQuery] string empCode,
        [FromQuery] string yearMonth,
        [FromQuery] string username = "",
        [FromQuery] decimal? monthlyBasic = null,
        [FromQuery] decimal? dailyRate = null,
        [FromQuery] int? workingDays = null,
        [FromQuery] bool applyHalfDayRule = true)
    {
        try
        {
            await _access.EnsureCanAccessEmployeeAsync(username, empCode);
            var report = await _service.GetSalaryReportAsync(
                empCode,
                yearMonth,
                monthlyBasic,
                dailyRate,
                workingDays,
                applyHalfDayRule);
            return Ok(report);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("attendance/excel")]
    public async Task<IActionResult> AttendanceExcel(
        [FromQuery] string empCode,
        [FromQuery] string yearMonth,
        [FromQuery] string username = "",
        [FromQuery] bool applyHalfDayRule = true)
    {
        try
        {
            await _access.EnsureCanAccessEmployeeAsync(username, empCode);
            var bytes = await _service.BuildAttendanceExcelAsync(empCode, yearMonth, applyHalfDayRule);
            var name = $"attendance-{empCode}-{yearMonth}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", name);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("salary/excel")]
    public async Task<IActionResult> SalaryExcel(
        [FromQuery] string empCode,
        [FromQuery] string yearMonth,
        [FromQuery] string username = "",
        [FromQuery] decimal? monthlyBasic = null,
        [FromQuery] decimal? dailyRate = null,
        [FromQuery] int? workingDays = null,
        [FromQuery] bool applyHalfDayRule = true)
    {
        try
        {
            await _access.EnsureCanAccessEmployeeAsync(username, empCode);
            var bytes = await _service.BuildSalaryExcelAsync(
                empCode,
                yearMonth,
                monthlyBasic,
                dailyRate,
                workingDays,
                applyHalfDayRule);
            var name = $"salary-{empCode}-{yearMonth}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", name);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("leave")]
    public async Task<IActionResult> LeaveList(
        [FromQuery] string empCode,
        [FromQuery] string username = "",
        [FromQuery] int take = 50)
    {
        try
        {
            await _access.EnsureCanAccessEmployeeAsync(username, empCode);
            return Ok(await _selfService.GetLeaveApplicationsAsync(empCode, take));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("leave-eligibility")]
    public async Task<IActionResult> LeaveEligibility(
        [FromQuery] string empCode,
        [FromQuery] string username = "")
    {
        try
        {
            await _access.EnsureCanAccessEmployeeAsync(username, empCode);
            return Ok(await _selfService.GetLeaveEligibilityAsync(empCode));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("leave")]
    public async Task<IActionResult> LeaveApply([FromBody] HrLeaveApplyRequest request, [FromQuery] string username = "")
    {
        try
        {
            var user = FirstNonEmpty(username, request.Username);
            // Employee applies for self only. HR cannot apply on behalf of someone.
            await _access.EnsureCanApplyOwnLeaveAsync(user, request.EmpCode);
            return Ok(await _selfService.ApplyLeaveAsync(request));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("wfh")]
    public async Task<IActionResult> WfhApply([FromBody] HrLeaveApplyRequest request, [FromQuery] string username = "")
    {
        try
        {
            var user = FirstNonEmpty(username, request.Username);
            request.LeaveType = "WFH";
            await _access.EnsureCanApplyOwnLeaveAsync(user, request.EmpCode);
            return Ok(await _selfService.ApplyLeaveAsync(request));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("leave/pending")]
    public async Task<IActionResult> LeavePending([FromQuery] string username = "")
    {
        try
        {
            var access = await _access.ResolveAsync(username);
            if (!access.HasFullAccess)
                return StatusCode(403, new { message = "Only HR can view pending leave requests." });
            return Ok(await _selfService.ListPendingLeaveAsync());
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("leave/approve")]
    public async Task<IActionResult> LeaveApprove([FromBody] HrLeaveDecisionRequest request, [FromQuery] string username = "")
    {
        try
        {
            var user = FirstNonEmpty(username, request.Username);
            await _access.EnsureCanAccessEmployeeAsync(user, request.EmpCode, requireFullAccess: true);
            return Ok(await _selfService.DecideLeaveAsync(request, user, approve: true));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("leave/reject")]
    public async Task<IActionResult> LeaveReject([FromBody] HrLeaveDecisionRequest request, [FromQuery] string username = "")
    {
        try
        {
            var user = FirstNonEmpty(username, request.Username);
            await _access.EnsureCanAccessEmployeeAsync(user, request.EmpCode, requireFullAccess: true);
            return Ok(await _selfService.DecideLeaveAsync(request, user, approve: false));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("confirmation")]
    public async Task<IActionResult> Confirmation([FromBody] HrEmpActionRequest request, [FromQuery] string username = "")
    {
        try
        {
            var user = FirstNonEmpty(username, request.AppliedBy);
            await _access.EnsureCanAccessEmployeeAsync(user, request.EmpCode, requireFullAccess: true);
            return Ok(await _selfService.ApplyConfirmationAsync(request.EmpCode, user));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("leave-credit/preview")]
    public async Task<IActionResult> LeaveCreditPreview(
        [FromQuery] string empCode,
        [FromQuery] string username = "")
    {
        try
        {
            await _access.EnsureCanAccessEmployeeAsync(username, empCode, requireFullAccess: true);
            return Ok(await _selfService.PreviewHoMonthlyLeaveCreditAsync(empCode));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("leave-credit")]
    public async Task<IActionResult> LeaveCredit([FromBody] HrLeaveCreditRequest request, [FromQuery] string username = "")
    {
        try
        {
            await _access.EnsureCanAccessEmployeeAsync(username, request.EmpCode, requireFullAccess: true);
            return Ok(await _selfService.ApplyHoMonthlyLeaveCreditAsync(
                request.EmpCode,
                request.IncludeOneTimeGrant));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("attendance-ack")]
    public async Task<IActionResult> AttendanceAck(
        [FromQuery] string empCode,
        [FromQuery] string yearMonth,
        [FromQuery] string username = "")
    {
        try
        {
            await _access.EnsureCanAccessEmployeeAsync(username, empCode);
            return Ok(await _selfService.GetAttendanceAckAsync(empCode, yearMonth));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("attendance-ack/pending")]
    public async Task<IActionResult> AttendanceAckPending([FromQuery] string username = "")
    {
        try
        {
            var access = await _access.ResolveAsync(username);
            if (!access.HasFullAccess)
                return StatusCode(403, new { message = "Only HR can view pending month-end verify requests." });
            return Ok(await _selfService.ListPendingAttendanceVerifyAsync());
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Employee: send verify request to HR. Full HR (grouphr / plastenehr / prakash): approve month-end.
    /// Body.approve=true requires full access.
    /// </summary>
    [HttpPost("attendance-ack")]
    public async Task<IActionResult> AttendanceAckVerify(
        [FromBody] HrAttendanceAckRequest request,
        [FromQuery] string username = "")
    {
        try
        {
            var user = FirstNonEmpty(username, request.VerifiedBy);
            await _access.EnsureCanAccessEmployeeAsync(user, request.EmpCode);

            var access = await _access.ResolveAsync(user);
            if (request.Approve)
            {
                if (!access.HasFullAccess)
                    return StatusCode(403, new { message = "Only HR can approve month-end verification." });
                return Ok(await _selfService.ApproveAttendanceVerifyAsync(
                    request.EmpCode,
                    request.YearMonth,
                    user,
                    request.Note));
            }

            // Employees (and HR acting as request) send Pending request to HR.
            return Ok(await _selfService.RequestAttendanceVerifyAsync(
                request.EmpCode,
                request.YearMonth,
                user,
                request.Note));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var v in values)
        {
            if (!string.IsNullOrWhiteSpace(v))
                return v.Trim();
        }
        return "";
    }
}

public sealed class HrEmpActionRequest
{
    public string EmpCode { get; set; } = "";
    public string? AppliedBy { get; set; }
}

public sealed class HrLeaveCreditRequest
{
    public string EmpCode { get; set; } = "";
    public bool IncludeOneTimeGrant { get; set; }
}

public sealed class HrAttendanceAckRequest
{
    public string EmpCode { get; set; } = "";
    public string YearMonth { get; set; } = "";
    public string? VerifiedBy { get; set; }
    public string? Note { get; set; }
    /// <summary>When true, HR approves. When false/omitted, employee sends request to HR.</summary>
    public bool Approve { get; set; }
}
