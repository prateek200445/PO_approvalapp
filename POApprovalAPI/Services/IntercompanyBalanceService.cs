using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using POApprovalAPI.Documents;
using QuestPDF.Fluent;

namespace POApprovalAPI.Services;

/// <summary>
/// Intercompany balances from the data query inside
/// <c>sp_Automail_InterCompanyBalance_Limited</c> (not the email send).
/// </summary>
public class IntercompanyBalanceService
{
    private const int CommandTimeoutSeconds = 120;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(2);

    /// <summary>Same company list and sort as the automail SP.</summary>
    private static readonly string[] ReportCompanies =
    [
        "HCP Plastene Bulkpack Ltd",
        "HCP ENTERPRISE LIMITED",
        "K.P. WOVEN PRIVATE LIMITED",
        "Plastene India Limited",
        "Plastene Polyfilms Limited",
        "Oswal Extrusion Limited",
        "OSWAL COMMODITIES PRIVATE LIMITED",
    ];

    /// <summary>
    /// Verified retrieval from sp_Automail_InterCompanyBalance_Limited — do not change.
    /// CompanyType=1 (group), CompanyId unused.
    /// </summary>
    private const string BalanceSql = @"
SELECT
    @CompanyName AS CurrentCompany,
    F1.GROUPNAME AS InterCompany,
    ROUND(SUM(V.Amount), 2) AS ClosingINR
FROM Ac_InterCompanyLedger AC
INNER JOIN LedgerMaster L
    ON AC.LedgerId = L.SrNo
INNER JOIN vw_LedgerSummary V
    ON V.LedgerName = L.LedgerName
   AND V.CompanyName = L.CompanyName
INNER JOIN FactoryInfo F
    ON F.SrNo = AC.CompanyId
LEFT JOIN FactoryInfo F1
    ON F1.SrNo = AC.InterCompanyID
WHERE
    F.GroupName =
        CASE
            WHEN @CompanyType = 1
            THEN @CompanyName
            ELSE F.GroupName
        END
    AND F.SrNo =
        CASE
            WHEN @CompanyType = 2
            THEN @CompanyId
            ELSE F.SrNo
        END
    AND AC.InterCompanyID <> 0
    AND V.Date <= CONVERT(CHAR(10), @DateTo, 120)
    AND F1.GROUPNAME IN
    (
        'HCP Plastene Bulkpack Ltd',
        'HCP ENTERPRISE LIMITED',
        'K.P. WOVEN PRIVATE LIMITED',
        'Plastene India Limited',
        'Plastene Polyfilms Limited',
        'Oswal Extrusion Limited',
        'OSWAL COMMODITIES PRIVATE LIMITED'
    )
    AND F1.GROUPNAME <> @CompanyName
GROUP BY
    F1.GROUPNAME";

    private readonly DatabaseService _database;
    private readonly IMemoryCache _cache;

    public IntercompanyBalanceService(DatabaseService database, IMemoryCache cache)
    {
        _database = database;
        _cache = cache;
    }

    public async Task<IntercompanyDashboardDto> GetDashboardAsync(DateTime asOf, bool refresh = false)
    {
        var asOfDate = asOf.Date;
        var key = $"intercompany-balances-sp-v1|{asOfDate:yyyy-MM-dd}";
        if (refresh)
            _cache.Remove(key);

        if (_cache.TryGetValue(key, out IntercompanyDashboardDto? cached) && cached is not null)
            return cached;

        var dto = await BuildDashboardAsync(asOfDate);
        _cache.Set(key, dto, CacheTtl);
        return dto;
    }

    public async Task<byte[]> BuildExcelAsync(DateTime asOf, bool refresh = false)
    {
        var data = await GetDashboardAsync(asOf, refresh);
        var grid = BuildBalanceGrid(data.Matrices);
        var asOfDate = asOf.Date;
        var navy = XLColor.FromHtml("#0B3A5B");
        var headerBlue = XLColor.FromHtml("#1565A8");
        var gold = XLColor.FromHtml("#C9A227");
        var sky = XLColor.FromHtml("#E0F2FE");
        var inCulture = CultureInfo.GetCultureInfo("en-IN");

        using var workbook = new XLWorkbook();
        WriteMatrixSheet(workbook, grid, asOfDate, navy, headerBlue, gold, sky);
        WriteDetailSheet(workbook, data.Lines, asOfDate, navy, headerBlue, gold, inCulture);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> BuildPdfAsync(DateTime asOf, bool refresh = false)
    {
        var data = await GetDashboardAsync(asOf, refresh);
        return new IntercompanyBalancePdfDocument(data, asOf.Date).GeneratePdf();
    }

    internal static IntercompanyBalanceGrid BuildBalanceGrid(IReadOnlyList<IntercompanyMatrixDto> matrices)
    {
        var fromCompanies = matrices.Select(m => m.Company).Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
        var fromByKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in fromCompanies)
        {
            var key = CompanyAlignKey(name);
            if (key.Length > 0 && !fromByKey.ContainsKey(key))
                fromByKey[key] = name;
        }

        var extraColumns = new List<string>();
        var extraSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var cells = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);

        void AddCell(string from, string to, double value)
        {
            if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
                return;
            if (CompanyAlignKey(from) == CompanyAlignKey(to))
                return;
            if (!cells.TryGetValue(from, out var row))
            {
                row = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                cells[from] = row;
            }
            row[to] = row.TryGetValue(to, out var existing) ? existing + value : value;
        }

        foreach (var matrix in matrices)
        {
            foreach (var pair in matrix.Amounts ?? [])
            {
                var value = pair.Value;
                var mapped = fromByKey.TryGetValue(CompanyAlignKey(pair.Key), out var hit) ? hit : null;
                if (mapped != null)
                {
                    AddCell(matrix.Company, mapped, value);
                    continue;
                }

                var extra = pair.Key.Trim();
                if (extra.Length == 0)
                    continue;
                if (extraSeen.Add(extra))
                    extraColumns.Add(extra);
                AddCell(matrix.Company, extra, value);
            }
        }

        extraColumns.Sort(StringComparer.OrdinalIgnoreCase);
        var columns = fromCompanies.Concat(extraColumns).ToList();
        var rowTotals = fromCompanies
            .Select(from => columns.Sum(to => cells.TryGetValue(from, out var row) && row.TryGetValue(to, out var v) ? v : 0d))
            .ToList();
        var colTotals = columns
            .Select(to => fromCompanies.Sum(from => cells.TryGetValue(from, out var row) && row.TryGetValue(to, out var v) ? v : 0d))
            .ToList();

        return new IntercompanyBalanceGrid
        {
            FromCompanies = fromCompanies,
            Columns = columns,
            Cells = cells,
            RowTotals = rowTotals,
            ColTotals = colTotals,
            GrandTotal = rowTotals.Sum(),
        };
    }

    internal static string CompanyAlignKey(string name)
    {
        var lowered = (name ?? "").ToLowerInvariant();
        var alnum = Regex.Replace(lowered, @"[^a-z0-9]+", " ");
        var stripped = Regex.Replace(alnum, @"\b(pvt|private|ltd|limited|llp)\b", " ");
        return Regex.Replace(stripped, @"\s+", " ").Trim();
    }

    private static void WriteMatrixSheet(
        XLWorkbook workbook,
        IntercompanyBalanceGrid grid,
        DateTime asOfDate,
        XLColor navy,
        XLColor headerBlue,
        XLColor gold,
        XLColor sky)
    {
        var sheet = workbook.Worksheets.Add("Matrix Cr");
        sheet.Style.Font.FontName = "Calibri";
        sheet.Style.Font.FontSize = 10;
        var lastCol = Math.Max(2, grid.Columns.Count + 2);

        sheet.Range(1, 1, 1, lastCol).Merge();
        var title = sheet.Cell(1, 1);
        title.Value = "INTER-COMPANY BALANCE MATRIX";
        title.Style.Font.Bold = true;
        title.Style.Font.FontSize = 16;
        title.Style.Font.FontColor = XLColor.White;
        title.Style.Fill.BackgroundColor = navy;
        title.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        title.Style.Alignment.Indent = 1;
        sheet.Row(1).Height = 28;

        sheet.Range(2, 1, 2, lastCol).Merge();
        var subtitle = sheet.Cell(2, 1);
        subtitle.Value = $"As on {asOfDate:dd-MMM-yyyy}  ·  Amounts in ₹ Crore  ·  Generated {DateTime.Now:dd-MMM-yyyy HH:mm}";
        subtitle.Style.Font.FontColor = XLColor.White;
        subtitle.Style.Font.FontSize = 10;
        subtitle.Style.Fill.BackgroundColor = headerBlue;
        subtitle.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        subtitle.Style.Alignment.Indent = 1;
        sheet.Row(2).Height = 18;

        sheet.Range(3, 1, 3, lastCol).Merge();
        sheet.Cell(3, 1).Style.Fill.BackgroundColor = gold;
        sheet.Row(3).Height = 4;

        const int headerRow = 5;
        sheet.Cell(headerRow, 1).Value = "From \\ To";
        for (var i = 0; i < grid.Columns.Count; i++)
            sheet.Cell(headerRow, i + 2).Value = grid.Columns[i];
        sheet.Cell(headerRow, lastCol).Value = "Total";

        var headerRange = sheet.Range(headerRow, 1, headerRow, lastCol);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Fill.BackgroundColor = navy;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        headerRange.Style.Alignment.WrapText = true;
        sheet.Row(headerRow).Height = 36;

        for (var r = 0; r < grid.FromCompanies.Count; r++)
        {
            var excelRow = headerRow + 1 + r;
            var from = grid.FromCompanies[r];
            sheet.Cell(excelRow, 1).Value = from;
            sheet.Cell(excelRow, 1).Style.Font.Bold = true;
            sheet.Cell(excelRow, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F1F8");
            sheet.Cell(excelRow, 1).Style.Alignment.WrapText = true;

            for (var c = 0; c < grid.Columns.Count; c++)
            {
                var to = grid.Columns[c];
                var cell = sheet.Cell(excelRow, c + 2);
                var diagonal = CompanyAlignKey(from) == CompanyAlignKey(to);
                if (diagonal)
                {
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
                    continue;
                }

                var value = grid.Cells.TryGetValue(from, out var row) && row.TryGetValue(to, out var v) ? v : 0d;
                if (Math.Abs(value) < 0.005)
                    continue;
                cell.Value = value / 10_000_000d;
                cell.Style.NumberFormat.Format = "#,##0.00";
                cell.Style.Fill.BackgroundColor = sky;
                if (value < 0)
                    cell.Style.Font.FontColor = XLColor.FromHtml("#B91C1C");
            }

            var totalCell = sheet.Cell(excelRow, lastCol);
            var rowTotal = grid.RowTotals[r];
            if (Math.Abs(rowTotal) >= 0.005)
                totalCell.Value = rowTotal / 10_000_000d;
            totalCell.Style.NumberFormat.Format = "#,##0.00";
            totalCell.Style.Font.Bold = true;
            totalCell.Style.Font.FontColor = XLColor.White;
            totalCell.Style.Fill.BackgroundColor = navy;
            if (rowTotal < 0)
                totalCell.Style.Font.FontColor = XLColor.FromHtml("#FECACA");
        }

        var totalRow = headerRow + 1 + grid.FromCompanies.Count;
        sheet.Cell(totalRow, 1).Value = "Total";
        sheet.Range(totalRow, 1, totalRow, lastCol).Style.Font.Bold = true;
        sheet.Range(totalRow, 1, totalRow, lastCol).Style.Font.FontColor = XLColor.White;
        sheet.Range(totalRow, 1, totalRow, lastCol).Style.Fill.BackgroundColor = navy;
        for (var c = 0; c < grid.ColTotals.Count; c++)
        {
            var cell = sheet.Cell(totalRow, c + 2);
            if (Math.Abs(grid.ColTotals[c]) >= 0.005)
                cell.Value = grid.ColTotals[c] / 10_000_000d;
            cell.Style.NumberFormat.Format = "#,##0.00";
            if (grid.ColTotals[c] < 0)
                cell.Style.Font.FontColor = XLColor.FromHtml("#FECACA");
        }

        var grand = sheet.Cell(totalRow, lastCol);
        if (Math.Abs(grid.GrandTotal) >= 0.005)
            grand.Value = grid.GrandTotal / 10_000_000d;
        grand.Style.NumberFormat.Format = "#,##0.00";
        if (grid.GrandTotal < 0)
            grand.Style.Font.FontColor = XLColor.FromHtml("#FECACA");

        sheet.Column(1).Width = 28;
        for (var i = 2; i <= lastCol; i++)
            sheet.Column(i).Width = 16;
        sheet.SheetView.FreezeRows(headerRow);
        sheet.SheetView.FreezeColumns(1);
        sheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        sheet.PageSetup.FitToPages(1, 1);
    }

    private static void WriteDetailSheet(
        XLWorkbook workbook,
        IReadOnlyList<IntercompanyLineDto> lines,
        DateTime asOfDate,
        XLColor navy,
        XLColor headerBlue,
        XLColor gold,
        CultureInfo inCulture)
    {
        var sheet = workbook.Worksheets.Add("Detailed balances");
        sheet.Style.Font.FontName = "Calibri";
        sheet.Style.Font.FontSize = 11;

        sheet.Range(1, 1, 1, 4).Merge();
        var title = sheet.Cell(1, 1);
        title.Value = "DETAILED INTER-COMPANY BALANCES";
        title.Style.Font.Bold = true;
        title.Style.Font.FontSize = 16;
        title.Style.Font.FontColor = XLColor.White;
        title.Style.Fill.BackgroundColor = navy;
        title.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        title.Style.Alignment.Indent = 1;
        sheet.Row(1).Height = 28;

        sheet.Range(2, 1, 2, 4).Merge();
        var subtitle = sheet.Cell(2, 1);
        subtitle.Value = $"As on {asOfDate:dd-MMM-yyyy}  ·  {lines.Count.ToString("N0", inCulture)} rows  ·  Generated {DateTime.Now:dd-MMM-yyyy HH:mm}";
        subtitle.Style.Font.FontColor = XLColor.White;
        subtitle.Style.Fill.BackgroundColor = headerBlue;
        subtitle.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        subtitle.Style.Alignment.Indent = 1;
        sheet.Row(2).Height = 18;

        sheet.Range(3, 1, 3, 4).Merge();
        sheet.Cell(3, 1).Style.Fill.BackgroundColor = gold;
        sheet.Row(3).Height = 4;

        const int headerRow = 5;
        var headers = new[] { "Company", "Counterparty", "Balance (INR)", "Balance (₹ Cr.)" };
        for (var i = 0; i < headers.Length; i++)
            sheet.Cell(headerRow, i + 1).Value = headers[i];

        var headerRange = sheet.Range(headerRow, 1, headerRow, 4);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Fill.BackgroundColor = navy;

        var ordered = lines.OrderBy(l => l.Company, StringComparer.OrdinalIgnoreCase)
            .ThenBy(l => l.Balance)
            .ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            var row = headerRow + 1 + i;
            var line = ordered[i];
            sheet.Cell(row, 1).Value = line.Company;
            sheet.Cell(row, 2).Value = line.Counterparty;
            sheet.Cell(row, 3).Value = line.Balance;
            sheet.Cell(row, 4).Value = line.BalanceCr;
            sheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
            sheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
            if (line.Balance < 0)
            {
                sheet.Cell(row, 3).Style.Font.FontColor = XLColor.FromHtml("#B91C1C");
                sheet.Cell(row, 4).Style.Font.FontColor = XLColor.FromHtml("#B91C1C");
            }
        }

        if (ordered.Count > 0)
        {
            var last = headerRow + ordered.Count;
            var table = sheet.Range(headerRow, 1, last, 4).CreateTable("IntercompanyDetails");
            table.Theme = XLTableTheme.TableStyleMedium2;
            table.ShowAutoFilter = true;
        }

        sheet.Column(1).Width = 36;
        sheet.Column(2).Width = 36;
        sheet.Column(3).Width = 18;
        sheet.Column(4).Width = 16;
        sheet.SheetView.FreezeRows(headerRow);
        sheet.PageSetup.PageOrientation = XLPageOrientation.Portrait;
        sheet.PageSetup.FitToPages(1, 0);
    }

    private async Task<IntercompanyDashboardDto> BuildDashboardAsync(DateTime asOfDate)
    {
        using var connection = _database.CreateConnection();
        var pairs = await QuerySpBalancesAsync(connection, asOfDate);

        var lookup = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in pairs)
        {
            var company = (row.CurrentCompany ?? "").Trim();
            var counterparty = (row.InterCompany ?? "").Trim();
            if (company.Length == 0 || counterparty.Length == 0)
                continue;

            if (!lookup.TryGetValue(company, out var map))
            {
                map = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                lookup[company] = map;
            }

            map[counterparty] = Math.Round(row.ClosingINR, 2, MidpointRounding.AwayFromZero);
        }

        var lines = new List<IntercompanyLineDto>();
        var matrices = new List<IntercompanyMatrixDto>();

        foreach (var company in ReportCompanies)
        {
            lookup.TryGetValue(company, out var map);
            map ??= new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            var amounts = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var total = 0d;
            foreach (var other in ReportCompanies)
            {
                if (other.Equals(company, StringComparison.OrdinalIgnoreCase))
                    continue;
                var value = map.TryGetValue(other, out var v) ? v : 0d;
                amounts[other] = value;
                total += value;
            }

            matrices.Add(new IntercompanyMatrixDto
            {
                Company = company,
                Amounts = amounts,
                Total = Math.Round(total, 2, MidpointRounding.AwayFromZero),
            });

            foreach (var other in ReportCompanies)
            {
                if (other.Equals(company, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!map.TryGetValue(other, out var value) || Math.Round(Math.Abs(value), 0) == 0)
                    continue;

                lines.Add(new IntercompanyLineDto
                {
                    Company = company,
                    Counterparty = other,
                    LedgerName = other,
                    Balance = value,
                    BalanceCr = Math.Round(value / 10_000_000d, 2, MidpointRounding.AwayFromZero),
                });
            }
        }

        return new IntercompanyDashboardDto
        {
            AsOf = asOfDate.ToString("yyyy-MM-dd"),
            Counterparties = ReportCompanies.ToList(),
            Matrices = matrices,
            Lines = lines,
        };
    }

    private static async Task<List<SpBalanceRow>> QuerySpBalancesAsync(SqlConnection connection, DateTime asOf)
    {
        var rows = new List<SpBalanceRow>();
        foreach (var company in ReportCompanies)
        {
            var part = await connection.QueryAsync<SpBalanceRow>(
                BalanceSql,
                new
                {
                    CompanyName = company,
                    CompanyType = 1,
                    CompanyId = 0,
                    DateTo = asOf,
                },
                commandTimeout: CommandTimeoutSeconds);
            rows.AddRange(part);
        }

        return rows;
    }

    private sealed class SpBalanceRow
    {
        public string? CurrentCompany { get; set; }
        public string? InterCompany { get; set; }
        public double ClosingINR { get; set; }
    }
}

public class IntercompanyDashboardDto
{
    public string AsOf { get; set; } = "";
    public List<string> Counterparties { get; set; } = [];
    public List<IntercompanyMatrixDto> Matrices { get; set; } = [];
    public List<IntercompanyLineDto> Lines { get; set; } = [];
}

public class IntercompanyMatrixDto
{
    public string Company { get; set; } = "";
    public Dictionary<string, double> Amounts { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public double Total { get; set; }
}

public class IntercompanyLineDto
{
    public string Company { get; set; } = "";
    public string Counterparty { get; set; } = "";
    public string LedgerName { get; set; } = "";
    public double Balance { get; set; }
    public double BalanceCr { get; set; }
}

internal sealed class IntercompanyBalanceGrid
{
    public List<string> FromCompanies { get; init; } = [];
    public List<string> Columns { get; init; } = [];
    public Dictionary<string, Dictionary<string, double>> Cells { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public List<double> RowTotals { get; init; } = [];
    public List<double> ColTotals { get; init; } = [];
    public double GrandTotal { get; init; }
}
