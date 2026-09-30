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
    private readonly HrEmployeeMasterService _employeeMaster;
    private readonly HrEmployeeDocumentService _documents;
    private readonly HrAttendanceEditService _attendanceEdits;

    public HrReportsController(
        HrReportsService service,
        HrSelfServiceService selfService,
        HrAccessService access,
        HrEmployeeMasterService employeeMaster,
        HrEmployeeDocumentService documents,
        HrAttendanceEditService attendanceEdits)
    {
        _service = service;
        _selfService = selfService;
        _access = access;
        _employeeMaster = employeeMaster;
        _documents = documents;
        _attendanceEdits = attendanceEdits;
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
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
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
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
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
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
        }
    }

    [HttpGet("employees")]
    public async Task<IActionResult> SearchEmployees(
        [FromQuery] string username = "",
        [FromQuery] string? q = null,
        [FromQuery] string? company = null,
        [FromQuery] string? branch = null,
        [FromQuery] bool officeOnly = false,
        [FromQuery] int take = 200,
        [FromQuery] bool includeInactive = false)
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

            var all = await _service.SearchEmployeesAsync(q, company, branch, officeOnly, take, includeInactive);
            return Ok(all);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
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
            var access = await _access.EnsureCanAccessEmployeeAsync(username, empCode);
            if (!access.CanModifyAttendance) applyHalfDayRule = true;
            var report = await _service.GetAttendanceReportAsync(empCode, yearMonth, applyHalfDayRule);
            return Ok(report);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
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
            var access = await _access.EnsureCanAccessEmployeeAsync(username, empCode);
            if (!access.CanModifyAttendance)
                (monthlyBasic, dailyRate, workingDays, applyHalfDayRule) = (null, null, null, true);
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
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
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
            var access = await _access.EnsureCanAccessEmployeeAsync(username, empCode);
            if (!access.CanModifyAttendance) applyHalfDayRule = true;
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
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
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
            var access = await _access.EnsureCanAccessEmployeeAsync(username, empCode);
            if (!access.CanModifyAttendance)
                (monthlyBasic, dailyRate, workingDays, applyHalfDayRule) = (null, null, null, true);
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
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
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
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
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
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
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
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
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
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
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
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
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
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
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
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
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
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
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
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
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
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
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
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
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
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
        }
    }

    /// <summary>
    /// Employee: send verify request to HR. Attendance editors (grouphr / plastenehr): approve month-end.
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
                if (!access.CanModifyAttendance)
                    return StatusCode(403, new { message = "Only grouphr / plastenehr can approve month-end attendance." });
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
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
        }
    }

    [HttpPut("attendance/edit")]
    public async Task<IActionResult> SaveAttendanceEdit(
        [FromBody] HrAttendanceEditRequest request,
        [FromQuery] string username = "")
    {
        try
        {
            var access = await _access.EnsureCanAccessEmployeeAsync(username, request.EmpCode);
            if (!access.CanModifyAttendance)
                return StatusCode(403, new { message = "Only grouphr / plastenehr can edit attendance." });

            var date = (request.Date ?? "").Trim();
            if (date.Length < 7)
                return BadRequest(new { message = "Date must be yyyy-MM-dd." });
            var report = await _service.GetAttendanceReportAsync(request.EmpCode, date[..7], true);
            var day = report.Days.FirstOrDefault(d => d.Date == date);
            if (day is null)
                return BadRequest(new { message = "That date is outside the employee's attendance month." });

            return Ok(await _attendanceEdits.SaveAsync(request, day.MachineStatus, username));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
        }
    }

    [HttpDelete("attendance/edit")]
    public async Task<IActionResult> RevertAttendanceEdit(
        [FromQuery] string empCode,
        [FromQuery] string date,
        [FromQuery] string username = "")
    {
        try
        {
            var access = await _access.EnsureCanAccessEmployeeAsync(username, empCode);
            if (!access.CanModifyAttendance)
                return StatusCode(403, new { message = "Only grouphr / plastenehr can edit attendance." });
            var removed = await _attendanceEdits.RevertAsync(empCode, date, username);
            return Ok(new { reverted = removed });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
        }
    }

    [HttpGet("attendance/edit-history")]
    public async Task<IActionResult> AttendanceEditHistory(
        [FromQuery] string empCode,
        [FromQuery] string yearMonth,
        [FromQuery] string username = "")
    {
        try
        {
            var access = await _access.EnsureCanAccessEmployeeAsync(username, empCode);
            if (!access.HasFullAccess)
                return StatusCode(403, new { message = "Only HR can view attendance edit history." });
            if (!DateTime.TryParseExact(yearMonth, "yyyy-MM", System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var month))
                return BadRequest(new { message = "yearMonth must be yyyy-MM." });
            return Ok(await _attendanceEdits.GetHistoryAsync(empCode, month, month.AddMonths(1).AddDays(-1)));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
        }
    }

    [HttpGet("employee-form/options")]
    public async Task<IActionResult> EmployeeFormOptions([FromQuery] string username = "")
    {
        try
        {
            var access = await _access.ResolveAsync(username);
            if (!access.HasFullAccess)
                return StatusCode(403, new { message = "Only HR can add employees." });
            return Ok(await _employeeMaster.GetFormOptionsAsync());
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
        }
    }

    [HttpGet("employee-form/next-code")]
    public async Task<IActionResult> EmployeeNextCode([FromQuery] string branch, [FromQuery] string username = "")
    {
        try
        {
            var access = await _access.ResolveAsync(username);
            if (!access.HasFullAccess)
                return StatusCode(403, new { message = "Only HR can add employees." });
            return Ok(await _employeeMaster.SuggestEmpCodeAsync(branch));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
        }
    }

    [HttpPost("employees")]
    public async Task<IActionResult> CreateEmployee([FromBody] HrCreateEmployeeRequest request, [FromQuery] string username = "")
    {
        try
        {
            var user = FirstNonEmpty(username, request.Username);
            var access = await _access.ResolveAsync(user);
            if (!access.HasFullAccess)
                return StatusCode(403, new { message = "Only HR can add employees." });
            return Ok(await _employeeMaster.CreateEmployeeAsync(request, user));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
        }
    }

    [HttpGet("employee-form/document-types")]
    public IActionResult EmployeeDocumentTypes() => Ok(HrEmployeeDocumentService.DocumentTypes);

    [HttpGet("employees/{empCode}/photo")]
    public async Task<IActionResult> EmployeePhoto(string empCode, [FromQuery] string username = "")
    {
        try
        {
            var access = await _access.ResolveAsync(username);
            if (!access.HasFullAccess)
                return StatusCode(403, new { message = "Only HR can view employee documents." });
            var photo = await _documents.GetPhotoAsync(empCode);
            return photo is null ? NotFound(new { message = "No photo on file." }) : File(photo.Value.Content, photo.Value.ContentType);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
        }
    }

    [HttpPost("employees/{empCode}/photo")]
    [RequestSizeLimit(HrEmployeeDocumentService.MaxPhotoBytes + 64 * 1024)]
    public async Task<IActionResult> UploadEmployeePhoto(string empCode, IFormFile? file, [FromQuery] string username = "")
    {
        try
        {
            var access = await _access.ResolveAsync(username);
            if (!access.HasFullAccess)
                return StatusCode(403, new { message = "Only HR can upload employee documents." });
            if (file is null || file.Length == 0)
                return BadRequest(new { message = "Choose a photo to upload." });
            var message = await _documents.SavePhotoAsync(empCode, await ReadAllAsync(file));
            return Ok(new { message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
        }
    }

    [HttpGet("employees/{empCode}/documents")]
    public async Task<IActionResult> EmployeeDocuments(string empCode, [FromQuery] string username = "")
    {
        try
        {
            var access = await _access.ResolveAsync(username);
            if (!access.HasFullAccess)
                return StatusCode(403, new { message = "Only HR can view employee documents." });
            return Ok(await _documents.ListDocumentsAsync(empCode));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
        }
    }

    [HttpPost("employees/{empCode}/documents")]
    [RequestSizeLimit(HrEmployeeDocumentService.MaxDocumentBytes + 64 * 1024)]
    public async Task<IActionResult> UploadEmployeeDocument(
        string empCode,
        IFormFile? file,
        [FromForm] string docType,
        [FromQuery] string username = "")
    {
        try
        {
            var access = await _access.ResolveAsync(username);
            if (!access.HasFullAccess)
                return StatusCode(403, new { message = "Only HR can upload employee documents." });
            if (file is null || file.Length == 0)
                return BadRequest(new { message = "Choose a file to upload." });
            var saved = await _documents.SaveDocumentAsync(
                empCode, docType, file.FileName, await ReadAllAsync(file), username.Trim());
            return Ok(saved);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
        }
    }

    [HttpGet("employees/{empCode}/documents/{docId:int}/file")]
    public async Task<IActionResult> EmployeeDocumentFile(string empCode, int docId, [FromQuery] string username = "")
    {
        try
        {
            var access = await _access.ResolveAsync(username);
            if (!access.HasFullAccess)
                return StatusCode(403, new { message = "Only HR can view employee documents." });
            var doc = await _documents.GetDocumentAsync(empCode, docId);
            return doc is null
                ? NotFound(new { message = "Document not found." })
                : File(doc.Value.Content, doc.Value.ContentType, doc.Value.FileName);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
        }
    }

    [HttpDelete("employees/{empCode}/documents/{docId:int}")]
    public async Task<IActionResult> DeleteEmployeeDocument(string empCode, int docId, [FromQuery] string username = "")
    {
        try
        {
            var access = await _access.ResolveAsync(username);
            if (!access.HasFullAccess)
                return StatusCode(403, new { message = "Only HR can delete employee documents." });
            return await _documents.DeleteDocumentAsync(empCode, docId, username.Trim())
                ? Ok(new { message = "Document removed." })
                : NotFound(new { message = "Document not found." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = PayrollSqlErrors.UserMessage(ex) });
        }
    }

    private static async Task<byte[]> ReadAllAsync(IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        using var buffer = new MemoryStream((int)Math.Min(file.Length, int.MaxValue));
        await stream.CopyToAsync(buffer);
        return buffer.ToArray();
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
