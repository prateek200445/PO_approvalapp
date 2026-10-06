using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Dapper;

namespace POApprovalAPI.Services;

/// <summary>
/// One-click HR master data from the payroll ERP: personal details, origin state, joining/exit, current salary,
/// salary revision history (Salary master date bands), PF/ESIC, headcount by designation/department and attrition.
/// An employee is current when empinfo.isactive = 'yes' and there is no Leavingform entry on or after DateOJ;
/// the ERP keeps many leavers flagged active, so the leaving form is treated as the source of truth for exits.
/// </summary>
public sealed class HrMasterService
{
    private const int WageDaysPerMonth = 26;

    // Requests inside FreshFor are served from memory; older data (up to ServeStaleFor) is still returned instantly
    // while one background reload runs, so users only wait for the ERP on a cold start.
    private static readonly TimeSpan FreshFor = TimeSpan.FromMinutes(35);
    private static readonly TimeSpan ServeStaleFor = TimeSpan.FromHours(12);
    private static readonly object RefreshGate = new();
    private static Snapshot? _snapshot;
    private static Task<Snapshot>? _refreshTask;

    private readonly DatabaseService _database;
    private readonly ILogger<HrMasterService> _logger;

    public HrMasterService(DatabaseService database, ILogger<HrMasterService> logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task<HrMasterReportDto> GetReportAsync(HrMasterQuery query, int employeeLimit = 200, bool refresh = false)
    {
        var snap = await GetSnapshotAsync(refresh);
        var fy = ResolveFy(query.FyStartYear);
        var stats = StatsFor(snap, query, fy);
        var listed = FilterList(stats.Scoped, query, fy).ToList();

        return new HrMasterReportDto
        {
            GeneratedAt = DateTime.Now,
            DataAsOf = snap.LoadedAt,
            FyLabel = fy.Label,
            Summary = stats.Summary,
            Designations = stats.Designations,
            Departments = stats.Departments,
            Companies = stats.Companies,
            OriginStates = stats.OriginStates,
            Attrition = stats.Attrition,
            AttritionByCompany = stats.AttritionByCompany,
            AttritionByDepartment = stats.AttritionByDepartment,
            EmployeeTotal = listed.Count,
            Employees = listed.Take(Math.Max(1, employeeLimit)).Select(ToDto).ToList(),
        };
    }

    public async Task<IReadOnlyList<string>> GetCompaniesAsync()
    {
        var snap = await GetSnapshotAsync();
        return snap.CompanyNames;
    }

    /// <summary>Reloads from the ERP and precomputes the default (all companies, current FY) view.</summary>
    public async Task WarmAsync()
    {
        var snap = await RefreshAsync();
        StatsFor(snap, new HrMasterQuery(), ResolveFy(null));
    }

    public async Task<byte[]> BuildExcelAsync(HrMasterQuery query)
    {
        var snap = await GetSnapshotAsync();
        var fy = ResolveFy(query.FyStartYear);
        var stats = StatsFor(snap, query, fy);
        var scoped = stats.Scoped;
        var listed = FilterList(scoped, query, fy).ToList();
        var summary = stats.Summary;
        var attrition = stats.Attrition;

        using var wb = new XLWorkbook();

        var s = wb.AddWorksheet("Summary");
        s.Cell(1, 1).Value = "HR master data";
        s.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(14);
        var info = new (string, XLCellValue)[]
        {
            ("Generated", DateTime.Now.ToString("dd-MMM-yyyy HH:mm", CultureInfo.InvariantCulture)),
            ("Company", string.IsNullOrWhiteSpace(query.Company) ? "All companies" : query.Company),
            ("Branch", string.IsNullOrWhiteSpace(query.Branch) ? "All branches" : query.Branch),
            ("Employee list", StatusLabel(query.Status)),
            ("Financial year", fy.Label),
            ("Current headcount", summary.CurrentHeadcount),
            ("Male / Female / Other", $"{summary.Male} / {summary.Female} / {summary.OtherGender}"),
            ($"Joiners {fy.Label}", summary.JoinersFy),
            ($"Leavers {fy.Label}", summary.LeaversFy),
            ($"Attrition {fy.Label} (%)", summary.AttritionFyPct),
            ("Average tenure (years)", summary.AvgTenureYears),
            ("Average age (years)", summary.AvgAgeYears),
            ("PF covered (current)", summary.PfCovered),
            ("ESIC number recorded (current)", summary.EsicCovered),
            ("Monthly salary of current employees", summary.MonthlySalaryTotal),
            ("", ""),
            ("Current employee", "ERP active = yes and no leaving form dated on/after the joining date"),
            ("Exit date", "Latest ERP leaving form date on/after the joining date"),
            ("Salary / increments", "ERP Salary master revisions (FromDate bands); increment = rise in monthly total vs previous revision"),
            ("Monthly salary basis", $"Salary master total; else salary per day × {WageDaysPerMonth}; else CTC ÷ 12"),
            ("Attrition", "Leavers in period ÷ average headcount ((opening + closing) ÷ 2) × 100"),
            ("Origin state", "Read from the permanent address (state, district or PIN code)"),
        };
        for (var i = 0; i < info.Length; i++)
        {
            s.Cell(i + 3, 1).Value = info[i].Item1;
            s.Cell(i + 3, 1).Style.Font.SetBold();
            s.Cell(i + 3, 2).Value = info[i].Item2;
        }
        s.Cell(17, 2).Style.NumberFormat.Format = "#,##0";
        s.Columns().AdjustToContents(1, 60);

        var emp = wb.AddWorksheet("Employees");
        var headers = new[]
        {
            "Emp code", "Name", "Father name", "Gender", "Date of birth", "Age", "Marital status", "Blood group",
            "Mobile", "Email", "Qualification", "Origin state", "Permanent address", "Present address",
            "Company", "Branch", "Department", "Designation", "Category", "Contractor",
            "Date of joining", "Confirmation date", "Tenure (years)", "Status", "Exit date", "Exit reason",
            "Monthly salary", "Salary basis", "Gross salary", "CTC (yearly)", "Salary per day", "Salary from",
            "Last increment date", "Last increment amount", "Last increment %", "No. of revisions",
            "PF applicable", "PF account", "UAN", "ESIC no", "PAN", "Aadhaar (masked)",
            "Bank", "Bank account (masked)", "IFSC",
        };
        WriteHeader(emp, headers);
        var r = 2;
        foreach (var e in listed)
        {
            var c = 1;
            emp.Cell(r, c++).Value = e.EmpCode;
            emp.Cell(r, c++).Value = e.Name;
            emp.Cell(r, c++).Value = e.FatherName;
            emp.Cell(r, c++).Value = e.Gender;
            SetDate(emp.Cell(r, c++), e.Dob);
            SetNum(emp.Cell(r, c++), e.Age(DateTime.Today));
            emp.Cell(r, c++).Value = e.MaritalStatus;
            emp.Cell(r, c++).Value = e.BloodGroup;
            emp.Cell(r, c++).Value = e.Mobile;
            emp.Cell(r, c++).Value = e.Email;
            emp.Cell(r, c++).Value = e.Qualification;
            emp.Cell(r, c++).Value = e.OriginState;
            emp.Cell(r, c++).Value = e.PermanentAddress;
            emp.Cell(r, c++).Value = e.PresentAddress;
            emp.Cell(r, c++).Value = e.Company;
            emp.Cell(r, c++).Value = e.Branch;
            emp.Cell(r, c++).Value = e.Department;
            emp.Cell(r, c++).Value = e.Designation;
            emp.Cell(r, c++).Value = e.Category;
            emp.Cell(r, c++).Value = e.Contractor;
            SetDate(emp.Cell(r, c++), e.DateOfJoining);
            SetDate(emp.Cell(r, c++), e.ConfirmationDate);
            SetNum(emp.Cell(r, c++), e.TenureYears(DateTime.Today));
            emp.Cell(r, c++).Value = e.IsCurrent ? "Current" : "Left";
            SetDate(emp.Cell(r, c++), e.ExitDate);
            emp.Cell(r, c++).Value = e.ExitReason;
            SetMoney(emp.Cell(r, c++), e.MonthlySalary);
            emp.Cell(r, c++).Value = e.SalaryBasis;
            SetMoney(emp.Cell(r, c++), e.CurrentSalary?.Gross);
            SetMoney(emp.Cell(r, c++), e.Ctc > 0 ? e.Ctc : null);
            SetMoney(emp.Cell(r, c++), e.SalaryPerDay > 0 ? e.SalaryPerDay : null);
            SetDate(emp.Cell(r, c++), e.CurrentSalary?.FromDate);
            SetDate(emp.Cell(r, c++), e.LastIncrement?.Date);
            SetMoney(emp.Cell(r, c++), e.LastIncrement?.Amount);
            SetNum(emp.Cell(r, c++), e.LastIncrement?.Pct);
            emp.Cell(r, c++).Value = e.Salaries.Count;
            emp.Cell(r, c++).Value = e.PfApplicable;
            emp.Cell(r, c++).Value = e.PfAccount;
            emp.Cell(r, c++).Value = e.Uan;
            emp.Cell(r, c++).Value = e.Esic;
            emp.Cell(r, c++).Value = e.Pan;
            emp.Cell(r, c++).Value = Mask(e.Aadhaar);
            emp.Cell(r, c++).Value = e.BankName;
            emp.Cell(r, c++).Value = Mask(e.BankAccount);
            emp.Cell(r, c++).Value = e.Ifsc;
            r++;
        }
        FinishTable(emp, r - 1, headers.Length);

        var hist = wb.AddWorksheet("Salary history");
        var histHeaders = new[]
        {
            "Emp code", "Name", "Company", "Designation", "From date", "To date", "Basic + DA", "HRA",
            "Gross salary", "Monthly total", "Employee PF", "Employer PF", "Net payable", "Change vs previous", "Change %",
        };
        WriteHeader(hist, histHeaders);
        r = 2;
        foreach (var e in listed)
        {
            SalaryBand? prev = null;
            foreach (var b in e.Salaries)
            {
                var c = 1;
                hist.Cell(r, c++).Value = e.EmpCode;
                hist.Cell(r, c++).Value = e.Name;
                hist.Cell(r, c++).Value = e.Company;
                hist.Cell(r, c++).Value = e.Designation;
                SetDate(hist.Cell(r, c++), b.FromDate);
                SetDate(hist.Cell(r, c++), b.ToDate);
                SetMoney(hist.Cell(r, c++), b.BasicDa);
                SetMoney(hist.Cell(r, c++), b.Hra);
                SetMoney(hist.Cell(r, c++), b.Gross);
                SetMoney(hist.Cell(r, c++), b.Monthly);
                SetMoney(hist.Cell(r, c++), b.EmployeePf);
                SetMoney(hist.Cell(r, c++), b.EmployerPf);
                SetMoney(hist.Cell(r, c++), b.NetPayable);
                if (prev != null)
                {
                    var diff = b.Monthly - prev.Monthly;
                    SetMoney(hist.Cell(r, c++), diff);
                    SetNum(hist.Cell(r, c++), prev.Monthly > 0 ? Math.Round(diff / prev.Monthly * 100, 1) : null);
                }
                prev = b;
                r++;
            }
        }
        FinishTable(hist, r - 1, histHeaders.Length);

        WriteHeadcountSheet(wb, "Headcount by designation", "Designation", scoped, e => e.Designation);
        WriteHeadcountSheet(wb, "Headcount by department", "Department", scoped, e => e.Department);
        WriteHeadcountSheet(wb, "Headcount by origin state", "Origin state", scoped, e => e.OriginState);

        var at = wb.AddWorksheet("Attrition");
        at.Cell(1, 1).Value = $"Month-wise attrition {fy.Label}";
        at.Cell(1, 1).Style.Font.SetBold();
        var atHeaders = new[] { "Month", "Opening headcount", "Joiners", "Leavers", "Closing headcount", "Average headcount", "Attrition %" };
        for (var i = 0; i < atHeaders.Length; i++) at.Cell(2, i + 1).Value = atHeaders[i];
        at.Range(2, 1, 2, atHeaders.Length).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#E8EEF7"));
        r = 3;
        foreach (var m in attrition)
        {
            at.Cell(r, 1).Value = m.Label;
            at.Cell(r, 2).Value = m.Opening;
            at.Cell(r, 3).Value = m.Joiners;
            at.Cell(r, 4).Value = m.Leavers;
            at.Cell(r, 5).Value = m.Closing;
            at.Cell(r, 6).Value = m.AvgHeadcount;
            at.Cell(r, 7).Value = m.RatePct;
            r++;
        }
        at.Cell(r, 1).Value = $"{fy.Label} total";
        at.Cell(r, 3).Value = summary.JoinersFy;
        at.Cell(r, 4).Value = summary.LeaversFy;
        at.Cell(r, 7).Value = summary.AttritionFyPct;
        at.Row(r).Style.Font.SetBold();
        r += 2;
        r = WriteAttritionGroup(at, r, "Company", stats.AttritionByCompany);
        r += 1;
        WriteAttritionGroup(at, r, "Department", stats.AttritionByDepartment);
        at.Columns().AdjustToContents(1, 40);

        var lv = wb.AddWorksheet($"Leavers {fy.Label}".Replace("/", "-"));
        var lvHeaders = new[] { "Emp code", "Name", "Company", "Branch", "Department", "Designation", "Date of joining", "Exit date", "Tenure (years)", "Exit reason", "Remarks", "Last monthly salary" };
        WriteHeader(lv, lvHeaders);
        r = 2;
        foreach (var e in scoped.Where(e => e.ExitDate >= fy.Start && e.ExitDate < fy.EndExclusive).OrderByDescending(e => e.ExitDate))
        {
            var c = 1;
            lv.Cell(r, c++).Value = e.EmpCode;
            lv.Cell(r, c++).Value = e.Name;
            lv.Cell(r, c++).Value = e.Company;
            lv.Cell(r, c++).Value = e.Branch;
            lv.Cell(r, c++).Value = e.Department;
            lv.Cell(r, c++).Value = e.Designation;
            SetDate(lv.Cell(r, c++), e.DateOfJoining);
            SetDate(lv.Cell(r, c++), e.ExitDate);
            SetNum(lv.Cell(r, c++), e.TenureYears(e.ExitDate ?? DateTime.Today));
            lv.Cell(r, c++).Value = e.ExitReason;
            lv.Cell(r, c++).Value = e.ExitRemarks;
            SetMoney(lv.Cell(r, c++), e.MonthlySalary);
            r++;
        }
        FinishTable(lv, r - 1, lvHeaders.Length);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static string StatusLabel(string? status) => (status ?? "current").ToLowerInvariant() switch
    {
        "left_fy" => "Left this financial year",
        "left" => "All leavers",
        "all" => "All employees (current and left)",
        _ => "Current employees",
    };

    private static List<Employee> ApplyScope(List<Employee> all, HrMasterQuery q)
    {
        var company = (q.Company ?? "").Trim();
        var branch = (q.Branch ?? "").Trim();
        return all.Where(e =>
                (company == "" || string.Equals(e.Company, company, StringComparison.OrdinalIgnoreCase)) &&
                (branch == "" || string.Equals(e.Branch, branch, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    private static IEnumerable<Employee> FilterList(List<Employee> scoped, HrMasterQuery q, Fy fy)
    {
        var status = (q.Status ?? "current").Trim().ToLowerInvariant();
        IEnumerable<Employee> rows = status switch
        {
            "left_fy" => scoped.Where(e => e.ExitDate >= fy.Start && e.ExitDate < fy.EndExclusive),
            "left" => scoped.Where(e => !e.IsCurrent),
            "all" => scoped,
            _ => scoped.Where(e => e.IsCurrent),
        };
        var designation = (q.Designation ?? "").Trim();
        if (designation != "")
            rows = rows.Where(e => string.Equals(e.Designation, designation, StringComparison.OrdinalIgnoreCase));
        var department = (q.Department ?? "").Trim();
        if (department != "")
            rows = rows.Where(e => string.Equals(e.Department, department, StringComparison.OrdinalIgnoreCase));
        var search = (q.Search ?? "").Trim();
        if (search != "")
            rows = rows.Where(e =>
                e.EmpCode.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                e.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                e.Designation.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                e.Department.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                e.Uan.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                e.PfAccount.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                e.Esic.Contains(search, StringComparison.OrdinalIgnoreCase));
        return status is "left_fy" or "left"
            ? rows.OrderByDescending(e => e.ExitDate ?? DateTime.MinValue).ThenBy(e => e.Name)
            : rows.OrderBy(e => e.Company).ThenBy(e => e.Name);
    }

    private static HrMasterSummaryDto BuildSummary(List<Employee> scoped, Fy fy, List<HrAttritionMonthDto> months)
    {
        var today = DateTime.Today;
        var current = scoped.Where(e => e.IsCurrent).ToList();
        var leavers = scoped.Count(e => e.ExitDate >= fy.Start && e.ExitDate < fy.EndExclusive);
        var joiners = scoped.Count(e => e.DateOfJoining >= fy.Start && e.DateOfJoining < fy.EndExclusive);
        var avgHeadcount = months.Count == 0 ? 0 : months.Average(m => m.AvgHeadcount);
        var tenures = current.Select(e => e.TenureYears(today)).Where(t => t != null).Select(t => t!.Value).ToList();
        var ages = current.Select(e => e.Age(today)).Where(a => a is > 14 and < 90).Select(a => a!.Value).ToList();
        return new HrMasterSummaryDto
        {
            CurrentHeadcount = current.Count,
            Male = current.Count(e => e.Gender == "Male"),
            Female = current.Count(e => e.Gender == "Female"),
            OtherGender = current.Count(e => e.Gender is not ("Male" or "Female")),
            JoinersFy = joiners,
            LeaversFy = leavers,
            AttritionFyPct = avgHeadcount > 0 ? Math.Round((decimal)(leavers / avgHeadcount * 100), 1) : 0,
            AvgTenureYears = tenures.Count == 0 ? 0 : Math.Round(tenures.Average(), 1),
            AvgAgeYears = ages.Count == 0 ? 0 : Math.Round(ages.Average(), 1),
            PfCovered = current.Count(e => e.PfApplicable.Equals("Yes", StringComparison.OrdinalIgnoreCase) || e.Uan != ""),
            EsicCovered = current.Count(e => e.Esic != ""),
            MonthlySalaryTotal = Math.Round(current.Sum(e => e.MonthlySalary ?? 0), 0),
            LeftFlaggedActive = scoped.Count(e => e.ActiveFlag && !e.IsCurrent),
        };
    }

    private static List<HrHeadcountRowDto> Headcount(List<Employee> scoped, Func<Employee, string> key)
    {
        var current = scoped.Where(e => e.IsCurrent).ToList();
        return current
            .GroupBy(key, StringComparer.OrdinalIgnoreCase)
            .Select(g => new HrHeadcountRowDto
            {
                Name = g.Key,
                Count = g.Count(),
                Male = g.Count(e => e.Gender == "Male"),
                Female = g.Count(e => e.Gender == "Female"),
                MonthlySalary = Math.Round(g.Sum(e => e.MonthlySalary ?? 0), 0),
                ByCompany = g.GroupBy(e => e.Company, StringComparer.OrdinalIgnoreCase)
                    .OrderByDescending(c => c.Count())
                    .Select(c => new HrNameCountDto { Name = c.Key, Count = c.Count() })
                    .ToList(),
            })
            .OrderByDescending(r => r.Count).ThenBy(r => r.Name)
            .ToList();
    }

    private static bool InHeadcount(Employee e, DateTime at) =>
        e.DateOfJoining != null && e.DateOfJoining < at &&
        (e.ExitDate != null ? e.ExitDate >= at : e.IsCurrent);

    private static List<HrAttritionMonthDto> BuildAttrition(List<Employee> scoped, Fy fy)
    {
        var list = new List<HrAttritionMonthDto>();
        var lastMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        for (var m = fy.Start; m < fy.EndExclusive && m <= lastMonth; m = m.AddMonths(1))
        {
            var next = m.AddMonths(1);
            var opening = scoped.Count(e => InHeadcount(e, m));
            var closing = scoped.Count(e => InHeadcount(e, next));
            var leavers = scoped.Count(e => e.ExitDate >= m && e.ExitDate < next);
            var joiners = scoped.Count(e => e.DateOfJoining >= m && e.DateOfJoining < next);
            var avg = (opening + closing) / 2.0;
            list.Add(new HrAttritionMonthDto
            {
                Month = m.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                Label = m.ToString("MMM yyyy", CultureInfo.InvariantCulture),
                Opening = opening,
                Joiners = joiners,
                Leavers = leavers,
                Closing = closing,
                AvgHeadcount = Math.Round(avg, 1),
                RatePct = avg > 0 ? Math.Round((decimal)(leavers / avg * 100), 1) : 0,
            });
        }
        return list;
    }

    private static List<HrAttritionGroupDto> AttritionByGroup(List<Employee> scoped, Fy fy, Func<Employee, string> key)
    {
        return scoped
            .GroupBy(key, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var members = g.ToList();
                var months = BuildAttrition(members, fy);
                var avg = months.Count == 0 ? 0 : months.Average(m => m.AvgHeadcount);
                var leavers = members.Count(e => e.ExitDate >= fy.Start && e.ExitDate < fy.EndExclusive);
                return new HrAttritionGroupDto
                {
                    Name = g.Key,
                    CurrentHeadcount = members.Count(e => e.IsCurrent),
                    AvgHeadcount = Math.Round(avg, 1),
                    Joiners = members.Count(e => e.DateOfJoining >= fy.Start && e.DateOfJoining < fy.EndExclusive),
                    Leavers = leavers,
                    RatePct = avg > 0 ? Math.Round((decimal)(leavers / avg * 100), 1) : 0,
                };
            })
            .Where(g => g.CurrentHeadcount > 0 || g.Leavers > 0)
            .OrderByDescending(g => g.CurrentHeadcount)
            .ToList();
    }

    private static Fy ResolveFy(int? startYear)
    {
        var today = DateTime.Today;
        var y = startYear ?? (today.Month >= 4 ? today.Year : today.Year - 1);
        var start = new DateTime(y, 4, 1);
        return new Fy(start, start.AddYears(1), $"FY {y}-{(y + 1) % 100:00}");
    }

    private static ScopeStats StatsFor(Snapshot snap, HrMasterQuery q, Fy fy)
    {
        var key = $"{(q.Company ?? "").Trim()}|{(q.Branch ?? "").Trim()}|{fy.Start.Year}";
        return snap.Stats.GetOrAdd(key, _ => new Lazy<ScopeStats>(() =>
        {
            var scoped = ApplyScope(snap.Employees, q);
            var attrition = BuildAttrition(scoped, fy);
            return new ScopeStats(
                scoped,
                BuildSummary(scoped, fy, attrition),
                Headcount(scoped, e => e.Designation),
                Headcount(scoped, e => e.Department),
                Headcount(scoped, e => e.Company),
                Headcount(scoped, e => e.OriginState),
                attrition,
                AttritionByGroup(scoped, fy, e => e.Company),
                AttritionByGroup(scoped, fy, e => e.Department));
        })).Value;
    }

    private async Task<Snapshot> GetSnapshotAsync(bool forceRefresh = false)
    {
        var snap = _snapshot;
        if (!forceRefresh && snap != null)
        {
            var age = DateTime.UtcNow - snap.LoadedAt;
            if (age < FreshFor) return snap;
            if (age < ServeStaleFor)
            {
                _ = RefreshAsync().ContinueWith(
                    t => _logger.LogWarning(t.Exception, "HR master background refresh failed."),
                    TaskContinuationOptions.OnlyOnFaulted);
                return snap;
            }
        }
        return await RefreshAsync();
    }

    private Task<Snapshot> RefreshAsync()
    {
        lock (RefreshGate)
        {
            return _refreshTask ??= LoadSnapshotAsync();
        }
    }

    private async Task<Snapshot> LoadSnapshotAsync()
    {
        try
        {
            await Task.Yield();
            var employees = await LoadFromErpAsync();
            var snap = new Snapshot(DateTime.UtcNow, employees);
            _snapshot = snap;
            return snap;
        }
        finally
        {
            lock (RefreshGate) _refreshTask = null;
        }
    }

    private async Task<List<Employee>> LoadFromErpAsync()
    {
        List<EmpRow> rows;
        List<SalaryRow> salaries;
        using (var connection = _database.CreatePayrollLoginEntryConnection())
        {
            rows = (await connection.QueryAsync<EmpRow>(@"
WITH lf AS (
    SELECT LTRIM(RTRIM(Empcode)) AS EmpCode, sysDateTime, Reason, Remarks,
           ROW_NUMBER() OVER (PARTITION BY LTRIM(RTRIM(Empcode)) ORDER BY sysDateTime DESC) AS rn
    FROM Leavingform WITH (NOLOCK)
    WHERE sysDateTime IS NOT NULL
)
SELECT
    LTRIM(RTRIM(e.EmpCode)) AS EmpCode, LTRIM(RTRIM(e.Name)) AS Name, LTRIM(RTRIM(e.Fathername)) AS FatherName,
    LTRIM(RTRIM(e.Sex)) AS Sex, e.DOB, LTRIM(RTRIM(e.mrg_status)) AS MaritalStatus, LTRIM(RTRIM(e.Blood)) AS BloodGroup,
    LTRIM(RTRIM(e.ContactNo)) AS Mobile, LTRIM(RTRIM(e.Email)) AS Email, LTRIM(RTRIM(e.Qualification)) AS Qualification,
    e.PermanentAdd AS PermanentAddress, e.PersentAdd AS PresentAddress,
    LTRIM(RTRIM(e.CompanyName)) AS Company, LTRIM(RTRIM(e.Branch)) AS Branch, LTRIM(RTRIM(e.Deptt)) AS Department,
    LTRIM(RTRIM(e.Designation)) AS Designation, LTRIM(RTRIM(e.category)) AS Category,
    LTRIM(RTRIM(e.ContractorName)) AS Contractor, e.DateOJ AS DateOfJoining, e.cnfrm_dt AS ConfirmationDate,
    ISNULL(e.CTC, 0) AS Ctc, ISNULL(e.SalaryPerDay, 0) AS SalaryPerDay,
    LTRIM(RTRIM(e.PFApplicability)) AS PfApplicable, LTRIM(RTRIM(e.PFaccount)) AS PfAccount,
    LTRIM(RTRIM(e.UANNo)) AS Uan, LTRIM(RTRIM(e.ESICNo)) AS Esic, LTRIM(RTRIM(e.pan_no)) AS Pan,
    LTRIM(RTRIM(e.AadharNo)) AS Aadhaar, LTRIM(RTRIM(e.bank_name)) AS BankName,
    LTRIM(RTRIM(e.BankNo)) AS BankAccount, LTRIM(RTRIM(e.IFSCCode)) AS Ifsc,
    CASE WHEN LOWER(LTRIM(RTRIM(ISNULL(e.isactive, '')))) = 'yes' THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS ActiveFlag,
    lf.sysDateTime AS LastLeavingDate, LTRIM(RTRIM(lf.Reason)) AS LeavingReason, LTRIM(RTRIM(lf.Remarks)) AS LeavingRemarks
FROM empinfo e WITH (NOLOCK)
LEFT JOIN lf ON lf.EmpCode = LTRIM(RTRIM(e.EmpCode)) AND lf.rn = 1
WHERE ISNULL(LTRIM(RTRIM(e.EmpCode)), '') <> ''", commandTimeout: 180)).ToList();

            salaries = (await connection.QueryAsync<SalaryRow>(@"
SELECT LTRIM(RTRIM(EmpCode)) AS EmpCode, FromDate, ToDate,
       ISNULL(Salary, 0) AS Salary, ISNULL(Basic_DA, 0) AS BasicDa, ISNULL(HRA, 0) AS Hra,
       ISNULL(GrossSalary, 0) AS Gross, ISNULL(Total, 0) AS Total,
       ISNULL(EmployeePF, 0) AS EmployeePf, ISNULL(EmployerPF, 0) AS EmployerPf, ISNULL(NetPayable, 0) AS NetPayable
FROM Salary WITH (NOLOCK)
WHERE ISNULL(LTRIM(RTRIM(EmpCode)), '') <> '' AND FromDate IS NOT NULL", commandTimeout: 180)).ToList();
        }

        var bands = salaries
            .GroupBy(s => s.EmpCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.OrderBy(s => s.FromDate).Select(s => new SalaryBand
            {
                FromDate = s.FromDate,
                ToDate = s.ToDate,
                BasicDa = s.BasicDa,
                Hra = s.Hra,
                Gross = s.Gross,
                Monthly = s.Total > 0 ? s.Total : s.Salary,
                EmployeePf = s.EmployeePf,
                EmployerPf = s.EmployerPf,
                NetPayable = s.NetPayable,
            }).ToList(), StringComparer.OrdinalIgnoreCase);

        var companyNames = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Company))
            .GroupBy(r => r.Company!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.GroupBy(x => x.Company!.Trim()).OrderByDescending(x => x.Count()).First().Key,
                StringComparer.OrdinalIgnoreCase);

        var today = DateTime.Today;
        var result = new List<Employee>(rows.Count);
        foreach (var r in rows.GroupBy(r => r.EmpCode, StringComparer.OrdinalIgnoreCase).Select(g => g.First()))
        {
            var exitAfterJoin = r.LastLeavingDate != null && (r.DateOfJoining == null || r.LastLeavingDate.Value.Date >= r.DateOfJoining.Value.Date);
            var empBands = bands.TryGetValue(r.EmpCode, out var b) ? b : new List<SalaryBand>();
            var e = new Employee
            {
                EmpCode = r.EmpCode,
                Name = Clean(r.Name),
                FatherName = Clean(r.FatherName),
                Gender = NormalizeGender(r.Sex),
                Dob = r.Dob is { Year: > 1900 } ? r.Dob : null,
                MaritalStatus = Clean(r.MaritalStatus),
                BloodGroup = Clean(r.BloodGroup),
                Mobile = Clean(r.Mobile),
                Email = Clean(r.Email),
                Qualification = Clean(r.Qualification),
                PermanentAddress = CleanAddress(r.PermanentAddress),
                PresentAddress = CleanAddress(r.PresentAddress),
                Company = string.IsNullOrWhiteSpace(r.Company) ? "—" : companyNames[r.Company.Trim()],
                Branch = OrDash(r.Branch),
                Department = OrDash(r.Department),
                Designation = OrDash(r.Designation),
                Category = Clean(r.Category),
                Contractor = Clean(r.Contractor),
                DateOfJoining = r.DateOfJoining is { Year: > 1950 } ? r.DateOfJoining.Value.Date : null,
                ConfirmationDate = r.ConfirmationDate is { Year: > 1950 } ? r.ConfirmationDate.Value.Date : null,
                Ctc = (decimal)r.Ctc,
                SalaryPerDay = (decimal)r.SalaryPerDay,
                PfApplicable = Clean(r.PfApplicable),
                PfAccount = Clean(r.PfAccount),
                Uan = CleanId(r.Uan),
                Esic = CleanId(r.Esic),
                Pan = CleanId(r.Pan),
                Aadhaar = CleanId(r.Aadhaar),
                BankName = Clean(r.BankName),
                BankAccount = CleanId(r.BankAccount),
                Ifsc = CleanId(r.Ifsc),
                ActiveFlag = r.ActiveFlag,
                IsCurrent = r.ActiveFlag && !exitAfterJoin,
                ExitDate = exitAfterJoin ? r.LastLeavingDate!.Value.Date : null,
                ExitReason = exitAfterJoin ? Clean(r.LeavingReason) : "",
                ExitRemarks = exitAfterJoin ? Clean(r.LeavingRemarks) : "",
                Salaries = empBands,
            };
            e.OriginState = OriginState.FromAddress(e.PermanentAddress) ?? OriginState.FromAddress(e.PresentAddress) ?? "Not recorded";
            e.CurrentSalary = empBands.Where(x => x.ToDate == null || x.ToDate >= today).OrderByDescending(x => x.FromDate).FirstOrDefault()
                              ?? empBands.LastOrDefault();
            (decimal? Amount, string Basis) monthly = e.CurrentSalary is { Monthly: > 0 } band
                ? (band.Monthly, "Salary master")
                : e.SalaryPerDay > 0
                    ? (Math.Round(e.SalaryPerDay * WageDaysPerMonth, 0), $"Per day × {WageDaysPerMonth}")
                    : e.Ctc > 0
                        ? (Math.Round(e.Ctc / 12, 0), "CTC ÷ 12")
                        : (null, "Not recorded");
            e.MonthlySalary = monthly.Amount;
            e.SalaryBasis = monthly.Basis;
            for (var i = empBands.Count - 1; i > 0; i--)
            {
                var diff = empBands[i].Monthly - empBands[i - 1].Monthly;
                if (diff <= 0) continue;
                e.LastIncrement = new Increment(empBands[i].FromDate, diff,
                    empBands[i - 1].Monthly > 0 ? Math.Round(diff / empBands[i - 1].Monthly * 100, 1) : null);
                break;
            }
            result.Add(e);
        }
        return result;
    }

    private static HrMasterEmployeeDto ToDto(Employee e)
    {
        var today = DateTime.Today;
        return new HrMasterEmployeeDto
        {
            EmpCode = e.EmpCode,
            Name = e.Name,
            FatherName = e.FatherName,
            Gender = e.Gender,
            Dob = e.Dob,
            Age = e.Age(today),
            Mobile = e.Mobile,
            OriginState = e.OriginState,
            PermanentAddress = e.PermanentAddress,
            Company = e.Company,
            Branch = e.Branch,
            Department = e.Department,
            Designation = e.Designation,
            DateOfJoining = e.DateOfJoining,
            TenureYears = e.TenureYears(e.ExitDate ?? today),
            Status = e.IsCurrent ? "Current" : "Left",
            ExitDate = e.ExitDate,
            ExitReason = e.ExitReason,
            MonthlySalary = e.MonthlySalary,
            SalaryBasis = e.SalaryBasis,
            Ctc = e.Ctc > 0 ? e.Ctc : null,
            SalaryPerDay = e.SalaryPerDay > 0 ? e.SalaryPerDay : null,
            LastIncrementDate = e.LastIncrement?.Date,
            LastIncrementAmount = e.LastIncrement?.Amount,
            LastIncrementPct = e.LastIncrement?.Pct,
            Revisions = e.Salaries.Count,
            PfApplicable = e.PfApplicable,
            PfAccount = e.PfAccount,
            Uan = e.Uan,
            Esic = e.Esic,
        };
    }

    private static void WriteHeadcountSheet(XLWorkbook wb, string title, string label, List<Employee> scoped, Func<Employee, string> key)
    {
        var ws = wb.AddWorksheet(title);
        var current = scoped.Where(e => e.IsCurrent).ToList();
        var companies = current.GroupBy(e => e.Company, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count()).Select(g => g.Key).ToList();
        var headers = new List<string> { label, "Total", "Male", "Female" };
        headers.AddRange(companies);
        headers.Add("Monthly salary");
        WriteHeader(ws, headers.ToArray());
        var r = 2;
        foreach (var g in current.GroupBy(key, StringComparer.OrdinalIgnoreCase).OrderByDescending(g => g.Count()))
        {
            var c = 1;
            ws.Cell(r, c++).Value = g.Key;
            ws.Cell(r, c++).Value = g.Count();
            ws.Cell(r, c++).Value = g.Count(e => e.Gender == "Male");
            ws.Cell(r, c++).Value = g.Count(e => e.Gender == "Female");
            foreach (var co in companies)
            {
                var n = g.Count(e => string.Equals(e.Company, co, StringComparison.OrdinalIgnoreCase));
                if (n > 0) ws.Cell(r, c).Value = n;
                c++;
            }
            SetMoney(ws.Cell(r, c), g.Sum(e => e.MonthlySalary ?? 0));
            r++;
        }
        ws.Cell(r, 1).Value = "Total";
        ws.Cell(r, 2).Value = current.Count;
        ws.Cell(r, 3).Value = current.Count(e => e.Gender == "Male");
        ws.Cell(r, 4).Value = current.Count(e => e.Gender == "Female");
        for (var i = 0; i < companies.Count; i++)
            ws.Cell(r, 5 + i).Value = current.Count(e => string.Equals(e.Company, companies[i], StringComparison.OrdinalIgnoreCase));
        SetMoney(ws.Cell(r, 5 + companies.Count), current.Sum(e => e.MonthlySalary ?? 0));
        ws.Row(r).Style.Font.SetBold();
        FinishTable(ws, r, headers.Count, autoFilter: false);
    }

    private static int WriteAttritionGroup(IXLWorksheet ws, int r, string label, List<HrAttritionGroupDto> groups)
    {
        var headers = new[] { label, "Current headcount", "Average headcount", "Joiners", "Leavers", "Attrition %" };
        for (var i = 0; i < headers.Length; i++) ws.Cell(r, i + 1).Value = headers[i];
        ws.Range(r, 1, r, headers.Length).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#E8EEF7"));
        r++;
        foreach (var g in groups)
        {
            ws.Cell(r, 1).Value = g.Name;
            ws.Cell(r, 2).Value = g.CurrentHeadcount;
            ws.Cell(r, 3).Value = g.AvgHeadcount;
            ws.Cell(r, 4).Value = g.Joiners;
            ws.Cell(r, 5).Value = g.Leavers;
            ws.Cell(r, 6).Value = g.RatePct;
            r++;
        }
        return r;
    }

    private static void WriteHeader(IXLWorksheet ws, string[] headers)
    {
        for (var i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];
        ws.Range(1, 1, 1, headers.Length).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#E8EEF7"));
        ws.SheetView.FreezeRows(1);
    }

    private static void FinishTable(IXLWorksheet ws, int lastRow, int columns, bool autoFilter = true)
    {
        if (autoFilter && lastRow >= 1) ws.Range(1, 1, Math.Max(1, lastRow), columns).SetAutoFilter();
        ws.Columns(1, columns).AdjustToContents(1, Math.Min(lastRow, 300), 8, 45);
    }

    private static void SetDate(IXLCell cell, DateTime? value)
    {
        if (value == null) return;
        cell.Value = value.Value;
        cell.Style.DateFormat.Format = "dd-mmm-yyyy";
    }

    private static void SetMoney(IXLCell cell, decimal? value)
    {
        if (value == null) return;
        cell.Value = value.Value;
        cell.Style.NumberFormat.Format = "#,##0";
    }

    private static void SetNum(IXLCell cell, decimal? value)
    {
        if (value == null) return;
        cell.Value = value.Value;
    }

    private static string Mask(string value)
    {
        var digits = value.Trim();
        if (digits.Length <= 4) return digits;
        return new string('X', digits.Length - 4) + digits[^4..];
    }

    private static string Clean(string? value)
    {
        var v = (value ?? "").Trim();
        return v.Equals("NULL", StringComparison.OrdinalIgnoreCase) || v.Trim('.', '-', '0', ' ') == "" ? "" : v;
    }

    private static string CleanId(string? value)
    {
        var v = Clean(value);
        return v.Trim('.', '-', ' ', '0') == "" || v.Equals("NA", StringComparison.OrdinalIgnoreCase) ? "" : v;
    }

    private static string CleanAddress(string? value) =>
        Regex.Replace(Clean(value), @"\s+", " ").Trim();

    private static string OrDash(string? value)
    {
        var v = Clean(value);
        return v == "" ? "—" : v;
    }

    private static string NormalizeGender(string? sex)
    {
        var s = (sex ?? "").Trim().ToUpperInvariant();
        return s.StartsWith('M') ? "Male" : s.StartsWith('F') ? "Female" : "Not recorded";
    }

    private sealed record Fy(DateTime Start, DateTime EndExclusive, string Label);

    private sealed class Snapshot
    {
        public Snapshot(DateTime loadedAt, List<Employee> employees)
        {
            LoadedAt = loadedAt;
            Employees = employees;
            CompanyNames = employees.Where(e => e.IsCurrent).Select(e => e.Company)
                .Where(c => c != "—").Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public DateTime LoadedAt { get; }
        public List<Employee> Employees { get; }
        public IReadOnlyList<string> CompanyNames { get; }
        public ConcurrentDictionary<string, Lazy<ScopeStats>> Stats { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed record ScopeStats(
        List<Employee> Scoped,
        HrMasterSummaryDto Summary,
        List<HrHeadcountRowDto> Designations,
        List<HrHeadcountRowDto> Departments,
        List<HrHeadcountRowDto> Companies,
        List<HrHeadcountRowDto> OriginStates,
        List<HrAttritionMonthDto> Attrition,
        List<HrAttritionGroupDto> AttritionByCompany,
        List<HrAttritionGroupDto> AttritionByDepartment);

    private sealed record Increment(DateTime Date, decimal Amount, decimal? Pct);

    private sealed class SalaryBand
    {
        public DateTime FromDate { get; init; }
        public DateTime? ToDate { get; init; }
        public decimal BasicDa { get; init; }
        public decimal Hra { get; init; }
        public decimal Gross { get; init; }
        public decimal Monthly { get; init; }
        public decimal EmployeePf { get; init; }
        public decimal EmployerPf { get; init; }
        public decimal NetPayable { get; init; }
    }

    private sealed class Employee
    {
        public string EmpCode { get; init; } = "";
        public string Name { get; init; } = "";
        public string FatherName { get; init; } = "";
        public string Gender { get; init; } = "";
        public DateTime? Dob { get; init; }
        public string MaritalStatus { get; init; } = "";
        public string BloodGroup { get; init; } = "";
        public string Mobile { get; init; } = "";
        public string Email { get; init; } = "";
        public string Qualification { get; init; } = "";
        public string PermanentAddress { get; init; } = "";
        public string PresentAddress { get; init; } = "";
        public string OriginState { get; set; } = "";
        public string Company { get; init; } = "";
        public string Branch { get; init; } = "";
        public string Department { get; init; } = "";
        public string Designation { get; init; } = "";
        public string Category { get; init; } = "";
        public string Contractor { get; init; } = "";
        public DateTime? DateOfJoining { get; init; }
        public DateTime? ConfirmationDate { get; init; }
        public decimal Ctc { get; init; }
        public decimal SalaryPerDay { get; init; }
        public string PfApplicable { get; init; } = "";
        public string PfAccount { get; init; } = "";
        public string Uan { get; init; } = "";
        public string Esic { get; init; } = "";
        public string Pan { get; init; } = "";
        public string Aadhaar { get; init; } = "";
        public string BankName { get; init; } = "";
        public string BankAccount { get; init; } = "";
        public string Ifsc { get; init; } = "";
        public bool ActiveFlag { get; init; }
        public bool IsCurrent { get; init; }
        public DateTime? ExitDate { get; init; }
        public string ExitReason { get; init; } = "";
        public string ExitRemarks { get; init; } = "";
        public List<SalaryBand> Salaries { get; init; } = new();
        public SalaryBand? CurrentSalary { get; set; }
        public decimal? MonthlySalary { get; set; }
        public string SalaryBasis { get; set; } = "";
        public Increment? LastIncrement { get; set; }

        public decimal? Age(DateTime at) =>
            Dob == null ? null : Math.Round((decimal)((at - Dob.Value).TotalDays / 365.25), 1);

        public decimal? TenureYears(DateTime at) =>
            DateOfJoining == null || at < DateOfJoining ? null : Math.Round((decimal)((at - DateOfJoining.Value).TotalDays / 365.25), 1);
    }

    private sealed class EmpRow
    {
        public string EmpCode { get; set; } = "";
        public string? Name { get; set; }
        public string? FatherName { get; set; }
        public string? Sex { get; set; }
        public DateTime? Dob { get; set; }
        public string? MaritalStatus { get; set; }
        public string? BloodGroup { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? Qualification { get; set; }
        public string? PermanentAddress { get; set; }
        public string? PresentAddress { get; set; }
        public string? Company { get; set; }
        public string? Branch { get; set; }
        public string? Department { get; set; }
        public string? Designation { get; set; }
        public string? Category { get; set; }
        public string? Contractor { get; set; }
        public DateTime? DateOfJoining { get; set; }
        public DateTime? ConfirmationDate { get; set; }
        public double Ctc { get; set; }
        public double SalaryPerDay { get; set; }
        public string? PfApplicable { get; set; }
        public string? PfAccount { get; set; }
        public string? Uan { get; set; }
        public string? Esic { get; set; }
        public string? Pan { get; set; }
        public string? Aadhaar { get; set; }
        public string? BankName { get; set; }
        public string? BankAccount { get; set; }
        public string? Ifsc { get; set; }
        public bool ActiveFlag { get; set; }
        public DateTime? LastLeavingDate { get; set; }
        public string? LeavingReason { get; set; }
        public string? LeavingRemarks { get; set; }
    }

    private sealed class SalaryRow
    {
        public string EmpCode { get; set; } = "";
        public DateTime FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public decimal Salary { get; set; }
        public decimal BasicDa { get; set; }
        public decimal Hra { get; set; }
        public decimal Gross { get; set; }
        public decimal Total { get; set; }
        public decimal EmployeePf { get; set; }
        public decimal EmployerPf { get; set; }
        public decimal NetPayable { get; set; }
    }
}

/// <summary>Best-effort home state from free-text ERP addresses: state names, common abbreviations, districts, then PIN code.</summary>
internal static class OriginState
{
    private static readonly (string State, string[] Words)[] Names =
    {
        ("Gujarat", new[] { "GUJARAT", "GUJRAT" }),
        ("Uttar Pradesh", new[] { "UTTAR PRADESH", "UTTARPRADESH", "U P", "UP" }),
        ("Bihar", new[] { "BIHAR" }),
        ("Jharkhand", new[] { "JHARKHAND", "JHARKHAN" }),
        ("West Bengal", new[] { "WEST BENGAL", "W B", "WB" }),
        ("Odisha", new[] { "ODISHA", "ORISSA", "ODISA" }),
        ("Madhya Pradesh", new[] { "MADHYA PRADESH", "MADHYAPRADESH", "M P", "MP" }),
        ("Rajasthan", new[] { "RAJASTHAN", "RAJSTHAN" }),
        ("Maharashtra", new[] { "MAHARASHTRA", "MAHARASTRA" }),
        ("Chhattisgarh", new[] { "CHHATTISGARH", "CHATTISGARH", "CHHATISGARH", "C G", "CG" }),
        ("Uttarakhand", new[] { "UTTARAKHAND", "UTTRAKHAND" }),
        ("Assam", new[] { "ASSAM" }),
        ("Haryana", new[] { "HARYANA" }),
        ("Punjab", new[] { "PUNJAB" }),
        ("Delhi", new[] { "DELHI" }),
        ("Nepal", new[] { "NEPAL" }),
        ("Karnataka", new[] { "KARNATAKA" }),
        ("Tamil Nadu", new[] { "TAMIL NADU", "TAMILNADU" }),
        ("Kerala", new[] { "KERALA" }),
        ("Andhra Pradesh", new[] { "ANDHRA PRADESH", "ANDHRA" }),
        ("Telangana", new[] { "TELANGANA" }),
        ("Himachal Pradesh", new[] { "HIMACHAL" }),
        ("Jammu & Kashmir", new[] { "JAMMU", "KASHMIR" }),
        ("Tripura", new[] { "TRIPURA" }),
        ("Manipur", new[] { "MANIPUR" }),
    };

    private static readonly (string State, string[] Words)[] Districts =
    {
        ("Gujarat", new[] { "MAHESANA", "MEHSANA", "MAHSANA", "MEHESANA", "SABARKANTHA", "BANASKANTHA", "AHMEDABAD", "AHMADABAD", "GANDHINAGAR", "KADI", "KALOL", "PATAN", "HIMATNAGAR", "VADODARA", "BARODA", "SURAT", "RAJKOT", "ANAND", "KHEDA", "PANCHMAHAL", "DAHOD", "KUTCH", "KACHCHH", "BHAVNAGAR", "JAMNAGAR", "JUNAGADH", "AMRELI", "ARAVALLI", "MODASA", "VIRAMGAM", "SANAND", "CHHOTA UDEPUR", "NARMADA", "BHARUCH", "VALSAD", "NAVSARI", "TAPI", "MORBI", "SURENDRANAGAR", "BOTAD", "MAHISAGAR", "DEESA", "PALANPUR", "UNJHA", "VISNAGAR", "VIJAPUR", "BECHRAJI", "JOTANA" }),
        ("Uttar Pradesh", new[] { "ALLAHABAD", "PRAYAGRAJ", "GHAZIPUR", "GAZIPUR", "GORAKHPUR", "VARANASI", "AZAMGARH", "JAUNPUR", "MIRZAPUR", "MIRJAPUR", "BALLIA", "BALIA", "DEORIA", "KUSHINAGAR", "PRATAPGARH", "SULTANPUR", "LUCKNOW", "KANPUR", "AGRA", "BASTI", "GONDA", "BAHRAICH", "MAU", "CHANDAULI", "BHADOHI", "FAIZABAD", "AYODHYA", "AMBEDKAR NAGAR", "SIDDHARTH NAGAR", "MAHARAJGANJ", "SANT KABIR NAGAR", "BARABANKI", "RAEBARELI", "UNNAO", "HARDOI", "SITAPUR", "KHERI", "BAREILLY", "MORADABAD", "MEERUT", "ALIGARH", "MATHURA", "ETAWAH", "JHANSI", "BANDA", "FATEHPUR", "KAUSHAMBI", "SONBHADRA" }),
        ("Bihar", new[] { "PATNA", "GAYA", "SIWAN", "CHHAPRA", "CHAPRA", "SARAN", "GOPALGANJ", "MUZAFFARPUR", "DARBHANGA", "BHAGALPUR", "BEGUSARAI", "SAMASTIPUR", "NALANDA", "ROHTAS", "SASARAM", "BUXAR", "ARRAH", "ARA", "BHOJPUR", "VAISHALI", "HAJIPUR", "MOTIHARI", "CHAMPARAN", "BETTIAH", "SITAMARHI", "MADHUBANI", "KATIHAR", "PURNIA", "PURNEA", "ARARIA", "KISHANGANJ", "NAWADA", "JAMUI", "SUPAUL", "SAHARSA", "MADHEPURA", "KAIMUR", "BHABUA", "JEHANABAD", "ARWAL", "SHEIKHPURA", "LAKHISARAI", "MUNGER", "KHAGARIA", "BANKA", "SHEOHAR" }),
        ("Jharkhand", new[] { "RANCHI", "DHANBAD", "BOKARO", "HAZARIBAGH", "HAZARIBAG", "GIRIDIH", "DEOGHAR", "DUMKA", "PALAMU", "PALAMAU", "GARHWA", "GODDA", "SAHIBGANJ", "SAHEBGANJ", "PAKUR", "JAMSHEDPUR", "CHATRA", "KODERMA", "LATEHAR", "LOHARDAGA", "GUMLA", "SIMDEGA", "KHUNTI", "CHAIBASA", "JAMTARA", "RAMGARH" }),
        ("West Bengal", new[] { "KOLKATA", "CALCUTTA", "COOCH BEHAR", "COOCHBEHAR", "COCHBEHAR", "JALPAIGURI", "MALDA", "MALDAH", "MURSHIDABAD", "DINAJPUR", "DARJEELING", "SILIGURI", "HOWRAH", "HOOGHLY", "NADIA", "BANKURA", "PURULIA", "BIRBHUM", "MEDINIPUR", "MIDNAPORE", "ALIPURDUAR" }),
        ("Odisha", new[] { "GANJAM", "CUTTACK", "BALASORE", "BALESHWAR", "KHORDHA", "KHURDA", "MAYURBHANJ", "BHUBANESWAR", "PURI", "KENDRAPARA", "JAJPUR", "BHADRAK", "KEONJHAR", "SAMBALPUR", "BOLANGIR", "KALAHANDI", "KORAPUT", "SUNDARGARH", "ROURKELA", "NAYAGARH", "GAJAPATI" }),
        ("Madhya Pradesh", new[] { "INDORE", "BHOPAL", "JHABUA", "ALIRAJPUR", "RATLAM", "UJJAIN", "REWA", "SATNA", "SIDHI", "SINGRAULI", "GWALIOR", "JABALPUR", "MANDSAUR", "NEEMUCH", "KHARGONE", "BARWANI", "KHANDWA", "SAGAR", "CHHATARPUR", "TIKAMGARH", "PANNA", "SHAHDOL", "DEWAS", "SHAJAPUR" }),
        ("Rajasthan", new[] { "BANSWARA", "DUNGARPUR", "UDAIPUR", "JAIPUR", "JODHPUR", "PALI", "SIROHI", "JALORE", "JALOR", "BARMER", "AJMER", "BHILWARA", "CHITTORGARH", "RAJSAMAND", "PRATAPGARH RAJ", "KOTA", "BIKANER", "NAGAUR", "SIKAR", "JHUNJHUNU", "ALWAR", "BHARATPUR", "TONK" }),
        ("Maharashtra", new[] { "MUMBAI", "PUNE", "NASHIK", "NASIK", "NAGPUR", "THANE", "JALGAON", "DHULE", "NANDURBAR", "AURANGABAD MH", "SOLAPUR", "KOLHAPUR", "AMRAVATI", "AKOLA", "NANDED", "LATUR" }),
        ("Chhattisgarh", new[] { "RAIPUR", "BILASPUR", "DURG", "BHILAI", "RAJNANDGAON", "KORBA", "RAIGARH", "JANJGIR", "SURGUJA", "AMBIKAPUR", "BASTAR", "JAGDALPUR" }),
        ("Uttarakhand", new[] { "DEHRADUN", "HARIDWAR", "HALDWANI", "NAINITAL", "UDHAM SINGH NAGAR", "ALMORA", "PITHORAGARH" }),
        ("Assam", new[] { "GUWAHATI", "DHUBRI", "KOKRAJHAR", "BARPETA", "NAGAON", "GOALPARA" }),
    };

    private static readonly (string Prefix, string State)[] Pin =
    {
        ("11", "Delhi"), ("12", "Haryana"), ("13", "Haryana"), ("14", "Punjab"), ("15", "Punjab"), ("16", "Punjab"),
        ("17", "Himachal Pradesh"), ("18", "Jammu & Kashmir"), ("19", "Jammu & Kashmir"),
        ("20", "Uttar Pradesh"), ("21", "Uttar Pradesh"), ("22", "Uttar Pradesh"), ("23", "Uttar Pradesh"),
        ("24", "Uttarakhand"), ("25", "Uttar Pradesh"), ("26", "Uttar Pradesh"), ("27", "Uttar Pradesh"), ("28", "Uttar Pradesh"),
        ("30", "Rajasthan"), ("31", "Rajasthan"), ("32", "Rajasthan"), ("33", "Rajasthan"), ("34", "Rajasthan"),
        ("36", "Gujarat"), ("37", "Gujarat"), ("38", "Gujarat"), ("39", "Gujarat"),
        ("40", "Maharashtra"), ("41", "Maharashtra"), ("42", "Maharashtra"), ("43", "Maharashtra"), ("44", "Maharashtra"),
        ("45", "Madhya Pradesh"), ("46", "Madhya Pradesh"), ("47", "Madhya Pradesh"), ("48", "Madhya Pradesh"), ("49", "Chhattisgarh"),
        ("50", "Telangana"), ("51", "Andhra Pradesh"), ("52", "Andhra Pradesh"), ("53", "Andhra Pradesh"),
        ("56", "Karnataka"), ("57", "Karnataka"), ("58", "Karnataka"), ("59", "Karnataka"),
        ("60", "Tamil Nadu"), ("61", "Tamil Nadu"), ("62", "Tamil Nadu"), ("63", "Tamil Nadu"), ("64", "Tamil Nadu"),
        ("67", "Kerala"), ("68", "Kerala"), ("69", "Kerala"),
        ("70", "West Bengal"), ("71", "West Bengal"), ("72", "West Bengal"), ("73", "West Bengal"), ("74", "West Bengal"),
        ("75", "Odisha"), ("76", "Odisha"), ("77", "Odisha"), ("78", "Assam"),
        ("80", "Bihar"), ("81", "Bihar"), ("82", "Bihar"), ("83", "Jharkhand"), ("84", "Bihar"), ("85", "Bihar"),
    };

    private static readonly Regex NonWord = new(@"[^A-Z0-9]+", RegexOptions.Compiled);
    private static readonly Regex PinCode = new(@"(?<!\d)([1-8]\d{2})\s?(\d{3})(?!\d)", RegexOptions.Compiled);

    public static string? FromAddress(string? address)
    {
        if (string.IsNullOrWhiteSpace(address)) return null;
        var upper = address.ToUpperInvariant();
        var text = " " + NonWord.Replace(upper, " ").Trim() + " ";
        if (text.Trim().Length < 3) return null;

        foreach (var (state, words) in Names)
            if (words.Any(w => text.Contains(" " + w + " ", StringComparison.Ordinal)))
                return state;

        var compact = " " + NonWord.Replace(upper.Replace(".", ""), " ").Trim() + " ";
        foreach (var (state, words) in Names)
            if (words.Any(w => w.Length <= 3 && compact.Contains(" " + w.Replace(" ", "") + " ", StringComparison.Ordinal)))
                return state;

        foreach (var (state, words) in Districts)
            if (words.Any(w => text.Contains(" " + w + " ", StringComparison.Ordinal)))
                return state;

        var pin = PinCode.Match(upper);
        if (pin.Success)
        {
            var prefix = pin.Groups[1].Value[..2];
            foreach (var (p, state) in Pin)
                if (p == prefix) return state;
        }
        return null;
    }
}

public sealed class HrMasterQuery
{
    public string? Company { get; set; }
    public string? Branch { get; set; }
    public string? Status { get; set; }
    public string? Designation { get; set; }
    public string? Department { get; set; }
    public string? Search { get; set; }
    public int? FyStartYear { get; set; }
}

public sealed class HrMasterReportDto
{
    public DateTime GeneratedAt { get; set; }
    public DateTime DataAsOf { get; set; }
    public string FyLabel { get; set; } = "";
    public HrMasterSummaryDto Summary { get; set; } = new();
    public List<HrHeadcountRowDto> Designations { get; set; } = new();
    public List<HrHeadcountRowDto> Departments { get; set; } = new();
    public List<HrHeadcountRowDto> Companies { get; set; } = new();
    public List<HrHeadcountRowDto> OriginStates { get; set; } = new();
    public List<HrAttritionMonthDto> Attrition { get; set; } = new();
    public List<HrAttritionGroupDto> AttritionByCompany { get; set; } = new();
    public List<HrAttritionGroupDto> AttritionByDepartment { get; set; } = new();
    public int EmployeeTotal { get; set; }
    public List<HrMasterEmployeeDto> Employees { get; set; } = new();
}

public sealed class HrMasterSummaryDto
{
    public int CurrentHeadcount { get; set; }
    public int Male { get; set; }
    public int Female { get; set; }
    public int OtherGender { get; set; }
    public int JoinersFy { get; set; }
    public int LeaversFy { get; set; }
    public decimal AttritionFyPct { get; set; }
    public decimal AvgTenureYears { get; set; }
    public decimal AvgAgeYears { get; set; }
    public int PfCovered { get; set; }
    public int EsicCovered { get; set; }
    public decimal MonthlySalaryTotal { get; set; }
    public int LeftFlaggedActive { get; set; }
}

public sealed class HrHeadcountRowDto
{
    public string Name { get; set; } = "";
    public int Count { get; set; }
    public int Male { get; set; }
    public int Female { get; set; }
    public decimal MonthlySalary { get; set; }
    public List<HrNameCountDto> ByCompany { get; set; } = new();
}

public sealed class HrNameCountDto
{
    public string Name { get; set; } = "";
    public int Count { get; set; }
}

public sealed class HrAttritionMonthDto
{
    public string Month { get; set; } = "";
    public string Label { get; set; } = "";
    public int Opening { get; set; }
    public int Joiners { get; set; }
    public int Leavers { get; set; }
    public int Closing { get; set; }
    public double AvgHeadcount { get; set; }
    public decimal RatePct { get; set; }
}

public sealed class HrAttritionGroupDto
{
    public string Name { get; set; } = "";
    public int CurrentHeadcount { get; set; }
    public double AvgHeadcount { get; set; }
    public int Joiners { get; set; }
    public int Leavers { get; set; }
    public decimal RatePct { get; set; }
}

public sealed class HrMasterEmployeeDto
{
    public string EmpCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string FatherName { get; set; } = "";
    public string Gender { get; set; } = "";
    public DateTime? Dob { get; set; }
    public decimal? Age { get; set; }
    public string Mobile { get; set; } = "";
    public string OriginState { get; set; } = "";
    public string PermanentAddress { get; set; } = "";
    public string Company { get; set; } = "";
    public string Branch { get; set; } = "";
    public string Department { get; set; } = "";
    public string Designation { get; set; } = "";
    public DateTime? DateOfJoining { get; set; }
    public decimal? TenureYears { get; set; }
    public string Status { get; set; } = "";
    public DateTime? ExitDate { get; set; }
    public string ExitReason { get; set; } = "";
    public decimal? MonthlySalary { get; set; }
    public string SalaryBasis { get; set; } = "";
    public decimal? Ctc { get; set; }
    public decimal? SalaryPerDay { get; set; }
    public DateTime? LastIncrementDate { get; set; }
    public decimal? LastIncrementAmount { get; set; }
    public decimal? LastIncrementPct { get; set; }
    public int Revisions { get; set; }
    public string PfApplicable { get; set; } = "";
    public string PfAccount { get; set; } = "";
    public string Uan { get; set; } = "";
    public string Esic { get; set; } = "";
}
