using System.Globalization;
using Dapper;
using Microsoft.Data.SqlClient;

namespace POApprovalAPI.Services;

/// <summary>
/// Self-service leave / WFH / confirmation / month-end attendance verify for HR Reports.
/// Writes into payroll Loginentry (LeaveHistory, availableleave, EmployeeConfirmation)
/// and app DB (HrAttendanceMonthAck). Existing ERP leave applications remain visible.
/// </summary>
public sealed class HrSelfServiceService
{
    public const int LeaveApplyMaxAheadDays = 3;
    public const decimal HoPlGrantAfterOneYear = 18m;
    public const decimal HoPlMonthlyCredit = 1.5m;
    public const decimal ClCreditOnConfirmation = 6m;

    private readonly DatabaseService _database;

    public HrSelfServiceService(DatabaseService database)
    {
        _database = database;
    }

    public async Task<IReadOnlyList<HrLeaveApplicationDto>> GetLeaveApplicationsAsync(string empCode, int take = 50)
    {
        empCode = RequireEmpCode(empCode);
        take = Math.Clamp(take, 1, 200);
        using var connection = _database.CreatePayrollLoginEntryConnection();

        var elig = await GetLeaveEligibilityAsync(empCode);
        var rows = (await connection.QueryAsync<HrLeaveApplicationDto>(@"
SELECT TOP (@Take)
    LTRIM(RTRIM(EmpCode)) AS EmpCode,
    LTRIM(RTRIM(TypeofLeave)) AS LeaveType,
    ISNULL(Days, 0) AS Days,
    CONVERT(varchar(10), FromDate, 23) AS FromDate,
    CONVERT(varchar(10), ISNULL(FromTo, FromDate), 23) AS ToDate,
    LTRIM(RTRIM(Purpose)) AS Purpose,
    LTRIM(RTRIM(status_leave)) AS Status,
    LTRIM(RTRIM(approvedby)) AS ApprovedBy,
    CONVERT(varchar(19), Currentdte, 120) AS AppliedAt
FROM LeaveHistory WITH (NOLOCK)
WHERE LTRIM(RTRIM(EmpCode)) = @EmpCode
ORDER BY FromDate DESC, Currentdte DESC",
            new { EmpCode = empCode, Take = take },
            commandTimeout: 60)).ToList();

        if (!elig.CanApplyPlCl)
        {
            rows = rows
                .Where(r =>
                {
                    var t = (r.LeaveType ?? "").Trim().ToUpperInvariant();
                    return t is not ("PL" or "CL");
                })
                .ToList();
        }

        return rows;
    }

    public async Task<HrLeaveEligibilityDto> GetLeaveEligibilityAsync(string empCode)
    {
        empCode = RequireEmpCode(empCode);
        using var connection = _database.CreatePayrollLoginEntryConnection();
        var doj = await connection.ExecuteScalarAsync<DateTime?>(@"
SELECT DateOJ FROM empinfo WITH (NOLOCK)
WHERE LTRIM(RTRIM(EmpCode)) = @EmpCode", new { EmpCode = empCode });
        if (doj is null)
            throw new InvalidOperationException($"Employee {empCode} not found.");

        var months = MonthsOfService(doj.Value, DateTime.Today);
        var completed = months >= 12;
        return new HrLeaveEligibilityDto
        {
            EmpCode = empCode,
            DateOfJoining = doj.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            MonthsOfService = months,
            CompletedOneYear = completed,
            CanApplyPlCl = completed,
            Message = completed
                ? "PL / CL available (1 year of service completed)."
                : $"PL / CL not available until 1 year of joining (now {months} month(s)). You can apply LWP / WFH.",
        };
    }

    public async Task<HrLeaveApplyResultDto> ApplyLeaveAsync(HrLeaveApplyRequest request)
    {
        var empCode = RequireEmpCode(request.EmpCode);
        var leaveType = NormalizeLeaveType(request.LeaveType);
        if (leaveType is not ("PL" or "CL" or "LWP" or "WFH"))
            throw new InvalidOperationException("Leave type must be PL, CL, LWP, or WFH.");

        if (!TryParseDate(request.FromDate, out var from) || !TryParseDate(request.ToDate ?? request.FromDate, out var to))
            throw new InvalidOperationException("fromDate / toDate must be yyyy-MM-dd.");

        if (to < from)
            throw new InvalidOperationException("toDate cannot be before fromDate.");

        var today = DateTime.Today;
        var maxDate = today.AddDays(LeaveApplyMaxAheadDays);
        if (from < today || from > maxDate)
            throw new InvalidOperationException(
                $"Leave start date must be between {today:yyyy-MM-dd} and {maxDate:yyyy-MM-dd} (next {LeaveApplyMaxAheadDays} days only).");

        var days = request.Days ?? ((decimal)(to - from).TotalDays + 1m);
        if (days <= 0)
            throw new InvalidOperationException("Days must be greater than zero.");

        // Half-day leave: allow 0.5 when same calendar day
        if (from.Date == to.Date && days > 1)
            days = 1m;

        using var connection = _database.CreatePayrollLoginEntryConnection();

        var doj = await connection.ExecuteScalarAsync<DateTime?>(@"
SELECT DateOJ FROM empinfo WITH (NOLOCK)
WHERE LTRIM(RTRIM(EmpCode)) = @EmpCode", new { EmpCode = empCode });
        if (doj is null)
            throw new InvalidOperationException($"Employee {empCode} not found.");

        if (leaveType is "PL" or "CL")
        {
            var months = MonthsOfService(doj.Value, today);
            if (months < 12)
                throw new InvalidOperationException(
                    $"PL / CL can be applied only after completing 1 year of joining (currently {months} month(s) from {doj.Value:yyyy-MM-dd}). Use LWP until then.");
        }

        // Overlap with existing non-rejected leave
        var overlap = await connection.ExecuteScalarAsync<int>(@"
SELECT CASE WHEN EXISTS (
  SELECT 1 FROM LeaveHistory WITH (NOLOCK)
  WHERE LTRIM(RTRIM(EmpCode)) = @EmpCode
    AND LOWER(LTRIM(RTRIM(ISNULL(status_leave,'')))) NOT IN ('rejected','cancelled','cancelled(after arroval)')
    AND FromDate <= @To
    AND ISNULL(FromTo, FromDate) >= @From
) THEN 1 ELSE 0 END", new { EmpCode = empCode, From = from, To = to });
        if (overlap == 1)
            throw new InvalidOperationException("Overlapping leave already exists for these dates.");

        if (leaveType is "PL" or "CL")
        {
            var bal = await connection.QueryFirstOrDefaultAsync<(decimal AvailPl, decimal AvailCl)>(@"
SELECT TOP 1 ISNULL(AvailPL,0) AS AvailPl, ISNULL(AvailCL,0) AS AvailCl
FROM availableleave WITH (NOLOCK)
WHERE LTRIM(RTRIM(Empcode)) = @EmpCode
ORDER BY FromDate DESC", new { EmpCode = empCode });
            if (leaveType == "PL" && bal.AvailPl < days)
                throw new InvalidOperationException($"Insufficient PL balance ({bal.AvailPl} available).");
            if (leaveType == "CL" && bal.AvailCl < days)
                throw new InvalidOperationException($"Insufficient CL balance ({bal.AvailCl} available).");
        }

        var purpose = Truncate((request.Purpose ?? "").Trim(), 50);
        if (purpose.Length == 0)
            purpose = leaveType == "WFH" ? "Work from home" : "Self leave apply";

        await connection.ExecuteAsync(@"
INSERT INTO LeaveHistory
    (EmpCode, Days, TypeofLeave, FromDate, FromTo, Purpose, ContactNo, address, telno, status_leave, approvedby, Currentdte)
VALUES
    (@EmpCode, @Days, @TypeofLeave, @FromDate, @FromTo, @Purpose, NULL, NULL, NULL, 'Pending', NULL, GETDATE())",
            new
            {
                EmpCode = empCode,
                Days = (double)days,
                TypeofLeave = leaveType,
                FromDate = from,
                FromTo = to,
                Purpose = purpose,
            },
            commandTimeout: 30);

        return new HrLeaveApplyResultDto
        {
            EmpCode = empCode,
            LeaveType = leaveType,
            FromDate = from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ToDate = to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Days = days,
            Status = "Pending",
            Message =
                leaveType == "WFH"
                    ? "WFH submitted (Pending). HR must approve before it reflects on attendance."
                    : "Leave submitted (Pending). HR must approve before it reflects on attendance / balances.",
        };
    }

    public async Task<IReadOnlyList<HrLeaveApplicationDto>> ListPendingLeaveAsync(int take = 200)
    {
        take = Math.Clamp(take, 1, 500);
        using var connection = _database.CreatePayrollLoginEntryConnection();
        // Hide historic ERP pending rows (2010–etc.). Only show new portal-era requests:
        // leave starting from today, or applied in the last 14 days.
        var rows = (await connection.QueryAsync<HrLeaveApplicationDto>(@"
SELECT TOP (@Take)
    LTRIM(RTRIM(EmpCode)) AS EmpCode,
    LTRIM(RTRIM(TypeofLeave)) AS LeaveType,
    ISNULL(Days, 0) AS Days,
    CONVERT(varchar(10), FromDate, 23) AS FromDate,
    CONVERT(varchar(10), ISNULL(FromTo, FromDate), 23) AS ToDate,
    LTRIM(RTRIM(Purpose)) AS Purpose,
    LTRIM(RTRIM(status_leave)) AS Status,
    LTRIM(RTRIM(approvedby)) AS ApprovedBy,
    CONVERT(varchar(19), Currentdte, 120) AS AppliedAt
FROM LeaveHistory WITH (NOLOCK)
WHERE LOWER(LTRIM(RTRIM(ISNULL(status_leave,'')))) NOT LIKE 'approv%'
  AND LOWER(LTRIM(RTRIM(ISNULL(status_leave,'')))) NOT LIKE 'reject%'
  AND LOWER(LTRIM(RTRIM(ISNULL(status_leave,'')))) NOT LIKE 'cancel%'
  AND (
        LOWER(LTRIM(RTRIM(ISNULL(status_leave,'')))) LIKE 'pend%'
     OR LTRIM(RTRIM(ISNULL(status_leave,''))) = ''
  )
  AND (
        CAST(FromDate AS date) >= CAST(GETDATE() AS date)
     OR Currentdte >= DATEADD(day, -14, GETDATE())
  )
ORDER BY ISNULL(Currentdte, FromDate) DESC, FromDate DESC",
            new { Take = take },
            commandTimeout: 60)).ToList();
        return rows;
    }

    public async Task<HrLeaveDecisionResultDto> DecideLeaveAsync(HrLeaveDecisionRequest request, string decidedBy, bool approve)
    {
        var empCode = RequireEmpCode(request.EmpCode);
        var leaveType = NormalizeLeaveType(request.LeaveType);
        if (!TryParseDate(request.FromDate, out var from) || !TryParseDate(request.ToDate ?? request.FromDate, out var to))
            throw new InvalidOperationException("fromDate / toDate must be yyyy-MM-dd.");

        decidedBy = Truncate((decidedBy ?? "").Trim(), 50);
        if (decidedBy.Length == 0)
            decidedBy = "HR";

        using var connection = _database.CreatePayrollLoginEntryConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            connection.Open();
        using var tx = connection.BeginTransaction();

        try
        {
            var row = await connection.QueryFirstOrDefaultAsync<dynamic>(@"
SELECT TOP 1
    EmpCode, Days, TypeofLeave, FromDate, FromTo, status_leave, Currentdte
FROM LeaveHistory WITH (UPDLOCK, ROWLOCK)
WHERE LTRIM(RTRIM(EmpCode)) = @EmpCode
  AND CAST(FromDate AS date) = @From
  AND CAST(ISNULL(FromTo, FromDate) AS date) = @To
  AND UPPER(LTRIM(RTRIM(TypeofLeave))) = @LeaveType
ORDER BY Currentdte DESC",
                new { EmpCode = empCode, From = from.Date, To = to.Date, LeaveType = leaveType },
                tx,
                commandTimeout: 30);

            if (row is null)
                throw new InvalidOperationException("Leave / WFH request not found.");

            var status = ((string?)row.status_leave ?? "").Trim().ToLowerInvariant();
            if (status.StartsWith("approv", StringComparison.Ordinal))
                throw new InvalidOperationException("This request is already approved.");
            if (status.StartsWith("reject", StringComparison.Ordinal) || status.StartsWith("cancel", StringComparison.Ordinal))
                throw new InvalidOperationException("This request is already rejected/cancelled.");

            var days = Convert.ToDecimal(row.Days ?? 0);
            if (days <= 0)
                days = (decimal)(to - from).TotalDays + 1m;

            var newStatus = approve ? "Approved" : "Rejected";
            var updated = await connection.ExecuteAsync(@"
UPDATE LeaveHistory
SET status_leave = @Status,
    approvedby = @ApprovedBy
WHERE LTRIM(RTRIM(EmpCode)) = @EmpCode
  AND CAST(FromDate AS date) = @From
  AND CAST(ISNULL(FromTo, FromDate) AS date) = @To
  AND UPPER(LTRIM(RTRIM(TypeofLeave))) = @LeaveType
  AND LOWER(LTRIM(RTRIM(ISNULL(status_leave,'')))) NOT LIKE 'approv%'
  AND LOWER(LTRIM(RTRIM(ISNULL(status_leave,'')))) NOT LIKE 'reject%'
  AND LOWER(LTRIM(RTRIM(ISNULL(status_leave,'')))) NOT LIKE 'cancel%'",
                new
                {
                    Status = newStatus,
                    ApprovedBy = decidedBy,
                    EmpCode = empCode,
                    From = from.Date,
                    To = to.Date,
                    LeaveType = leaveType,
                },
                tx,
                commandTimeout: 30);

            if (updated == 0)
                throw new InvalidOperationException("Could not update leave status (already decided or missing).");

            string balanceNote = "";
            if (approve && leaveType is "PL" or "CL")
            {
                var bal = await connection.QueryFirstOrDefaultAsync<dynamic>(@"
SELECT TOP 1 ISNULL(AvailPL,0) AS AvailPl, ISNULL(AvailCL,0) AS AvailCl, FromDate
FROM availableleave WITH (UPDLOCK, ROWLOCK)
WHERE LTRIM(RTRIM(Empcode)) = @EmpCode
ORDER BY FromDate DESC",
                    new { EmpCode = empCode },
                    tx,
                    commandTimeout: 30);

                if (bal is null)
                    throw new InvalidOperationException("No availableleave row to deduct balance.");

                decimal availPl = Convert.ToDecimal(bal.AvailPl);
                decimal availCl = Convert.ToDecimal(bal.AvailCl);
                DateTime fromDate = (DateTime)bal.FromDate;

                if (leaveType == "PL")
                {
                    if (availPl < days)
                        throw new InvalidOperationException($"Insufficient PL ({availPl}) to approve {days} day(s).");
                    await connection.ExecuteAsync(@"
UPDATE availableleave SET AvailPL = AvailPL - @Days
WHERE LTRIM(RTRIM(Empcode)) = @EmpCode AND FromDate = @FromDate",
                        new { EmpCode = empCode, Days = days, FromDate = fromDate },
                        tx);
                    balanceNote = $" Deducted {days} PL.";
                }
                else
                {
                    if (availCl < days)
                        throw new InvalidOperationException($"Insufficient CL ({availCl}) to approve {days} day(s).");
                    await connection.ExecuteAsync(@"
UPDATE availableleave SET AvailCL = AvailCL - @Days
WHERE LTRIM(RTRIM(Empcode)) = @EmpCode AND FromDate = @FromDate",
                        new { EmpCode = empCode, Days = days, FromDate = fromDate },
                        tx);
                    balanceNote = $" Deducted {days} CL.";
                }
            }

            tx.Commit();
            return new HrLeaveDecisionResultDto
            {
                EmpCode = empCode,
                LeaveType = leaveType,
                FromDate = from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ToDate = to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Days = days,
                Status = newStatus,
                Message = approve
                    ? $"Approved. Leave/WFH will reflect on attendance.{balanceNote}"
                    : "Rejected. No balance change.",
            };
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<HrConfirmationResultDto> ApplyConfirmationAsync(string empCode, string? appliedBy = null)
    {
        empCode = RequireEmpCode(empCode);
        using var connection = _database.CreatePayrollLoginEntryConnection();

        var emp = await connection.QueryFirstOrDefaultAsync<(string EmpCode, int IsHoEmp, DateTime? DateOj)>(@"
SELECT LTRIM(RTRIM(EmpCode)) AS EmpCode, ISNULL(IsHOEmp,0) AS IsHoEmp, DateOJ AS DateOj
FROM empinfo WITH (NOLOCK)
WHERE LTRIM(RTRIM(EmpCode)) = @EmpCode", new { EmpCode = empCode });
        if (emp.EmpCode is null)
            throw new InvalidOperationException($"Employee {empCode} not found.");

        var already = await connection.ExecuteScalarAsync<int>(@"
SELECT CASE WHEN EXISTS (
  SELECT 1 FROM EmployeeConfirmation WITH (NOLOCK) WHERE LTRIM(RTRIM(Empcode)) = @EmpCode
) THEN 1 ELSE 0 END", new { EmpCode = empCode });
        if (already == 1)
            throw new InvalidOperationException("Employee is already confirmed in ERP.");

        await connection.ExecuteAsync(@"
INSERT INTO EmployeeConfirmation (Empcode, MailDate) VALUES (@EmpCode, CAST(GETDATE() AS date))",
            new { EmpCode = empCode });

        // Credit CL after confirmation (open leave period row)
        var open = await connection.QueryFirstOrDefaultAsync<(decimal TotalCl, decimal AvailCl, DateTime? FromDate)>(@"
SELECT TOP 1 ISNULL(TotalCL,0) AS TotalCl, ISNULL(AvailCL,0) AS AvailCl, FromDate
FROM availableleave WITH (NOLOCK)
WHERE LTRIM(RTRIM(Empcode)) = @EmpCode
  AND (ToDate IS NULL OR ToDate >= CAST(GETDATE() AS date))
ORDER BY FromDate DESC", new { EmpCode = empCode });

        decimal credited = 0;
        if (open.FromDate.HasValue)
        {
            // Bring TotalCL / AvailCL up to policy entitlement if below
            var target = Math.Max(open.TotalCl, ClCreditOnConfirmation);
            var addAvail = Math.Max(0, target - open.AvailCl);
            if (addAvail > 0 || open.TotalCl < ClCreditOnConfirmation)
            {
                await connection.ExecuteAsync(@"
UPDATE availableleave
SET TotalCL = @TotalCl,
    AvailCL = AvailCL + @AddAvail
WHERE LTRIM(RTRIM(Empcode)) = @EmpCode
  AND FromDate = @FromDate
  AND (ToDate IS NULL OR ToDate >= CAST(GETDATE() AS date))",
                    new
                    {
                        EmpCode = empCode,
                        FromDate = open.FromDate.Value,
                        TotalCl = target,
                        AddAvail = addAvail,
                    });
                credited = addAvail;
            }
        }
        else
        {
            var fyStart = new DateTime(DateTime.Today.Month >= 4 ? DateTime.Today.Year : DateTime.Today.Year - 1, 4, 1);
            await connection.ExecuteAsync(@"
INSERT INTO availableleave (Empcode, TotalPL, TotalCL, AvailPL, AvailCL, FromDate, ToDate)
VALUES (@EmpCode, 0, @Cl, 0, @Cl, @FromDate, NULL)",
                new { EmpCode = empCode, Cl = (double)ClCreditOnConfirmation, FromDate = fyStart });
            credited = ClCreditOnConfirmation;
        }

        return new HrConfirmationResultDto
        {
            EmpCode = empCode,
            Confirmed = true,
            ConfirmedOn = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ClCredited = credited,
            Message = credited > 0
                ? $"Confirmation recorded. CL credited: {credited}."
                : "Confirmation recorded. CL already at entitlement.",
            AppliedBy = appliedBy,
        };
    }

    public async Task<HrLeaveCreditPreviewDto> PreviewHoMonthlyLeaveCreditAsync(string empCode)
    {
        empCode = RequireEmpCode(empCode);
        using var connection = _database.CreatePayrollLoginEntryConnection();
        var emp = await connection.QueryFirstOrDefaultAsync<(int IsHoEmp, DateTime? DateOj)>(@"
SELECT ISNULL(IsHOEmp,0) AS IsHoEmp, DateOJ AS DateOj
FROM empinfo WITH (NOLOCK)
WHERE LTRIM(RTRIM(EmpCode)) = @EmpCode", new { EmpCode = empCode });

        if (emp.DateOj is null)
            return new HrLeaveCreditPreviewDto
            {
                EmpCode = empCode,
                Eligible = false,
                Message = "Date of joining not found.",
            };

        var months = MonthsOfService(emp.DateOj.Value, DateTime.Today);
        var completedOneYear = months >= 12;
        var isHo = emp.IsHoEmp == 1;

        return new HrLeaveCreditPreviewDto
        {
            EmpCode = empCode,
            IsHoEmp = isHo,
            DateOfJoining = emp.DateOj.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            MonthsOfService = months,
            CompletedOneYear = completedOneYear,
            Eligible = isHo && completedOneYear,
            OneTimePlGrant = HoPlGrantAfterOneYear,
            MonthlyPlCredit = HoPlMonthlyCredit,
            Message = !isHo
                ? "Monthly PL credit policy applies to HO employees (IsHOEmp=1) only."
                : !completedOneYear
                    ? $"Eligible after 1 year of joining (now {months} months)."
                    : $"HO emp ≥1 year: one-time {HoPlGrantAfterOneYear} PL after year-1, then +{HoPlMonthlyCredit} PL each month (replaces yearly bulk credit).",
        };
    }

    public async Task<HrLeaveCreditResultDto> ApplyHoMonthlyLeaveCreditAsync(string empCode, bool includeOneTimeGrant = false)
    {
        var preview = await PreviewHoMonthlyLeaveCreditAsync(empCode);
        if (!preview.Eligible)
            throw new InvalidOperationException(preview.Message);

        using var connection = _database.CreatePayrollLoginEntryConnection();
        var open = await connection.QueryFirstOrDefaultAsync<(DateTime? FromDate, decimal TotalPl, decimal AvailPl)>(@"
SELECT TOP 1 FromDate, ISNULL(TotalPL,0) AS TotalPl, ISNULL(AvailPL,0) AS AvailPl
FROM availableleave WITH (NOLOCK)
WHERE LTRIM(RTRIM(Empcode)) = @EmpCode
  AND (ToDate IS NULL OR ToDate >= CAST(GETDATE() AS date))
ORDER BY FromDate DESC", new { EmpCode = empCode });

        if (!open.FromDate.HasValue)
            throw new InvalidOperationException("No open availableleave period to credit.");

        var add = HoPlMonthlyCredit + (includeOneTimeGrant ? HoPlGrantAfterOneYear : 0m);
        await connection.ExecuteAsync(@"
UPDATE availableleave
SET TotalPL = ISNULL(TotalPL,0) + @Add,
    AvailPL = ISNULL(AvailPL,0) + @Add
WHERE LTRIM(RTRIM(Empcode)) = @EmpCode AND FromDate = @FromDate",
            new { EmpCode = empCode, FromDate = open.FromDate.Value, Add = (double)add });

        return new HrLeaveCreditResultDto
        {
            EmpCode = empCode,
            PlCredited = add,
            IncludeOneTimeGrant = includeOneTimeGrant,
            Message = includeOneTimeGrant
                ? $"Credited {HoPlGrantAfterOneYear} (year-1 grant) + {HoPlMonthlyCredit} monthly PL."
                : $"Credited monthly {HoPlMonthlyCredit} PL for HO employee.",
        };
    }

    public async Task EnsureAttendanceAckTableAsync()
    {
        using var connection = _database.CreateConnection();
        await connection.ExecuteAsync(@"
IF OBJECT_ID('dbo.HrAttendanceMonthAck', 'U') IS NULL
BEGIN
  CREATE TABLE dbo.HrAttendanceMonthAck (
    EmpCode varchar(50) NOT NULL,
    YearMonth char(7) NOT NULL,
    Status varchar(20) NOT NULL CONSTRAINT DF_HrAttendanceMonthAck_Status DEFAULT ('Pending'),
    VerifiedAt datetime NOT NULL CONSTRAINT DF_HrAttendanceMonthAck_VerifiedAt DEFAULT (GETDATE()),
    VerifiedBy varchar(100) NULL,
    Note varchar(200) NULL,
    RequestedBy varchar(100) NULL,
    RequestedAt datetime NULL,
    ReviewedBy varchar(100) NULL,
    ReviewedAt datetime NULL,
    CONSTRAINT PK_HrAttendanceMonthAck PRIMARY KEY (EmpCode, YearMonth)
  );
END

IF COL_LENGTH('dbo.HrAttendanceMonthAck', 'Status') IS NULL
  ALTER TABLE dbo.HrAttendanceMonthAck ADD Status varchar(20) NOT NULL
    CONSTRAINT DF_HrAttendanceMonthAck_Status2 DEFAULT ('Approved');
IF COL_LENGTH('dbo.HrAttendanceMonthAck', 'RequestedBy') IS NULL
  ALTER TABLE dbo.HrAttendanceMonthAck ADD RequestedBy varchar(100) NULL;
IF COL_LENGTH('dbo.HrAttendanceMonthAck', 'RequestedAt') IS NULL
  ALTER TABLE dbo.HrAttendanceMonthAck ADD RequestedAt datetime NULL;
IF COL_LENGTH('dbo.HrAttendanceMonthAck', 'ReviewedBy') IS NULL
  ALTER TABLE dbo.HrAttendanceMonthAck ADD ReviewedBy varchar(100) NULL;
IF COL_LENGTH('dbo.HrAttendanceMonthAck', 'ReviewedAt') IS NULL
  ALTER TABLE dbo.HrAttendanceMonthAck ADD ReviewedAt datetime NULL;
");
    }

    public async Task<HrAttendanceAckDto> GetAttendanceAckAsync(string empCode, string yearMonth)
    {
        empCode = RequireEmpCode(empCode);
        yearMonth = RequireYearMonth(yearMonth);
        await EnsureAttendanceAckTableAsync();
        using var connection = _database.CreateConnection();
        var row = await connection.QueryFirstOrDefaultAsync<HrAttendanceAckDto>(@"
SELECT EmpCode, YearMonth,
       LTRIM(RTRIM(ISNULL(Status, 'Approved'))) AS Status,
       CONVERT(varchar(19), VerifiedAt, 120) AS VerifiedAt,
       VerifiedBy, Note,
       RequestedBy,
       CONVERT(varchar(19), RequestedAt, 120) AS RequestedAt,
       ReviewedBy,
       CONVERT(varchar(19), ReviewedAt, 120) AS ReviewedAt,
       CAST(CASE WHEN LTRIM(RTRIM(ISNULL(Status, 'Approved'))) = 'Approved' THEN 1 ELSE 0 END AS bit) AS Verified,
       CAST(CASE WHEN LTRIM(RTRIM(ISNULL(Status, ''))) = 'Pending' THEN 1 ELSE 0 END AS bit) AS PendingHr
FROM HrAttendanceMonthAck WITH (NOLOCK)
WHERE EmpCode = @EmpCode AND YearMonth = @YearMonth",
            new { EmpCode = empCode, YearMonth = yearMonth });
        return row ?? new HrAttendanceAckDto
        {
            EmpCode = empCode,
            YearMonth = yearMonth,
            Status = "None",
            Verified = false,
            PendingHr = false,
        };
    }

    /// <summary>
    /// Employee: send month-end verify request to HR (Status=Pending). Does not mark approved.
    /// </summary>
    public async Task<HrAttendanceAckDto> RequestAttendanceVerifyAsync(
        string empCode,
        string yearMonth,
        string? requestedBy = null,
        string? note = null)
    {
        empCode = RequireEmpCode(empCode);
        yearMonth = RequireYearMonth(yearMonth);
        await EnsureAttendanceAckTableAsync();
        using var connection = _database.CreateConnection();

        var existing = await GetAttendanceAckAsync(empCode, yearMonth);
        if (existing.Verified)
            throw new InvalidOperationException("This month is already approved by HR.");
        if (existing.PendingHr)
            throw new InvalidOperationException("Verify request already sent to HR for this month.");

        await connection.ExecuteAsync(@"
MERGE HrAttendanceMonthAck AS t
USING (SELECT @EmpCode AS EmpCode, @YearMonth AS YearMonth) AS s
ON t.EmpCode = s.EmpCode AND t.YearMonth = s.YearMonth
WHEN MATCHED THEN UPDATE SET
  Status = 'Pending',
  VerifiedAt = GETDATE(),
  VerifiedBy = @RequestedBy,
  Note = @Note,
  RequestedBy = @RequestedBy,
  RequestedAt = GETDATE(),
  ReviewedBy = NULL,
  ReviewedAt = NULL
WHEN NOT MATCHED THEN INSERT
  (EmpCode, YearMonth, Status, VerifiedAt, VerifiedBy, Note, RequestedBy, RequestedAt)
VALUES
  (@EmpCode, @YearMonth, 'Pending', GETDATE(), @RequestedBy, @Note, @RequestedBy, GETDATE());",
            new
            {
                EmpCode = empCode,
                YearMonth = yearMonth,
                RequestedBy = Truncate(requestedBy ?? empCode, 100),
                Note = Truncate(note ?? "Employee requested HR month-end verification", 200),
            });
        return await GetAttendanceAckAsync(empCode, yearMonth);
    }

    /// <summary>HR: approve employee month-end verify request (or mark approved directly).</summary>
    public async Task<HrAttendanceAckDto> ApproveAttendanceVerifyAsync(
        string empCode,
        string yearMonth,
        string? reviewedBy = null,
        string? note = null)
    {
        empCode = RequireEmpCode(empCode);
        yearMonth = RequireYearMonth(yearMonth);
        await EnsureAttendanceAckTableAsync();
        using var connection = _database.CreateConnection();
        await connection.ExecuteAsync(@"
MERGE HrAttendanceMonthAck AS t
USING (SELECT @EmpCode AS EmpCode, @YearMonth AS YearMonth) AS s
ON t.EmpCode = s.EmpCode AND t.YearMonth = s.YearMonth
WHEN MATCHED THEN UPDATE SET
  Status = 'Approved',
  VerifiedAt = GETDATE(),
  VerifiedBy = @ReviewedBy,
  Note = @Note,
  ReviewedBy = @ReviewedBy,
  ReviewedAt = GETDATE()
WHEN NOT MATCHED THEN INSERT
  (EmpCode, YearMonth, Status, VerifiedAt, VerifiedBy, Note, ReviewedBy, ReviewedAt)
VALUES
  (@EmpCode, @YearMonth, 'Approved', GETDATE(), @ReviewedBy, @Note, @ReviewedBy, GETDATE());",
            new
            {
                EmpCode = empCode,
                YearMonth = yearMonth,
                ReviewedBy = Truncate(reviewedBy ?? "HR", 100),
                Note = Truncate(note ?? "HR approved month-end attendance", 200),
            });
        return await GetAttendanceAckAsync(empCode, yearMonth);
    }

    /// <summary>Legacy name — prefer Request / Approve based on role.</summary>
    public Task<HrAttendanceAckDto> VerifyAttendanceMonthAsync(
        string empCode,
        string yearMonth,
        string? verifiedBy = null,
        string? note = null) =>
        RequestAttendanceVerifyAsync(empCode, yearMonth, verifiedBy, note);

    public async Task<IReadOnlyList<HrAttendanceAckDto>> ListPendingAttendanceVerifyAsync(int take = 100)
    {
        take = Math.Clamp(take, 1, 500);
        await EnsureAttendanceAckTableAsync();
        using var connection = _database.CreateConnection();
        var rows = await connection.QueryAsync<HrAttendanceAckDto>(@"
SELECT TOP (@Take)
       EmpCode, YearMonth,
       LTRIM(RTRIM(ISNULL(Status, 'Pending'))) AS Status,
       CONVERT(varchar(19), VerifiedAt, 120) AS VerifiedAt,
       VerifiedBy, Note,
       RequestedBy,
       CONVERT(varchar(19), RequestedAt, 120) AS RequestedAt,
       ReviewedBy,
       CONVERT(varchar(19), ReviewedAt, 120) AS ReviewedAt,
       CAST(0 AS bit) AS Verified,
       CAST(1 AS bit) AS PendingHr
FROM HrAttendanceMonthAck WITH (NOLOCK)
WHERE LTRIM(RTRIM(ISNULL(Status, ''))) = 'Pending'
ORDER BY ISNULL(RequestedAt, VerifiedAt) DESC",
            new { Take = take },
            commandTimeout: 60);
        return rows.ToList();
    }


    private static int MonthsOfService(DateTime dateOfJoining, DateTime asOf)
    {
        var months = (asOf.Year - dateOfJoining.Year) * 12 + asOf.Month - dateOfJoining.Month;
        if (asOf.Day < dateOfJoining.Day) months--;
        return Math.Max(0, months);
    }

    private static string NormalizeLeaveType(string? leaveType) =>
        (leaveType ?? "").Trim().ToUpperInvariant();

    private static bool TryParseDate(string? value, out DateTime date) =>
        DateTime.TryParseExact(
            (value ?? "").Trim(),
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);

    private static string RequireYearMonth(string yearMonth)
    {
        if (!DateTime.TryParseExact(
                (yearMonth ?? "").Trim(),
                "yyyy-MM",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _))
            throw new InvalidOperationException("yearMonth must be yyyy-MM.");
        return (yearMonth ?? "").Trim();
    }

    private static string RequireEmpCode(string? empCode)
    {
        var code = (empCode ?? "").Trim();
        if (code.Length == 0)
            throw new InvalidOperationException("empCode is required.");
        return code;
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}

public sealed class HrLeaveEligibilityDto
{
    public string EmpCode { get; set; } = "";
    public string? DateOfJoining { get; set; }
    public int MonthsOfService { get; set; }
    public bool CompletedOneYear { get; set; }
    public bool CanApplyPlCl { get; set; }
    public string Message { get; set; } = "";
}

public sealed class HrLeaveApplyRequest
{
    public string EmpCode { get; set; } = "";
    public string LeaveType { get; set; } = "PL";
    public string FromDate { get; set; } = "";
    public string? ToDate { get; set; }
    public decimal? Days { get; set; }
    public string? Purpose { get; set; }
    public string? Username { get; set; }
}

public sealed class HrLeaveApplicationDto
{
    public string EmpCode { get; set; } = "";
    public string LeaveType { get; set; } = "";
    public decimal Days { get; set; }
    public string FromDate { get; set; } = "";
    public string ToDate { get; set; } = "";
    public string? Purpose { get; set; }
    public string? Status { get; set; }
    public string? ApprovedBy { get; set; }
    public string? AppliedAt { get; set; }
}

public sealed class HrLeaveApplyResultDto
{
    public string EmpCode { get; set; } = "";
    public string LeaveType { get; set; } = "";
    public string FromDate { get; set; } = "";
    public string ToDate { get; set; } = "";
    public decimal Days { get; set; }
    public string Status { get; set; } = "";
    public string Message { get; set; } = "";
}

public sealed class HrLeaveDecisionRequest
{
    public string EmpCode { get; set; } = "";
    public string LeaveType { get; set; } = "";
    public string FromDate { get; set; } = "";
    public string? ToDate { get; set; }
    public string? Username { get; set; }
}

public sealed class HrLeaveDecisionResultDto
{
    public string EmpCode { get; set; } = "";
    public string LeaveType { get; set; } = "";
    public string FromDate { get; set; } = "";
    public string ToDate { get; set; } = "";
    public decimal Days { get; set; }
    public string Status { get; set; } = "";
    public string Message { get; set; } = "";
}

public sealed class HrConfirmationResultDto
{
    public string EmpCode { get; set; } = "";
    public bool Confirmed { get; set; }
    public string ConfirmedOn { get; set; } = "";
    public decimal ClCredited { get; set; }
    public string Message { get; set; } = "";
    public string? AppliedBy { get; set; }
}

public sealed class HrLeaveCreditPreviewDto
{
    public string EmpCode { get; set; } = "";
    public bool IsHoEmp { get; set; }
    public string? DateOfJoining { get; set; }
    public int MonthsOfService { get; set; }
    public bool CompletedOneYear { get; set; }
    public bool Eligible { get; set; }
    public decimal OneTimePlGrant { get; set; }
    public decimal MonthlyPlCredit { get; set; }
    public string Message { get; set; } = "";
}

public sealed class HrLeaveCreditResultDto
{
    public string EmpCode { get; set; } = "";
    public decimal PlCredited { get; set; }
    public bool IncludeOneTimeGrant { get; set; }
    public string Message { get; set; } = "";
}

public sealed class HrAttendanceAckDto
{
    public string EmpCode { get; set; } = "";
    public string YearMonth { get; set; } = "";
    /// <summary>None | Pending | Approved</summary>
    public string Status { get; set; } = "None";
    public bool Verified { get; set; }
    public bool PendingHr { get; set; }
    public string? VerifiedAt { get; set; }
    public string? VerifiedBy { get; set; }
    public string? Note { get; set; }
    public string? RequestedBy { get; set; }
    public string? RequestedAt { get; set; }
    public string? ReviewedBy { get; set; }
    public string? ReviewedAt { get; set; }
}
