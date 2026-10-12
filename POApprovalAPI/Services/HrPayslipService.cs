using System.Globalization;
using Dapper;

namespace POApprovalAPI.Services;

/// <summary>
/// Salary slips from the frozen (finalised) payroll in Loginentry @ 3445:
/// <list type="bullet">
/// <item><c>FreezeSalary</c> — monthly staff. Net = Total − TotalDeduc.</item>
/// <item><c>freezedaywithPFraj</c> / <c>freezedaywithpfoel</c> — per-day workers. Net = TotalAmount − statutory/other deductions − Advance.</item>
/// </list>
/// The ERP totals are authoritative; components that do not add up to them are shown as a balancing "other" line.
/// </summary>
public sealed class HrPayslipService
{
    private static readonly CultureInfo In = CultureInfo.GetCultureInfo("en-IN");
    private readonly DatabaseService _database;
    private readonly HrEmployeeDocumentService _documents;

    public HrPayslipService(DatabaseService database, HrEmployeeDocumentService documents)
    {
        _database = database;
        _documents = documents;
    }

    public async Task<List<string>> GetMonthsAsync(string empCode)
    {
        empCode = Normalize(empCode);
        using var connection = _database.CreatePayrollLoginEntryConnection();
        var rows = await connection.QueryAsync<int>(@"
SELECT DISTINCT TOP 36 ym FROM (
    SELECT Yr * 100 + Mnth AS ym FROM FreezeSalary WITH (NOLOCK) WHERE LTRIM(RTRIM(empcode)) = @EmpCode
    UNION SELECT Yr * 100 + Mnth FROM freezedaywithPFraj WITH (NOLOCK) WHERE LTRIM(RTRIM(EmpCode)) = @EmpCode
    UNION SELECT Yr * 100 + mnth FROM freezedaywithpfoel WITH (NOLOCK) WHERE LTRIM(RTRIM(EmpCode)) = @EmpCode
) m
ORDER BY ym DESC", new { EmpCode = empCode }, commandTimeout: 60);
        return rows.Select(ym => $"{ym / 100:D4}-{ym % 100:D2}").ToList();
    }

    public async Task<HrPayslipDto> GetPayslipAsync(string empCode, string yearMonth)
    {
        empCode = Normalize(empCode);
        var (year, month) = ParseYearMonth(yearMonth);
        using var connection = _database.CreatePayrollLoginEntryConnection();
        var p = new { EmpCode = empCode, Yr = year, Mnth = month };

        var emp = await connection.QueryFirstOrDefaultAsync<EmpRow>(@"
SELECT TOP 1
    LTRIM(RTRIM(EmpCode)) AS EmpCode, LTRIM(RTRIM(Name)) AS Name, LTRIM(RTRIM(Fathername)) AS FatherName,
    LTRIM(RTRIM(Designation)) AS Designation, LTRIM(RTRIM(Deptt)) AS Department,
    LTRIM(RTRIM(CompanyName)) AS Company, LTRIM(RTRIM(Branch)) AS Branch, DateOJ AS DateOfJoining,
    LTRIM(RTRIM(PFaccount)) AS PfAccount, LTRIM(RTRIM(UANNo)) AS Uan, LTRIM(RTRIM(ESICNo)) AS Esic,
    LTRIM(RTRIM(pan_no)) AS Pan, LTRIM(RTRIM(bank_name)) AS BankName, LTRIM(RTRIM(BankNo)) AS BankAccount,
    LTRIM(RTRIM(IFSCCode)) AS Ifsc
FROM empinfo WITH (NOLOCK)
WHERE LTRIM(RTRIM(EmpCode)) = @EmpCode", p, commandTimeout: 30)
            ?? throw new InvalidOperationException($"Employee {empCode} not found in payroll.");

        var slip = await MonthlyAsync(connection, p)
                   ?? await PerDayRajAsync(connection, p)
                   ?? await PerDayOelAsync(connection, p)
                   ?? throw new KeyNotFoundException(
                       $"No finalised salary for {empCode} in {new DateTime(year, month, 1):MMMM yyyy}. Salary slips appear once HR freezes the month's payroll.");

        Balance(slip.Earnings, slip.GrossEarnings, "Other earnings / adjustments");
        Balance(slip.Deductions, slip.TotalDeductions, "Other deductions");

        var photo = await _documents.GetPhotoAsync(empCode);

        slip.YearMonth = $"{year:D4}-{month:D2}";
        slip.PeriodLabel = new DateTime(year, month, 1).ToString("MMMM yyyy", In);
        slip.Employee = new HrPayslipEmployeeDto
        {
            EmpCode = emp.EmpCode ?? empCode,
            Name = emp.Name ?? "",
            FatherName = Blank(emp.FatherName),
            Designation = Blank(emp.Designation),
            Department = Blank(emp.Department) ?? Blank(slip.Department),
            Company = Blank(slip.Company) ?? Blank(emp.Company),
            Branch = Blank(slip.Branch) ?? Blank(emp.Branch),
            DateOfJoining = emp.DateOfJoining?.ToString("dd-MMM-yyyy", In),
            PfAccount = Blank(emp.PfAccount),
            Uan = Blank(emp.Uan),
            Esic = Blank(emp.Esic),
            Pan = Blank(emp.Pan),
            BankName = Blank(emp.BankName),
            BankAccountMasked = MaskAccount(emp.BankAccount),
            Ifsc = Blank(emp.Ifsc),
        };
        slip.PhotoDataUrl = photo is null ? null : $"data:{photo.Value.ContentType};base64,{Convert.ToBase64String(photo.Value.Content)}";
        slip.NetPayInWords = RupeesInWords(slip.NetPay);
        slip.GeneratedAt = DateTime.Now;
        return slip;
    }

    public async Task<byte[]?> GetPhotoBytesAsync(string empCode) =>
        (await _documents.GetPhotoAsync(Normalize(empCode)))?.Content;

    private static async Task<HrPayslipDto?> MonthlyAsync(System.Data.IDbConnection c, object p)
    {
        var r = await c.QueryFirstOrDefaultAsync<dynamic>(@"
SELECT TOP 1 * FROM FreezeSalary WITH (NOLOCK)
WHERE LTRIM(RTRIM(empcode)) = @EmpCode AND Yr = @Yr AND Mnth = @Mnth
ORDER BY transid DESC", p, commandTimeout: 30);
        if (r is null) return null;
        var d = (IDictionary<string, object?>)r;

        var slip = new HrPayslipDto
        {
            PayType = "Monthly",
            Company = Str(d, "CompName"),
            Branch = Str(d, "Branch"),
            Department = Str(d, "Deptt"),
            DaysInMonth = Num(d, "DayinMonth"),
            PaidDays = Num(d, "Present"),
            PaymentMode = Str(d, "BankCash"),
            GrossEarnings = Num(d, "Total"),
            TotalDeductions = Num(d, "TotalDeduc"),
            NetPay = Num(d, "Netpayable"),
        };
        Add(slip.Earnings, d,
            ("Basic", "Basic"), ("House Rent Allowance", "HRA"), ("Conveyance Allowance", "ConvAll"),
            ("Transport Allowance", "TransportAll"), ("Medical Allowance", "MedicalAll"), ("City Allowance", "CityAll"),
            ("CCA", "CCAAll"), ("Special Allowance", "SpAll"), ("Uniform Allowance", "UniformAll"),
            ("Hostel Allowance", "HostalAll"), ("Books Allowance", "BookAll"), ("Children Allowance", "ChildAll"),
            ("Education Allowance", "Educ"), ("LTC", "LTC"), ("Driver Allowance", "DriverAll"),
            ("Other Allowance", "Otherall"), ("Other Allowance 2", "OtherAll1"), ("Fixed Allowance", "FixAll"),
            ("Uniform Maintenance", "UniformMain"), ("Vehicle Maintenance", "VehicalMain"),
            ("Tea Allowance", "TeaAllow"), ("Washing Allowance", "WashingAllow"), ("Canteen Allowance", "CanteenAllow"),
            ("Incentive", "Incentive"), ("Additional Salary", "addsalary"), ("Additional Pay", "Addextra1"));
        Add(slip.Deductions, d,
            ("Provident Fund", "EmployerPF"), ("ESIC", "ESICDeduc"), ("Professional Tax", "ProfTax"), ("Income Tax (TDS)", "TDS"),
            ("Advance", "Advance"), ("Loan Recovery", "LoanDeduc"), ("Fine", "Fine"), ("Bond", "Bond"),
            ("Medical Recovery", "medical"), ("Store Recovery", "Store"), ("Other Deduction", "OtherDeduc"),
            ("Other Deduction 2", "OtherDeduc1"), ("Other Recovery", "OtherD"), ("Other Recovery 2", "OtherD1"));
        Info(slip.Notes, d, ("Leave / absence deduction (already reflected in earnings)", "PLDeduc"), ("Bonus accrued (paid annually)", "Bonus"));
        return slip;
    }

    private static async Task<HrPayslipDto?> PerDayRajAsync(System.Data.IDbConnection c, object p)
    {
        var r = await c.QueryFirstOrDefaultAsync<dynamic>(@"
SELECT TOP 1 * FROM freezedaywithPFraj WITH (NOLOCK)
WHERE LTRIM(RTRIM(EmpCode)) = @EmpCode AND Yr = @Yr AND Mnth = @Mnth", p, commandTimeout: 30);
        return r is null ? null : PerDay((IDictionary<string, object?>)r, "ESIC", "OverTime", ("Bed & Pillow", "BedPillow"), ("Extra", "Extra"), ("Grade Bonus", "GradeBonus"), ("Extra Incentive", "ExtraIncentive"), ("Additional Salary", "Addsalary"));
    }

    private static async Task<HrPayslipDto?> PerDayOelAsync(System.Data.IDbConnection c, object p)
    {
        var r = await c.QueryFirstOrDefaultAsync<dynamic>(@"
SELECT TOP 1 * FROM freezedaywithpfoel WITH (NOLOCK)
WHERE LTRIM(RTRIM(EmpCode)) = @EmpCode AND Yr = @Yr AND mnth = @Mnth", p, commandTimeout: 30);
        if (r is null) return null;
        var d = (IDictionary<string, object?>)r;
        var slip = PerDay(d, "ESICDeduc", "OverTime", ("Special Allowance", "SpAll"), ("City Allowance", "CityAll"), ("Tea Allowance", "TeaAllow"), ("Incentive (fixed)", "incentive"));
        Add(slip.Deductions, d, ("Loan Recovery", "LoanDeduc"), ("Other Deduction", "OtherDeduc1"), ("Extra Deduction", "extradeduc"), ("Penalty", "Panishment"));
        return slip;
    }

    private static HrPayslipDto PerDay(IDictionary<string, object?> d, string esicCol, string overtimeCol, params (string Label, string Col)[] extraEarnings)
    {
        var gross = Num(d, "TotalAmount");
        var net = Num(d, "NetPayable");
        var slip = new HrPayslipDto
        {
            PayType = "Per day",
            Branch = Str(d, "branch"),
            DaysInMonth = Num(d, "DayInMonth"),
            PaidDays = Num(d, "TotalPresent"),
            WorkingDays = NumOrNull(d, "WorkingDay"),
            LeaveDays = NumOrNull(d, "Leave"),
            OvertimeHours = NumOrNull(d, overtimeCol),
            RatePerDay = NumOrNull(d, "SalaryPerDay"),
            GrossEarnings = gross,
            TotalDeductions = gross - net,
            NetPay = net,
        };
        Add(slip.Earnings, d,
            ("Basic", "Basic"), ("House Rent Allowance", "Hra"), ("Conveyance Allowance", "Con"),
            ("Medical Allowance", "Medical"), ("Uniform Allowance", "Uniform"));
        Info(slip.Notes, d,
            [("Incentive", "IncentiveAmount"), ("Attendance bonus", "Attendancebonus"), ("Misc earnings", "Misc"),
             ("Washing allowance", "WashingAllow"), ("Canteen allowance", "CanteenAllow"), ("Additional pay", "Addextra1"),
             .. extraEarnings]);
        Add(slip.Deductions, d,
            ("Provident Fund", "PFAmount"), ("ESIC", esicCol), ("Professional Tax", "ProfTax"), ("Canteen Coupon", "Coupon"),
            ("Fine", "Fine"), ("Bond", "Bond"), ("Medical Recovery", "medicaldec"), ("Store Recovery", "Store"), ("Advance", "Advance"));
        return slip;
    }

    private static void Add(List<HrPayslipLineDto> lines, IDictionary<string, object?> d, params (string Label, string Col)[] cols)
    {
        foreach (var (label, col) in cols)
        {
            var v = Num(d, col);
            if (Math.Abs(v) >= 0.5m) lines.Add(new HrPayslipLineDto(label, Math.Round(v, 2)));
        }
    }

    private static void Info(List<HrPayslipLineDto> lines, IDictionary<string, object?> d, params (string Label, string Col)[] cols) =>
        Add(lines, d, cols);

    /// <summary>Make the listed lines add up to the ERP total.</summary>
    private static void Balance(List<HrPayslipLineDto> lines, decimal total, string label)
    {
        var diff = Math.Round(total - lines.Sum(l => l.Amount), 2);
        if (Math.Abs(diff) >= 1m) lines.Add(new HrPayslipLineDto(label, diff));
    }

    private static decimal Num(IDictionary<string, object?> d, string col) => NumOrNull(d, col) ?? 0m;

    private static decimal? NumOrNull(IDictionary<string, object?> d, string col)
    {
        var key = d.Keys.FirstOrDefault(k => string.Equals(k, col, StringComparison.OrdinalIgnoreCase));
        if (key is null || d[key] is null || d[key] is DBNull) return null;
        return Convert.ToDecimal(d[key], CultureInfo.InvariantCulture);
    }

    private static string? Str(IDictionary<string, object?> d, string col)
    {
        var key = d.Keys.FirstOrDefault(k => string.Equals(k, col, StringComparison.OrdinalIgnoreCase));
        return key is null ? null : Blank(d[key]?.ToString());
    }

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) || s.Trim() == "0" ? null : s.Trim();

    private static string? MaskAccount(string? acct)
    {
        acct = Blank(acct);
        if (acct is null) return null;
        return acct.Length <= 4 ? acct : new string('X', acct.Length - 4) + acct[^4..];
    }

    private static string Normalize(string empCode)
    {
        var code = (empCode ?? "").Trim();
        if (code.Length == 0 || code.Length > 30)
            throw new InvalidOperationException("Employee code is required.");
        return code;
    }

    private static (int Year, int Month) ParseYearMonth(string yearMonth)
    {
        if (!DateTime.TryParseExact(yearMonth, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            throw new InvalidOperationException("Month must be in YYYY-MM format.");
        return (d.Year, d.Month);
    }

    public static string RupeesInWords(decimal amount)
    {
        var rupees = (long)Math.Floor(Math.Abs(amount));
        var paise = (int)Math.Round((Math.Abs(amount) - rupees) * 100);
        var words = rupees == 0 ? "Zero" : IndianWords(rupees);
        var result = $"Rupees {words}";
        if (paise > 0) result += $" and {IndianWords(paise)} Paise";
        return (amount < 0 ? "Minus " : "") + result + " Only";
    }

    private static readonly string[] Ones =
    [
        "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve",
        "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen",
    ];
    private static readonly string[] Tens = ["", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"];

    private static string IndianWords(long n)
    {
        var parts = new List<string>();
        void Chunk(long value, string unit)
        {
            if (value <= 0) return;
            parts.Add(BelowHundred((int)value) + (unit.Length > 0 ? " " + unit : ""));
        }
        Chunk(n / 10000000, "Crore");
        n %= 10000000;
        Chunk(n / 100000, "Lakh");
        n %= 100000;
        Chunk(n / 1000, "Thousand");
        n %= 1000;
        Chunk(n / 100, "Hundred");
        n %= 100;
        if (n > 0) parts.Add(BelowHundred((int)n));
        return string.Join(" ", parts);
    }

    private static string BelowHundred(int n)
    {
        if (n >= 100) return IndianWords(n);
        if (n < 20) return Ones[n];
        return Tens[n / 10] + (n % 10 > 0 ? " " + Ones[n % 10] : "");
    }

    private sealed class EmpRow
    {
        public string? EmpCode { get; set; }
        public string? Name { get; set; }
        public string? FatherName { get; set; }
        public string? Designation { get; set; }
        public string? Department { get; set; }
        public string? Company { get; set; }
        public string? Branch { get; set; }
        public DateTime? DateOfJoining { get; set; }
        public string? PfAccount { get; set; }
        public string? Uan { get; set; }
        public string? Esic { get; set; }
        public string? Pan { get; set; }
        public string? BankName { get; set; }
        public string? BankAccount { get; set; }
        public string? Ifsc { get; set; }
    }
}

public sealed record HrPayslipLineDto(string Label, decimal Amount);

public sealed class HrPayslipEmployeeDto
{
    public string EmpCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string? FatherName { get; set; }
    public string? Designation { get; set; }
    public string? Department { get; set; }
    public string? Company { get; set; }
    public string? Branch { get; set; }
    public string? DateOfJoining { get; set; }
    public string? PfAccount { get; set; }
    public string? Uan { get; set; }
    public string? Esic { get; set; }
    public string? Pan { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountMasked { get; set; }
    public string? Ifsc { get; set; }
}

public sealed class HrPayslipDto
{
    public string YearMonth { get; set; } = "";
    public string PeriodLabel { get; set; } = "";
    /// <summary>Monthly | Per day</summary>
    public string PayType { get; set; } = "";
    public HrPayslipEmployeeDto Employee { get; set; } = new();
    public string? PhotoDataUrl { get; set; }
    public decimal DaysInMonth { get; set; }
    public decimal PaidDays { get; set; }
    public decimal? WorkingDays { get; set; }
    public decimal? LeaveDays { get; set; }
    public decimal? OvertimeHours { get; set; }
    public decimal? RatePerDay { get; set; }
    public string? PaymentMode { get; set; }
    public List<HrPayslipLineDto> Earnings { get; set; } = [];
    public List<HrPayslipLineDto> Deductions { get; set; } = [];
    /// <summary>Informational amounts that are not part of this month's pay (or are already inside a total).</summary>
    public List<HrPayslipLineDto> Notes { get; set; } = [];
    public decimal GrossEarnings { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetPay { get; set; }
    public string NetPayInWords { get; set; } = "";
    public DateTime GeneratedAt { get; set; }

    internal string? Company { get; set; }
    internal string? Branch { get; set; }
    internal string? Department { get; set; }
}
