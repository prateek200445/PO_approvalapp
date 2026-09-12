using Dapper;
using ClosedXML.Excel;

namespace POApprovalAPI.Services;

public sealed class GroupSalaryService
{
    private const int TimeoutSeconds = 180;

    /// <summary>Exact ledger names from Ex-Im / ERP salary &amp; allowance heads.</summary>
    public static readonly string[] SalaryLedgerNames =
    [
        "Allowance - Books & Periodicals",
        "Allowance - Canteen",
        "Allowance - Children Education & Hostel",
        "Allowance - Driver & Assistant",
        "Allowance - House Rent",
        "Allowance - Leave Travels",
        "Allowance - Medical",
        "Allowance - Other",
        "Allowance - Special",
        "Allowance - Transport",
        "Allowance - Uniform / Attire",
        "Allowance - Washing",
        "Att. Bonus (Salary Part)",
        "Bonus Expense",
        "Incentive Expense - Production",
        "Labour Charges",
        "Leave Encashment",
        "Other Deduction",
        "Salaries Expense",
    ];

    private readonly DatabaseService _database;

    public GroupSalaryService(DatabaseService database)
    {
        _database = database;
    }

    public async Task<GroupSalaryDashboardDto> GetDashboardAsync(
        DateTime dateFrom,
        DateTime dateTo,
        string? company = null)
    {
        dateFrom = dateFrom.Date;
        dateTo = dateTo.Date;
        if (dateTo < dateFrom)
            (dateFrom, dateTo) = (dateTo, dateFrom);

        var rows = await LoadRowsAsync(dateFrom, dateTo, company);
        var months = BuildMonthKeys(dateFrom, dateTo);
        var companies = rows
            .Select(r => r.CompanyName)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var companyMonth = months.ToDictionary(m => m, _ => 0m, StringComparer.OrdinalIgnoreCase);
        var ledgerMonth = months.ToDictionary(m => m, _ => 0m, StringComparer.OrdinalIgnoreCase);
        var companyTotals = companies.ToDictionary(c => c, _ => 0m, StringComparer.OrdinalIgnoreCase);
        var ledgerTotals = SalaryLedgerNames.ToDictionary(l => l, _ => 0m, StringComparer.OrdinalIgnoreCase);

        var companyMatrices = companies.ToDictionary(
            c => c,
            _ => months.ToDictionary(m => m, __ => 0m, StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);

        var ledgerMatrices = SalaryLedgerNames.ToDictionary(
            l => l,
            _ => months.ToDictionary(m => m, __ => 0m, StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);

        foreach (var r in rows)
        {
            var monthKey = $"{r.Yr:D4}-{r.Mo:D2}";
            if (!companyMonth.ContainsKey(monthKey)) continue;

            companyMonth[monthKey] += r.Amount;
            ledgerMonth[monthKey] += r.Amount;

            if (companyMatrices.TryGetValue(r.CompanyName, out var cm))
            {
                cm[monthKey] = cm.GetValueOrDefault(monthKey) + r.Amount;
                companyTotals[r.CompanyName] = companyTotals.GetValueOrDefault(r.CompanyName) + r.Amount;
            }

            var ledgerKey = MatchLedger(r.LedgerName);
            if (ledgerKey != null && ledgerMatrices.TryGetValue(ledgerKey, out var lm))
            {
                lm[monthKey] = lm.GetValueOrDefault(monthKey) + r.Amount;
                ledgerTotals[ledgerKey] = ledgerTotals.GetValueOrDefault(ledgerKey) + r.Amount;
            }
        }

        return new GroupSalaryDashboardDto
        {
            DateFrom = dateFrom.ToString("yyyy-MM-dd"),
            DateTo = dateTo.ToString("yyyy-MM-dd"),
            CompanyFilter = string.IsNullOrWhiteSpace(company) ? null : company.Trim(),
            Ledgers = SalaryLedgerNames.ToList(),
            Months = months,
            Companies = companies,
            TotalAmount = rows.Sum(r => r.Amount),
            CompanyRows = companies.Select(c => new GroupSalaryMatrixRowDto
            {
                Name = c,
                Amounts = companyMatrices[c],
                Total = companyTotals.GetValueOrDefault(c),
            }).OrderByDescending(x => Math.Abs(x.Total)).ToList(),
            LedgerRows = SalaryLedgerNames.Select(l => new GroupSalaryMatrixRowDto
            {
                Name = l,
                Amounts = ledgerMatrices[l],
                Total = ledgerTotals.GetValueOrDefault(l),
            }).ToList(),
            MonthTotals = months.Select(m => new GroupSalaryNamedAmountDto
            {
                Name = m,
                Amount = companyMonth.GetValueOrDefault(m),
            }).ToList(),
            Lines = rows
                .OrderBy(r => r.CompanyName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(r => r.Yr)
                .ThenBy(r => r.Mo)
                .ThenBy(r => r.LedgerName, StringComparer.OrdinalIgnoreCase)
                .Select(r => new GroupSalaryLineDto
                {
                    CompanyName = r.CompanyName,
                    LedgerName = r.LedgerName,
                    Year = r.Yr,
                    Month = r.Mo,
                    MonthKey = $"{r.Yr:D4}-{r.Mo:D2}",
                    Amount = r.Amount,
                })
                .ToList(),
        };
    }

    public async Task<IReadOnlyList<string>> GetCompaniesAsync()
    {
        using var connection = _database.CreateConnection();
        var list = (await connection.QueryAsync<string>(@"
SELECT DISTINCT LTRIM(RTRIM(CompanyName)) AS CompanyName
FROM vw_LedgerSummary WITH (NOLOCK)
WHERE LedgerName IN @Ledgers
  AND ISNULL(LTRIM(RTRIM(CompanyName)), N'') <> N''
ORDER BY CompanyName",
            new { Ledgers = SalaryLedgerNames },
            commandTimeout: TimeoutSeconds)).ToList();
        return list;
    }

    public async Task<byte[]> BuildExcelAsync(DateTime dateFrom, DateTime dateTo, string? company = null)
    {
        var data = await GetDashboardAsync(dateFrom, dateTo, company);
        using var wb = new XLWorkbook();

        var byCompany = wb.Worksheets.Add("By Company");
        WriteMatrixSheet(byCompany, "Company", data.Months, data.CompanyRows);

        var byLedger = wb.Worksheets.Add("By Ledger");
        WriteMatrixSheet(byLedger, "Ledger", data.Months, data.LedgerRows);

        var detail = wb.Worksheets.Add("Detail");
        detail.Cell(1, 1).Value = "Company";
        detail.Cell(1, 2).Value = "Ledger";
        detail.Cell(1, 3).Value = "Year";
        detail.Cell(1, 4).Value = "Month";
        detail.Cell(1, 5).Value = "Amount";
        detail.Range(1, 1, 1, 5).Style.Font.Bold = true;
        var r = 2;
        foreach (var line in data.Lines)
        {
            detail.Cell(r, 1).Value = line.CompanyName;
            detail.Cell(r, 2).Value = line.LedgerName;
            detail.Cell(r, 3).Value = line.Year;
            detail.Cell(r, 4).Value = line.Month;
            detail.Cell(r, 5).Value = line.Amount;
            r++;
        }
        detail.Columns().AdjustToContents(1, 40);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static void WriteMatrixSheet(
        IXLWorksheet ws,
        string nameHeader,
        IReadOnlyList<string> months,
        IReadOnlyList<GroupSalaryMatrixRowDto> rows)
    {
        ws.Cell(1, 1).Value = nameHeader;
        for (var i = 0; i < months.Count; i++)
            ws.Cell(1, i + 2).Value = months[i];
        ws.Cell(1, months.Count + 2).Value = "Total";
        ws.Row(1).Style.Font.Bold = true;

        for (var r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            ws.Cell(r + 2, 1).Value = row.Name;
            for (var i = 0; i < months.Count; i++)
                ws.Cell(r + 2, i + 2).Value = row.Amounts.GetValueOrDefault(months[i]);
            ws.Cell(r + 2, months.Count + 2).Value = row.Total;
        }

        var totalRow = rows.Count + 2;
        ws.Cell(totalRow, 1).Value = "Total";
        ws.Cell(totalRow, 1).Style.Font.Bold = true;
        for (var i = 0; i < months.Count; i++)
        {
            var m = months[i];
            ws.Cell(totalRow, i + 2).Value = rows.Sum(x => x.Amounts.GetValueOrDefault(m));
            ws.Cell(totalRow, i + 2).Style.Font.Bold = true;
        }
        ws.Cell(totalRow, months.Count + 2).Value = rows.Sum(x => x.Total);
        ws.Cell(totalRow, months.Count + 2).Style.Font.Bold = true;
        ws.Columns().AdjustToContents(1, 28);
    }

    private async Task<List<AggRow>> LoadRowsAsync(DateTime dateFrom, DateTime dateTo, string? company)
    {
        var allCompanies = string.IsNullOrWhiteSpace(company);
        using var connection = _database.CreateConnection();
        var rows = (await connection.QueryAsync<AggRow>(@"
SELECT
    LTRIM(RTRIM(ISNULL(VL.CompanyName, N''))) AS CompanyName,
    LTRIM(RTRIM(ISNULL(VL.LedgerName, N''))) AS LedgerName,
    YEAR(VL.Date) AS Yr,
    MONTH(VL.Date) AS Mo,
    SUM(ISNULL(VL.Amount, 0)) AS Amount
FROM vw_LedgerSummary VL WITH (NOLOCK)
WHERE VL.LedgerName IN @Ledgers
  AND VL.Date BETWEEN @DateFrom AND @DateTo
  AND (@AllCompanies = 1 OR VL.CompanyName = @Company)
GROUP BY
    LTRIM(RTRIM(ISNULL(VL.CompanyName, N''))),
    LTRIM(RTRIM(ISNULL(VL.LedgerName, N''))),
    YEAR(VL.Date),
    MONTH(VL.Date)",
            new
            {
                Ledgers = SalaryLedgerNames,
                DateFrom = dateFrom,
                DateTo = dateTo,
                AllCompanies = allCompanies ? 1 : 0,
                Company = company?.Trim() ?? "",
            },
            commandTimeout: TimeoutSeconds)).ToList();

        return rows.Where(r => !string.IsNullOrWhiteSpace(r.CompanyName)).ToList();
    }

    private static string? MatchLedger(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return SalaryLedgerNames.FirstOrDefault(l =>
            l.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static List<string> BuildMonthKeys(DateTime from, DateTime to)
    {
        var list = new List<string>();
        var cursor = new DateTime(from.Year, from.Month, 1);
        var end = new DateTime(to.Year, to.Month, 1);
        while (cursor <= end)
        {
            list.Add($"{cursor:yyyy-MM}");
            cursor = cursor.AddMonths(1);
        }
        return list;
    }

    private sealed class AggRow
    {
        public string CompanyName { get; set; } = "";
        public string LedgerName { get; set; } = "";
        public int Yr { get; set; }
        public int Mo { get; set; }
        public decimal Amount { get; set; }
    }
}

public sealed class GroupSalaryDashboardDto
{
    public string DateFrom { get; set; } = "";
    public string DateTo { get; set; } = "";
    public string? CompanyFilter { get; set; }
    public List<string> Ledgers { get; set; } = [];
    public List<string> Months { get; set; } = [];
    public List<string> Companies { get; set; } = [];
    public decimal TotalAmount { get; set; }
    public List<GroupSalaryMatrixRowDto> CompanyRows { get; set; } = [];
    public List<GroupSalaryMatrixRowDto> LedgerRows { get; set; } = [];
    public List<GroupSalaryNamedAmountDto> MonthTotals { get; set; } = [];
    public List<GroupSalaryLineDto> Lines { get; set; } = [];
}

public sealed class GroupSalaryMatrixRowDto
{
    public string Name { get; set; } = "";
    public Dictionary<string, decimal> Amounts { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public decimal Total { get; set; }
}

public sealed class GroupSalaryNamedAmountDto
{
    public string Name { get; set; } = "";
    public decimal Amount { get; set; }
}

public sealed class GroupSalaryLineDto
{
    public string CompanyName { get; set; } = "";
    public string LedgerName { get; set; } = "";
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthKey { get; set; } = "";
    public decimal Amount { get; set; }
}
