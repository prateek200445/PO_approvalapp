using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using Dapper;

namespace POApprovalAPI.Services;

public sealed class HrReportsService
{
    /// <summary>Maximum late first-punch time for a full day (without needing 9 hours).</summary>
    public static readonly TimeSpan MaxLateIn = new(10, 30, 0);

    /// <summary>Full-day alternative: total worked duration (last out − first in) ≥ 9 hours.</summary>
    public static readonly TimeSpan FullDayWorked = TimeSpan.FromHours(9);

    private readonly DatabaseService _database;

    public HrReportsService(DatabaseService database)
    {
        _database = database;
    }

    public async Task<IReadOnlyList<string>> GetCompaniesAsync()
    {
        using var connection = _database.CreatePayrollLoginEntryConnection();
        var rows = await connection.QueryAsync<string>(@"
SELECT LTRIM(RTRIM(CompanyName)) AS CompanyName
FROM empinfo WITH (NOLOCK)
WHERE ISNULL(LTRIM(RTRIM(CompanyName)), '') <> ''
GROUP BY LTRIM(RTRIM(CompanyName))
ORDER BY COUNT(*) DESC, LTRIM(RTRIM(CompanyName))", commandTimeout: 60);
        return rows.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    public async Task<IReadOnlyList<string>> GetBranchesAsync(string? company)
    {
        using var connection = _database.CreatePayrollLoginEntryConnection();
        var companyFilter = (company ?? "").Trim();
        var sql = @"
SELECT LTRIM(RTRIM(Branch)) AS Branch
FROM empinfo WITH (NOLOCK)
WHERE ISNULL(LTRIM(RTRIM(Branch)), '') <> ''";
        object args;
        if (companyFilter.Length > 0)
        {
            sql += " AND LOWER(LTRIM(RTRIM(CompanyName))) = LOWER(@Company)";
            args = new { Company = companyFilter };
        }
        else
        {
            args = new { };
        }

        sql += @"
GROUP BY LTRIM(RTRIM(Branch))
ORDER BY
  CASE WHEN LTRIM(RTRIM(Branch)) = 'RegistrationOffice' THEN 0 ELSE 1 END,
  COUNT(*) DESC,
  LTRIM(RTRIM(Branch))";

        var rows = await connection.QueryAsync<string>(sql, args, commandTimeout: 60);
        return rows.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
    }

    public async Task<IReadOnlyList<HrEmployeeOptionDto>> SearchEmployeesAsync(
        string? q,
        string? company = null,
        string? branch = null,
        bool officeOnly = false,
        int take = 80)
    {
        // Office/HO list is small (~140); allow full set. Otherwise keep search results bounded.
        take = officeOnly ? Math.Clamp(take, 1, 500) : Math.Clamp(take, 1, 200);
        using var connection = _database.CreatePayrollLoginEntryConnection();
        var term = (q ?? "").Trim();
        var companyFilter = (company ?? "").Trim();
        var branchFilter = (branch ?? "").Trim();

        var sql = @"
SELECT TOP (@Take)
    LTRIM(RTRIM(e.EmpCode)) AS EmpCode,
    LTRIM(RTRIM(e.Name)) AS Name,
    LTRIM(RTRIM(e.Designation)) AS Designation,
    LTRIM(RTRIM(e.Deptt)) AS Department,
    LTRIM(RTRIM(e.CompanyName)) AS CompanyName,
    LTRIM(RTRIM(e.Branch)) AS Branch,
    CASE WHEN ISNULL(e.IsHOEmp, 0) = 1 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsHoEmp,
    LTRIM(RTRIM(e.isactive)) AS IsActive
FROM empinfo e WITH (NOLOCK)
WHERE ISNULL(LTRIM(RTRIM(e.EmpCode)), '') <> ''
  AND ISNULL(LTRIM(RTRIM(e.Name)), '') <> ''
  AND LOWER(LTRIM(RTRIM(ISNULL(e.isactive,'')))) = 'yes'";

        if (companyFilter.Length > 0)
            sql += " AND LOWER(LTRIM(RTRIM(e.CompanyName))) = LOWER(@Company)";
        if (branchFilter.Length > 0)
            sql += " AND LOWER(LTRIM(RTRIM(e.Branch))) = LOWER(@Branch)";
        // True HO/office staff flag in ERP — NOT Branch=RegistrationOffice (that mix includes plant workers).
        if (officeOnly)
            sql += " AND ISNULL(e.IsHOEmp, 0) = 1";

        if (term.Length > 0)
        {
            sql += @"
  AND (
        e.EmpCode LIKE @Like
     OR e.Name LIKE @Like
     OR e.Designation LIKE @Like
     OR e.Deptt LIKE @Like
     OR e.CompanyName LIKE @Like
     OR e.Branch LIKE @Like
  )";
        }

        sql += @"
ORDER BY
  CASE WHEN ISNULL(e.IsHOEmp, 0) = 1 THEN 0 ELSE 1 END,
  CASE WHEN @TermLen > 0 AND e.EmpCode = @Exact THEN 0
       WHEN @TermLen > 0 AND e.EmpCode LIKE @Prefix THEN 1
       ELSE 2 END,
  e.Name";

        return (await connection.QueryAsync<HrEmployeeOptionDto>(
            sql,
            new
            {
                Take = take,
                Company = companyFilter,
                Branch = branchFilter,
                Like = $"%{EscapeLike(term)}%",
                Exact = term,
                Prefix = EscapeLike(term) + "%",
                TermLen = term.Length,
            },
            commandTimeout: 60)).ToList();
    }

    public async Task<HrAttendanceReportDto> GetAttendanceReportAsync(
        string empCode,
        string yearMonth,
        bool applyHalfDayRule = true)
    {
        var (year, month, from, to) = ParseYearMonth(yearMonth);
        empCode = RequireEmpCode(empCode);

        using var connection = _database.CreatePayrollLoginEntryConnection();
        var employee = await connection.QueryFirstOrDefaultAsync<HrEmployeeOptionDto>(@"
SELECT
    LTRIM(RTRIM(EmpCode)) AS EmpCode,
    LTRIM(RTRIM(Name)) AS Name,
    LTRIM(RTRIM(Designation)) AS Designation,
    LTRIM(RTRIM(Deptt)) AS Department,
    LTRIM(RTRIM(CompanyName)) AS CompanyName,
    LTRIM(RTRIM(Branch)) AS Branch,
    CASE WHEN ISNULL(IsHOEmp, 0) = 1 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsHoEmp,
    LTRIM(RTRIM(isactive)) AS IsActive
FROM empinfo WITH (NOLOCK)
WHERE LTRIM(RTRIM(EmpCode)) = @EmpCode", new { EmpCode = empCode }, commandTimeout: 30);

        if (employee is null)
            throw new InvalidOperationException($"Employee '{empCode}' was not found in empinfo.");

        var dateOj = await connection.ExecuteScalarAsync<DateTime?>(@"
SELECT DateOJ FROM empinfo WITH (NOLOCK)
WHERE LTRIM(RTRIM(EmpCode)) = @EmpCode", new { EmpCode = empCode }, commandTimeout: 15);
        var monthsOfService = dateOj is null
            ? 0
            : Math.Max(0,
                (to.Year - dateOj.Value.Year) * 12 + to.Month - dateOj.Value.Month
                - (to.Day < dateOj.Value.Day ? 1 : 0));
        var canApplyPlCl = monthsOfService >= 12;

        var punches = (await connection.QueryAsync<HrPunchRow>(@"
SELECT
    CAST(Sysdate AS date) AS AttendanceDate,
    InTime AS InTime,
    OutTime AS OutTime,
    CAST(NULL AS varchar(50)) AS Branch
FROM tempattendance WITH (NOLOCK)
WHERE LTRIM(RTRIM(Empcode)) = @EmpCode
  AND Sysdate >= @From
  AND Sysdate < @ToExclusive
  AND InTime IS NOT NULL
ORDER BY Sysdate, InTime",
            new { EmpCode = empCode, From = from, ToExclusive = to.AddDays(1) },
            commandTimeout: 90)).ToList();

        // Fallback: machine punches (exclude corrupt future dates on some devices)
        if (punches.Count == 0)
        {
            punches = (await connection.QueryAsync<HrPunchRow>(@"
SELECT
    CAST(AttendanceDate AS date) AS AttendanceDate,
    intime AS InTime,
    CAST(NULL AS datetime) AS OutTime,
    LTRIM(RTRIM(Branch)) AS Branch
FROM Attendancemachine WITH (NOLOCK)
WHERE LTRIM(RTRIM(Empcode)) = @EmpCode
  AND AttendanceDate >= @From
  AND AttendanceDate < @ToExclusive
  AND AttendanceDate <= DATEADD(day, 1, CAST(GETDATE() AS date))
  AND intime IS NOT NULL
ORDER BY AttendanceDate, intime",
                new { EmpCode = empCode, From = from, ToExclusive = to.AddDays(1) },
                commandTimeout: 90)).ToList();
        }

        var punchesByDay = punches.GroupBy(p => p.AttendanceDate.Date).ToDictionary(g => g.Key, g => g.ToList());

        var leaveRows = (await connection.QueryAsync<HrLeaveRow>(@"
SELECT
    LTRIM(RTRIM(TypeofLeave)) AS TypeofLeave,
    ISNULL(Days, 0) AS Days,
    CAST(FromDate AS date) AS FromDate,
    CAST(ISNULL(FromTo, FromDate) AS date) AS ToDate,
    LTRIM(RTRIM(status_leave)) AS StatusLeave
FROM LeaveHistory WITH (NOLOCK)
WHERE LTRIM(RTRIM(EmpCode)) = @EmpCode
  AND LOWER(LTRIM(RTRIM(ISNULL(status_leave, '')))) LIKE 'approv%'
  AND FromDate < @ToExclusive
  AND ISNULL(FromTo, FromDate) >= @From",
            new { EmpCode = empCode, From = from, ToExclusive = to.AddDays(1) },
            commandTimeout: 60)).ToList();

        var leaveByDay = new Dictionary<DateTime, (string Type, decimal DayValue)>();
        foreach (var leave in leaveRows)
        {
            var type = (leave.TypeofLeave ?? "").Trim().ToUpperInvariant();
            // Under 1 year: ignore PL/CL entirely (no calendar / payable leave of those types).
            if (!canApplyPlCl && type is "PL" or "CL")
                continue;
            if (type is not ("PL" or "CL" or "WFH"))
                continue;

            var spanDays = (leave.ToDate.Date - leave.FromDate.Date).Days + 1;
            if (spanDays <= 0) continue;

            // Single-day half leave (Days=0.5); multi-day uses 1 per calendar day.
            var perDay = spanDays == 1 && leave.Days > 0 && leave.Days < 1
                ? leave.Days
                : 1m;

            for (var ld = leave.FromDate.Date; ld <= leave.ToDate.Date; ld = ld.AddDays(1))
            {
                if (ld < from || ld > to) continue;
                // Prefer first mapped leave; if both, keep existing
                if (!leaveByDay.ContainsKey(ld))
                    leaveByDay[ld] = (type, perDay);
            }
        }

        var leaveBalance = canApplyPlCl
            ? await connection.QueryFirstOrDefaultAsync<HrLeaveBalanceRow>(@"
SELECT TOP 1
    ISNULL(TotalPL, 0) AS TotalPl,
    ISNULL(TotalCL, 0) AS TotalCl,
    ISNULL(AvailPL, 0) AS AvailPl,
    ISNULL(AvailCL, 0) AS AvailCl,
    FromDate,
    ToDate
FROM availableleave WITH (NOLOCK)
WHERE LTRIM(RTRIM(Empcode)) = @EmpCode
  AND FromDate <= @MonthEnd
  AND (ToDate IS NULL OR ToDate >= @MonthStart)
ORDER BY FromDate DESC",
                new { EmpCode = empCode, MonthStart = from, MonthEnd = to },
                commandTimeout: 30)
            : null;

        if (canApplyPlCl && leaveBalance is null)
        {
            leaveBalance = await connection.QueryFirstOrDefaultAsync<HrLeaveBalanceRow>(@"
SELECT TOP 1
    ISNULL(TotalPL, 0) AS TotalPl,
    ISNULL(TotalCL, 0) AS TotalCl,
    ISNULL(AvailPL, 0) AS AvailPl,
    ISNULL(AvailCL, 0) AS AvailCl,
    FromDate,
    ToDate
FROM availableleave WITH (NOLOCK)
WHERE LTRIM(RTRIM(Empcode)) = @EmpCode
ORDER BY FromDate DESC",
                new { EmpCode = empCode },
                commandTimeout: 30);
        }

        var days = new List<HrAttendanceDayDto>();
        for (var d = from; d <= to; d = d.AddDays(1))
        {
            punchesByDay.TryGetValue(d, out var dayPunches);
            var firstIn = dayPunches?.Where(x => x.InTime.HasValue).OrderBy(x => x.InTime).FirstOrDefault();
            var lastOut = dayPunches?
                .Where(x => x.OutTime.HasValue)
                .OrderByDescending(x => x.OutTime)
                .FirstOrDefault();

            string status;
            decimal payable;
            decimal? workedHours = null;
            if (firstIn?.InTime is DateTime inn && lastOut?.OutTime is DateTime outt && outt > inn)
                workedHours = Math.Round((decimal)(outt - inn).TotalHours, 2, MidpointRounding.AwayFromZero);

            if (leaveByDay.TryGetValue(d, out var leaveDay))
            {
                status = leaveDay.Type; // PL, CL, or WFH
                // PL/CL/WFH count as payable (WFH treated as present day).
                payable = leaveDay.DayValue;
            }
            else
            {
                status = Classify(firstIn?.InTime, lastOut?.OutTime, applyHalfDayRule);
                payable = status switch
                {
                    "Present" => 1m,
                    "Half Day" => 0.5m,
                    _ => 0m,
                };
            }

            days.Add(new HrAttendanceDayDto
            {
                Date = d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                DayName = d.ToString("ddd", CultureInfo.InvariantCulture),
                PunchIn = firstIn?.InTime?.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
                PunchOut = lastOut?.OutTime?.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
                WorkedHours = workedHours,
                Branch = firstIn?.Branch ?? lastOut?.Branch,
                Status = status,
                PayableDay = payable,
            });
        }

        var present = days.Count(x => x.Status == "Present");
        var half = days.Count(x => x.Status == "Half Day");
        var absent = days.Count(x => x.Status == "Absent");
        var plDays = days.Where(x => x.Status == "PL").Sum(x => x.PayableDay);
        var clDays = days.Where(x => x.Status == "CL").Sum(x => x.PayableDay);
        var wfhDays = days.Where(x => x.Status == "WFH").Sum(x => x.PayableDay);
        var payableDays = days.Sum(x => x.PayableDay);

        string? dataNote = null;
        if (present == 0 && half == 0 && plDays == 0 && clDays == 0 && wfhDays == 0)
        {
            var latestAny = await connection.ExecuteScalarAsync<DateTime?>(@"
SELECT MAX(Sysdate) FROM tempattendance WITH (NOLOCK)", commandTimeout: 60);
            if (latestAny is null)
                dataNote = "No punches found in payroll Loginentry.tempattendance.";
            else if (latestAny.Value.Date < from)
                dataNote =
                    $"Payroll attendance (port 3445) has no punches after {latestAny.Value:dd-MMM-yyyy} for this period.";
        }

        return new HrAttendanceReportDto
        {
            YearMonth = $"{year:D4}-{month:D2}",
            PeriodLabel = from.ToString("MMMM yyyy", CultureInfo.InvariantCulture),
            HalfDayAfter = applyHalfDayRule ? "in≤10:30 or ≥9h worked" : "off",
            ApplyHalfDayRule = applyHalfDayRule,
            Employee = employee,
            Days = days,
            DataNote = dataNote,
            CanApplyPlCl = canApplyPlCl,
            CompletedOneYear = canApplyPlCl,
            MonthsOfService = monthsOfService,
            DateOfJoining = dateOj?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            LeaveBalance = leaveBalance is null
                ? null
                : new HrLeaveBalanceDto
                {
                    TotalPl = leaveBalance.TotalPl,
                    TotalCl = leaveBalance.TotalCl,
                    AvailPl = leaveBalance.AvailPl,
                    AvailCl = leaveBalance.AvailCl,
                    PeriodFrom = leaveBalance.FromDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    PeriodTo = leaveBalance.ToDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                },
            Summary = new HrAttendanceSummaryDto
            {
                CalendarDays = days.Count,
                PresentDays = present,
                HalfDays = half,
                AbsentDays = absent,
                PlDays = canApplyPlCl ? plDays : 0,
                ClDays = canApplyPlCl ? clDays : 0,
                WfhDays = wfhDays,
                PayableDays = payableDays,
            },
        };
    }

    public async Task<HrSalaryReportDto> GetSalaryReportAsync(
        string empCode,
        string yearMonth,
        decimal? monthlyBasic,
        decimal? dailyRate,
        int? workingDaysOverride,
        bool applyHalfDayRule = true)
    {
        var attendance = await GetAttendanceReportAsync(empCode, yearMonth, applyHalfDayRule);
        var payableDays = attendance.Summary.PayableDays;
        var calendarDays = attendance.Summary.CalendarDays;
        var (year, month, from, to) = ParseYearMonth(yearMonth);

        var sundayOffWorkingDays = Enumerable.Range(0, calendarDays)
            .Select(i => DateTime.ParseExact(attendance.Days[i].Date, "yyyy-MM-dd", CultureInfo.InvariantCulture))
            .Count(d => d.DayOfWeek != DayOfWeek.Sunday);

        var workingDays = workingDaysOverride is > 0
            ? workingDaysOverride.Value
            : Math.Max(1, sundayOffWorkingDays);

        var erpRate = await ResolveErpWageAsync(empCode, from, to);

        string formula;
        decimal computed;
        decimal? usedMonthly = monthlyBasic is > 0 ? monthlyBasic : null;
        decimal? usedDaily = dailyRate is > 0 ? dailyRate : null;
        var rateSource = "manual";
        string? rateNote = null;

        // Manual override wins; otherwise auto from ERP Salary / SalaryPerDay.
        if (usedDaily is null && usedMonthly is null && erpRate is not null)
        {
            if (erpRate.IsDaily && erpRate.DailyRate is > 0)
            {
                usedDaily = erpRate.DailyRate;
                rateSource = erpRate.Source;
            }
            else if (erpRate.MonthlySalary is > 0)
            {
                usedMonthly = erpRate.MonthlySalary;
                rateSource = erpRate.Source;
            }

            rateNote = erpRate.Detail;
        }
        else if (usedDaily is not null || usedMonthly is not null)
        {
            rateSource = "manual";
            rateNote = "Using manually entered rate (overrides ERP).";
        }

        if (usedDaily is not null)
        {
            computed = Math.Round(usedDaily.Value * payableDays, 2, MidpointRounding.AwayFromZero);
            formula = $"DailyRate ({usedDaily:0.##}) × PayableDays ({payableDays:0.##})";
        }
        else if (usedMonthly is not null)
        {
            computed = Math.Round(
                usedMonthly.Value * payableDays / workingDays,
                2,
                MidpointRounding.AwayFromZero);
            formula =
                $"MonthlySalary ({usedMonthly:0.##}) × PayableDays ({payableDays:0.##}) / WorkingDays ({workingDays})";
        }
        else
        {
            throw new InvalidOperationException(
                $"No ERP salary master found for '{empCode}' in {year:D4}-{month:D2}, and no manual rate was provided.");
        }

        return new HrSalaryReportDto
        {
            YearMonth = attendance.YearMonth,
            PeriodLabel = attendance.PeriodLabel,
            Employee = attendance.Employee,
            AttendanceSummary = attendance.Summary,
            MonthlyBasic = usedMonthly,
            DailyRate = usedDaily,
            ErpBasicDa = erpRate?.BasicDa,
            ErpGrossSalary = erpRate?.GrossSalary,
            RateSource = rateSource,
            RateDetail = rateNote,
            WorkingDays = workingDays,
            PayableDays = payableDays,
            Formula = formula,
            ComputedSalary = computed,
            Note =
                (applyHalfDayRule
                    ? "Present = first punch on/before 10:30 AM, OR worked ≥ 9 hours (last out − first in). Otherwise punched day = Half Day (0.5). "
                    : "Attendance rule off: any punch counts as full Present (1 day). ") +
                "Auto rate from payroll Salary master (monthly package) or empinfo.SalaryPerDay when IsSalaryPerDay=yes.",
        };
    }

    /// <summary>
    /// Wage discovery (payroll Loginentry @ 3445):
    /// 1. empinfo.IsSalaryPerDay=yes + SalaryPerDay → daily rate × payable days
    /// 2. Salary master (Salary / Total) effective for month → monthly × payable / working days
    /// 3. empinfo.CTC / 12 as last-resort monthly
    /// Joining date: empinfo.DateOJ (used for leave eligibility, not wage).
    /// Payable days: Present=1 (in≤10:30 or ≥9h), Half Day=0.5, Absent=0 (+ approved PL/CL/WFH).
    /// </summary>
    private async Task<ErpWageRate?> ResolveErpWageAsync(string empCode, DateTime monthStart, DateTime monthEnd)
    {
        empCode = RequireEmpCode(empCode);
        using var connection = _database.CreatePayrollLoginEntryConnection();

        var emp = await connection.QueryFirstOrDefaultAsync<dynamic>(@"
SELECT
    ISNULL(SalaryPerDay, 0) AS SalaryPerDay,
    LTRIM(RTRIM(ISNULL(IsSalaryPerDay, ''))) AS IsSalaryPerDay,
    ISNULL(CTC, 0) AS Ctc
FROM empinfo WITH (NOLOCK)
WHERE LTRIM(RTRIM(EmpCode)) = @EmpCode", new { EmpCode = empCode }, commandTimeout: 30);

        if (emp is not null)
        {
            var isDaily = string.Equals(
                (string?)emp.IsSalaryPerDay,
                "yes",
                StringComparison.OrdinalIgnoreCase);
            decimal perDay = Convert.ToDecimal(emp.SalaryPerDay);
            if (isDaily && perDay > 0)
            {
                return new ErpWageRate
                {
                    IsDaily = true,
                    DailyRate = perDay,
                    Source = "empinfo.SalaryPerDay",
                    Detail = $"IsSalaryPerDay=yes · SalaryPerDay={perDay:0.##}",
                };
            }
        }

        // Open-ended ToDate (NULL) = current rate band in ERP.
        var salary = await connection.QueryFirstOrDefaultAsync<dynamic>(@"
SELECT TOP 1
    ISNULL(Salary, 0) AS Salary,
    ISNULL(Basic_DA, 0) AS BasicDa,
    ISNULL(GrossSalary, 0) AS GrossSalary,
    ISNULL(Total, 0) AS Total,
    FromDate,
    ToDate
FROM Salary WITH (NOLOCK)
WHERE LTRIM(RTRIM(EmpCode)) = @EmpCode
  AND FromDate <= @MonthEnd
  AND (ToDate IS NULL OR ToDate >= @MonthStart)
ORDER BY FromDate DESC",
            new { EmpCode = empCode, MonthStart = monthStart, MonthEnd = monthEnd },
            commandTimeout: 30);

        if (salary is null)
        {
            // Fallback: latest Salary row before/on month end
            salary = await connection.QueryFirstOrDefaultAsync<dynamic>(@"
SELECT TOP 1
    ISNULL(Salary, 0) AS Salary,
    ISNULL(Basic_DA, 0) AS BasicDa,
    ISNULL(GrossSalary, 0) AS GrossSalary,
    ISNULL(Total, 0) AS Total,
    FromDate,
    ToDate
FROM Salary WITH (NOLOCK)
WHERE LTRIM(RTRIM(EmpCode)) = @EmpCode
  AND FromDate <= @MonthEnd
ORDER BY FromDate DESC",
                new { EmpCode = empCode, MonthEnd = monthEnd },
                commandTimeout: 30);
        }

        if (salary is not null)
        {
            decimal monthly = Convert.ToDecimal(salary.Salary);
            if (monthly <= 0)
                monthly = Convert.ToDecimal(salary.Total);
            if (monthly > 0)
            {
                DateTime fromDate = (DateTime)salary.FromDate;
                DateTime? toDate = salary.ToDate as DateTime?;
                var toLabel = toDate.HasValue ? toDate.Value.ToString("dd-MMM-yyyy") : "open";
                return new ErpWageRate
                {
                    IsDaily = false,
                    MonthlySalary = monthly,
                    BasicDa = Convert.ToDecimal(salary.BasicDa),
                    GrossSalary = Convert.ToDecimal(salary.GrossSalary),
                    Source = "Salary master",
                    Detail =
                        $"Salary={monthly:0.##} · Basic_DA={Convert.ToDecimal(salary.BasicDa):0.##} · " +
                        $"effective {fromDate:dd-MMM-yyyy} → {toLabel}",
                };
            }
        }

        // Last resort: CTC / 12
        if (emp is not null)
        {
            decimal ctc = Convert.ToDecimal(emp.Ctc);
            if (ctc > 0)
            {
                var monthly = Math.Round(ctc / 12m, 2, MidpointRounding.AwayFromZero);
                return new ErpWageRate
                {
                    IsDaily = false,
                    MonthlySalary = monthly,
                    Source = "empinfo.CTC/12",
                    Detail = $"No Salary master row · estimated from CTC {ctc:0.##} / 12 = {monthly:0.##}",
                };
            }
        }

        return null;
    }

    private sealed class ErpWageRate
    {
        public bool IsDaily { get; set; }
        public decimal? MonthlySalary { get; set; }
        public decimal? DailyRate { get; set; }
        public decimal? BasicDa { get; set; }
        public decimal? GrossSalary { get; set; }
        public string Source { get; set; } = "";
        public string Detail { get; set; } = "";
    }

    public async Task<byte[]> BuildAttendanceExcelAsync(
        string empCode,
        string yearMonth,
        bool applyHalfDayRule = true)
    {
        var report = await GetAttendanceReportAsync(empCode, yearMonth, applyHalfDayRule);
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Attendance");
        ws.Cell(1, 1).Value = "Employee Attendance Report";
        ws.Cell(2, 1).Value = $"{report.Employee.Name} ({report.Employee.EmpCode})";
        ws.Cell(3, 1).Value = report.PeriodLabel;
        ws.Cell(4, 1).Value = report.CanApplyPlCl
            ? $"Present {report.Summary.PresentDays} | Half {report.Summary.HalfDays} | PL {report.Summary.PlDays} | CL {report.Summary.ClDays} | WFH {report.Summary.WfhDays} | Absent {report.Summary.AbsentDays} | Payable {report.Summary.PayableDays}"
            : $"Present {report.Summary.PresentDays} | Half {report.Summary.HalfDays} | WFH {report.Summary.WfhDays} | Absent {report.Summary.AbsentDays} | Payable {report.Summary.PayableDays} (PL/CL N/A — under 1 year)";
        if (report.CanApplyPlCl && report.LeaveBalance is not null)
        {
            ws.Cell(5, 1).Value =
                $"Leave balance — Avail PL {report.LeaveBalance.AvailPl} / Total {report.LeaveBalance.TotalPl} · Avail CL {report.LeaveBalance.AvailCl} / Total {report.LeaveBalance.TotalCl}";
        }
        else if (!report.CanApplyPlCl)
        {
            ws.Cell(5, 1).Value =
                $"PL/CL not applicable until 1 year of joining ({report.MonthsOfService} month(s)" +
                (string.IsNullOrEmpty(report.DateOfJoining) ? ")" : $", DOJ {report.DateOfJoining})");
        }

        ws.Cell(7, 1).Value = "Date";
        ws.Cell(7, 2).Value = "Day";
        ws.Cell(7, 3).Value = "Punch In";
        ws.Cell(7, 4).Value = "Punch Out";
        ws.Cell(7, 5).Value = "Worked Hrs";
        ws.Cell(7, 6).Value = "Status";
        ws.Cell(7, 7).Value = "Payable Day";
        ws.Cell(7, 8).Value = "Branch";
        ws.Cell(7, 9).Value = "Rule";
        ws.Range(7, 1, 7, 9).Style.Font.SetBold();
        ws.Cell(6, 1).Value = "Rule: Present if in ≤ 10:30 AM OR worked ≥ 9 hours; else Half Day (0.5).";

        var r = 8;
        foreach (var day in report.Days)
        {
            ws.Cell(r, 1).Value = day.Date;
            ws.Cell(r, 2).Value = day.DayName;
            ws.Cell(r, 3).Value = day.PunchIn ?? "";
            ws.Cell(r, 4).Value = day.PunchOut ?? "";
            ws.Cell(r, 5).Value = day.WorkedHours?.ToString("0.##") ?? "";
            ws.Cell(r, 6).Value = day.Status;
            ws.Cell(r, 7).Value = day.PayableDay;
            ws.Cell(r, 8).Value = day.Branch ?? "";
            ws.Cell(r, 9).Value = report.HalfDayAfter;
            r++;
        }

        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> BuildSalaryExcelAsync(
        string empCode,
        string yearMonth,
        decimal? monthlyBasic,
        decimal? dailyRate,
        int? workingDaysOverride,
        bool applyHalfDayRule = true)
    {
        var sal = await GetSalaryReportAsync(
            empCode, yearMonth, monthlyBasic, dailyRate, workingDaysOverride, applyHalfDayRule);
        var att = await GetAttendanceReportAsync(empCode, yearMonth, applyHalfDayRule);
        using var wb = new XLWorkbook();
        var sum = wb.Worksheets.Add("Salary");
        sum.Cell(1, 1).Value = "Employee Salary Calculation";
        sum.Cell(2, 1).Value = $"{sal.Employee.Name} ({sal.Employee.EmpCode})";
        sum.Cell(3, 1).Value = sal.PeriodLabel;
        sum.Cell(5, 1).Value = "Present days";
        sum.Cell(5, 2).Value = sal.AttendanceSummary.PresentDays;
        sum.Cell(6, 1).Value = "Half days";
        sum.Cell(6, 2).Value = sal.AttendanceSummary.HalfDays;
        sum.Cell(7, 1).Value = "Absent days";
        sum.Cell(7, 2).Value = sal.AttendanceSummary.AbsentDays;
        sum.Cell(8, 1).Value = "Payable days";
        sum.Cell(8, 2).Value = sal.PayableDays;
        sum.Cell(9, 1).Value = "Working days";
        sum.Cell(9, 2).Value = sal.WorkingDays;
        sum.Cell(10, 1).Value = "Monthly salary";
        sum.Cell(10, 2).Value = sal.MonthlyBasic;
        sum.Cell(11, 1).Value = "Daily rate";
        sum.Cell(11, 2).Value = sal.DailyRate;
        sum.Cell(12, 1).Value = "Rate source";
        sum.Cell(12, 2).Value = sal.RateSource;
        sum.Cell(13, 1).Value = "Rate detail";
        sum.Cell(13, 2).Value = sal.RateDetail ?? "";
        sum.Cell(14, 1).Value = "ERP Basic_DA";
        sum.Cell(14, 2).Value = sal.ErpBasicDa;
        sum.Cell(15, 1).Value = "Formula";
        sum.Cell(15, 2).Value = sal.Formula;
        sum.Cell(16, 1).Value = "Computed salary";
        sum.Cell(16, 2).Value = sal.ComputedSalary;
        sum.Cell(16, 2).Style.Font.SetBold();
        sum.Columns().AdjustToContents();

        var ws = wb.Worksheets.Add("Attendance");
        ws.Cell(1, 1).Value = "Date";
        ws.Cell(1, 2).Value = "Day";
        ws.Cell(1, 3).Value = "Punch In";
        ws.Cell(1, 4).Value = "Punch Out";
        ws.Cell(1, 5).Value = "Worked Hrs";
        ws.Cell(1, 6).Value = "Status";
        ws.Cell(1, 7).Value = "Payable Day";
        var r = 2;
        foreach (var day in att.Days)
        {
            ws.Cell(r, 1).Value = day.Date;
            ws.Cell(r, 2).Value = day.DayName;
            ws.Cell(r, 3).Value = day.PunchIn ?? "";
            ws.Cell(r, 4).Value = day.PunchOut ?? "";
            ws.Cell(r, 5).Value = day.WorkedHours?.ToString("0.##") ?? "";
            ws.Cell(r, 6).Value = day.Status;
            ws.Cell(r, 7).Value = day.PayableDay;
            r++;
        }

        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Present if first punch ≤ 10:30 AM <b>or</b> worked duration ≥ 9 hours.
    /// Otherwise a punched day is Half Day; no punch = Absent.
    /// </summary>
    public static string Classify(DateTime? inTime, DateTime? outTime = null, bool applyHalfDayRule = true)
    {
        if (inTime is null) return "Absent";
        if (!applyHalfDayRule) return "Present";

        var inTod = inTime.Value.TimeOfDay;
        if (inTod <= MaxLateIn)
            return "Present";

        if (outTime is DateTime outt && outt > inTime.Value)
        {
            var worked = outt - inTime.Value;
            if (worked >= FullDayWorked)
                return "Present";
        }

        return "Half Day";
    }

    private static (int Year, int Month, DateTime From, DateTime To) ParseYearMonth(string yearMonth)
    {
        if (!DateTime.TryParseExact(
                yearMonth?.Trim(),
                "yyyy-MM",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dt))
            throw new InvalidOperationException("yearMonth must be yyyy-MM.");

        var from = new DateTime(dt.Year, dt.Month, 1);
        var to = from.AddMonths(1).AddDays(-1);
        return (dt.Year, dt.Month, from, to);
    }

    private static string RequireEmpCode(string empCode)
    {
        var code = (empCode ?? "").Trim();
        if (code.Length == 0)
            throw new InvalidOperationException("empCode is required.");
        return code;
    }

    private static string EscapeLike(string value) =>
        value.Replace("[", "[[]", StringComparison.Ordinal)
            .Replace("%", "[%]", StringComparison.Ordinal)
            .Replace("_", "[_]", StringComparison.Ordinal);

    private sealed class HrPunchRow
    {
        public DateTime AttendanceDate { get; set; }
        public DateTime? InTime { get; set; }
        public DateTime? OutTime { get; set; }
        public string? Branch { get; set; }
    }

    private sealed class HrLeaveRow
    {
        public string? TypeofLeave { get; set; }
        public decimal Days { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string? StatusLeave { get; set; }
    }

    private sealed class HrLeaveBalanceRow
    {
        public decimal TotalPl { get; set; }
        public decimal TotalCl { get; set; }
        public decimal AvailPl { get; set; }
        public decimal AvailCl { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}

public sealed class HrEmployeeOptionDto
{
    public string EmpCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Designation { get; set; }
    public string? Department { get; set; }
    public string? CompanyName { get; set; }
    public string? Branch { get; set; }
    public bool IsHoEmp { get; set; }
    public string? IsActive { get; set; }
}

public sealed class HrAttendanceDayDto
{
    public string Date { get; set; } = "";
    public string DayName { get; set; } = "";
    public string? PunchIn { get; set; }
    public string? PunchOut { get; set; }
    /// <summary>Hours between first in and last out (null if incomplete punches).</summary>
    public decimal? WorkedHours { get; set; }
    public string Status { get; set; } = "";
    public decimal PayableDay { get; set; }
    public string? Branch { get; set; }
}

public sealed class HrAttendanceSummaryDto
{
    public int CalendarDays { get; set; }
    public int PresentDays { get; set; }
    public int HalfDays { get; set; }
    public int AbsentDays { get; set; }
    public decimal PlDays { get; set; }
    public decimal ClDays { get; set; }
    public decimal WfhDays { get; set; }
    public decimal PayableDays { get; set; }
}

public sealed class HrLeaveBalanceDto
{
    public decimal TotalPl { get; set; }
    public decimal TotalCl { get; set; }
    public decimal AvailPl { get; set; }
    public decimal AvailCl { get; set; }
    public string? PeriodFrom { get; set; }
    public string? PeriodTo { get; set; }
}

public sealed class HrAttendanceReportDto
{
    public string YearMonth { get; set; } = "";
    public string PeriodLabel { get; set; } = "";
    public string HalfDayAfter { get; set; } = "in≤10:30 or ≥9h worked";
    public bool ApplyHalfDayRule { get; set; } = true;
    public HrEmployeeOptionDto Employee { get; set; } = new();
    public HrAttendanceSummaryDto Summary { get; set; } = new();
    public HrLeaveBalanceDto? LeaveBalance { get; set; }
    public List<HrAttendanceDayDto> Days { get; set; } = [];
    public string? DataNote { get; set; }
    public bool CanApplyPlCl { get; set; } = true;
    public bool CompletedOneYear { get; set; } = true;
    public int MonthsOfService { get; set; }
    public string? DateOfJoining { get; set; }
}

public sealed class HrSalaryReportDto
{
    public string YearMonth { get; set; } = "";
    public string PeriodLabel { get; set; } = "";
    public HrEmployeeOptionDto Employee { get; set; } = new();
    public HrAttendanceSummaryDto AttendanceSummary { get; set; } = new();
    public decimal? MonthlyBasic { get; set; }
    public decimal? DailyRate { get; set; }
    public decimal? ErpBasicDa { get; set; }
    public decimal? ErpGrossSalary { get; set; }
    public string RateSource { get; set; } = "";
    public string? RateDetail { get; set; }
    public int WorkingDays { get; set; }
    public decimal PayableDays { get; set; }
    public string Formula { get; set; } = "";
    public decimal ComputedSalary { get; set; }
    public string Note { get; set; } = "";
}
