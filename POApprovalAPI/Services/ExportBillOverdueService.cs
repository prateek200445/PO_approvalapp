using System.Collections.Concurrent;
using System.Globalization;
using ClosedXML.Excel;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using POApprovalAPI.Documents;
using QuestPDF.Fluent;

namespace POApprovalAPI.Services;

/// <summary>
/// Bill receivable overdue — ERP Ledger Summary Consolidate (Bill Wise):
/// outstanding = Opening + Debit − Credit, grouped by bill.
/// </summary>
public class ExportBillOverdueService
{
    public const string DefaultGroupName = "Debtors-Overseas";
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 200;
    private const int CommandTimeoutSeconds = 120;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(4);
    private static readonly TimeSpan MetaCacheTtl = TimeSpan.FromHours(6);
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> LoadLocks = new();

    private readonly DatabaseService _database;
    private readonly IMemoryCache _cache;

    public ExportBillOverdueService(DatabaseService database, IMemoryCache cache)
    {
        _database = database;
        _cache = cache;
    }

    public async Task<IReadOnlyList<string>> GetCompaniesAsync()
    {
        const string key = "export-bill-overdue-companies-v2";
        if (_cache.TryGetValue(key, out IReadOnlyList<string>? cached) && cached is not null)
            return cached;

        var factories = await GetFactoryRowsAsync();
        var list = factories
            .Select(f => f.Name)
            .Where(n => n.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        _cache.Set(key, (IReadOnlyList<string>)list, MetaCacheTtl);
        return list;
    }

    /// <summary>
    /// Company dropdown with FactoryInfo groups first (same G-{group} pattern as Ledger Summary).
    /// </summary>
    public async Task<IReadOnlyList<ExportCompanyOptionDto>> GetCompanyOptionsAsync()
    {
        const string key = "export-bill-overdue-company-options-v1";
        if (_cache.TryGetValue(key, out IReadOnlyList<ExportCompanyOptionDto>? cached) && cached is not null)
            return cached;

        var factories = await GetFactoryRowsAsync();
        var options = new List<ExportCompanyOptionDto>();

        var groups = factories
            .Select(f => f.GroupName)
            .Where(g => g.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var group in groups)
        {
            options.Add(new ExportCompanyOptionDto
            {
                Value = $"G-{group}",
                Label = $"{group} (Group)",
                Kind = "group",
            });
        }

        foreach (var factory in factories.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase))
        {
            options.Add(new ExportCompanyOptionDto
            {
                Value = $"C-{factory.SrNo}",
                Label = factory.Name,
                Kind = "company",
            });
        }

        _cache.Set(key, (IReadOnlyList<ExportCompanyOptionDto>)options, MetaCacheTtl);
        return options;
    }

    public async Task<IReadOnlyList<string>> GetGroupsAsync()
    {
        // Only export-relevant outstanding groups — never dump the full ERP group catalog to the UI.
        const string key = "export-bill-overdue-groups-v2-export-only";
        if (_cache.TryGetValue(key, out IReadOnlyList<string>? cached) && cached is not null)
            return cached;

        using var connection = _database.CreateConnection();
        var rows = await connection.QueryAsync<string>(
            @"SELECT DISTINCT ExpenseGroupHead
              FROM CashVoucherExpenseGroupHead WITH (NOLOCK)
              WHERE OutStanding = 'Yes'
              ORDER BY ExpenseGroupHead");

        var filtered = rows
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .Where(IsExportReceivableGroup)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!filtered.Any(g => g.Equals(DefaultGroupName, StringComparison.OrdinalIgnoreCase)))
            filtered.Insert(0, DefaultGroupName);

        _cache.Set(key, (IReadOnlyList<string>)filtered, MetaCacheTtl);
        return filtered;
    }

    /// <summary>
    /// Keep only overseas / export debtor-style groups for the Export Bill Overdue UI.
    /// </summary>
    private static bool IsExportReceivableGroup(string name)
    {
        var n = name.Trim();
        if (n.Equals(DefaultGroupName, StringComparison.OrdinalIgnoreCase))
            return true;

        var lower = n.ToLowerInvariant();
        var isDebtor = lower.Contains("debtor");
        var isOverseasOrExport =
            lower.Contains("overseas") ||
            lower.Contains("export") ||
            lower.Contains("foreign");

        return isDebtor && isOverseasOrExport;
    }

    /// <summary>
    /// Indian FY start (1 April) for the given as-of / to date — same default as Ledger Summary.
    /// </summary>
    public static DateTime FinancialYearStart(DateTime asOf)
    {
        var y = asOf.Month >= 4 ? asOf.Year : asOf.Year - 1;
        return new DateTime(y, 4, 1);
    }
    public async Task<ExportBillOverdueResultDto> GetOverdueBillsAsync(
        string company,
        DateTime asOf,
        string? groupName,
        int page = 1,
        int pageSize = DefaultPageSize,
        bool refresh = false,
        DateTime? dateFrom = null,
        string? search = null)
    {
        var selectedGroup = string.IsNullOrWhiteSpace(groupName) ? DefaultGroupName : groupName.Trim();
        if (!IsExportReceivableGroup(selectedGroup))
            selectedGroup = DefaultGroupName;
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var offset = (page - 1) * pageSize;

        var companyLabel = string.IsNullOrWhiteSpace(company) ? "All Companies" : company.Trim();
        var asOfDate = asOf.Date;
        var fromDate = ResolveDateFrom(dateFrom, asOfDate);
        var universeKey = UniverseCacheKey(asOfDate, selectedGroup);

        if (refresh)
        {
            _cache.Remove(universeKey);
            _cache.Remove(AgingReportCacheKey(universeKey, companyLabel, fromDate));
            _cache.Remove(AgingReportCacheKey(universeKey, "All Companies", fromDate));
            _cache.Remove($"{universeKey}|{companyLabel}|xlsx-v14|{fromDate:yyyy-MM-dd}");
            _cache.Remove($"{universeKey}|All Companies|xlsx-v14|{fromDate:yyyy-MM-dd}");
        }

        var universe = await GetOrLoadRowsAsync(universeKey, asOfDate, selectedGroup);
        var ic = await GetIntercompanyExclusionAsync();
        var rows = OverdueOnly(ExcludeIntercompany(await FilterRowsByCompanyAsync(universe, companyLabel), ic))
            .Where(r => MatchesSearch(r, search))
            .ToList();
        var total = rows.Count;
        var pageItems = rows.Skip(offset).Take(pageSize).ToList();

        return new ExportBillOverdueResultDto
        {
            Items = pageItems,
            Company = companyLabel,
            DateFrom = fromDate.ToString("yyyy-MM-dd"),
            AsOf = asOfDate.ToString("yyyy-MM-dd"),
            GroupName = selectedGroup,
            Source = "ERP FrmReceivable BindGrid (dueamount); overdue grid excludes IC",
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
        };
    }

    /// <summary>
    /// ERP FrmAgingReport1 default slabs extended with a 91–120 band (BETWEEN 1–30 … 91–120, then &gt;=121).
    /// Matches FrmAgingReport1: DATEDIFF(DD, billdate, as-of) with BETWEEN / &gt;= bounds (day 0 is not in 1–30).
    /// </summary>
    public static readonly string[] AgingBucketLabels =
    {
        "1–30", "31–60", "61–90", "91–120", "121+",
    };

    public static string AgingBucketHeading(string label) => $"{label} days";

    public async Task<ExportAgingReportDto> GetAgingReportAsync(
        string company,
        DateTime asOf,
        string? groupName,
        bool refresh = false,
        DateTime? dateFrom = null)
    {
        var selectedGroup = string.IsNullOrWhiteSpace(groupName) ? DefaultGroupName : groupName.Trim();
        if (!IsExportReceivableGroup(selectedGroup))
            selectedGroup = DefaultGroupName;

        var companyLabel = string.IsNullOrWhiteSpace(company) ? "All Companies" : company.Trim();
        var asOfDate = asOf.Date;
        var fromDate = ResolveDateFrom(dateFrom, asOfDate);
        var universeKey = UniverseCacheKey(asOfDate, selectedGroup);
        var agingKey = AgingReportCacheKey(universeKey, companyLabel, fromDate);

        if (refresh)
        {
            _cache.Remove(universeKey);
            _cache.Remove(agingKey);
            _cache.Remove(AgingReportCacheKey(universeKey, "All Companies", fromDate));
            _cache.Remove($"{universeKey}|{companyLabel}|xlsx-v14|{fromDate:yyyy-MM-dd}");
            _cache.Remove($"{universeKey}|All Companies|xlsx-v14|{fromDate:yyyy-MM-dd}");
        }
        else if (_cache.TryGetValue(agingKey, out ExportAgingReportDto? cachedReport) && cachedReport is not null)
        {
            return cachedReport;
        }

        // Aging matches ERP FrmReceivable / FrmAgingReport1: do not strip intercompany.
        var universe = await GetOrLoadRowsAsync(universeKey, asOfDate, selectedGroup);
        var rows = await FilterRowsByCompanyAsync(universe, companyLabel);
        var report = BuildAgingReport(rows, companyLabel, selectedGroup, fromDate, asOfDate);
        _cache.Set(agingKey, report, CacheTtl);
        return report;
    }

    /// <summary>
    /// ERP FrmAgingReport1: BETWEEN 1 AND 30, …, BETWEEN 91 AND 120, then &gt;=121.
    /// Returns -1 for day 0 / On Account / future (Notoverdue — in totals, not in age slabs).
    /// </summary>
    private static int AgingBucketIndex(int days)
    {
        if (days < 1) return -1;
        if (days <= 30) return 0;
        if (days <= 60) return 1;
        if (days <= 90) return 2;
        if (days <= 120) return 3;
        return 4;
    }

    /// <summary>
    /// ERP FrmReceivable BillDateOverdueDays + FrmAgingReport1 Notoverdue:
    /// On Account (1900-01-01) → 0 days (kept off slabs); else DATEDIFF(DD, bill date, as-of), floored at 0.
    /// </summary>
    private static int AgingDays(ExportBillOverdueItemDto row, DateTime asOfDate)
    {
        var s = row.BillDate;
        // Fast path: On Account / epoch bills stay in Notoverdue (totals only).
        if (string.IsNullOrEmpty(s) || s.StartsWith("1900", StringComparison.Ordinal))
            return 0;
        if (!DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var billDate) &&
            !DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out billDate))
            return 0;
        if (billDate.Year <= 1900)
            return 0;
        var days = (asOfDate.Date - billDate.Date).Days;
        return days < 0 ? 0 : days;
    }

    private static List<ExportBillOverdueItemDto> OverdueOnly(List<ExportBillOverdueItemDto> rows) =>
        rows.Where(r => r.OverdueDays > 0 && r.PendingAmount > 0).ToList();

    /// <summary>
    /// Portal overdue grid only: hide intercompany factory/ledger pairs.
    /// ERP FrmReceivable does not apply this filter — aging keeps IC for parity.
    /// </summary>
    private static List<ExportBillOverdueItemDto> ExcludeIntercompany(
        List<ExportBillOverdueItemDto> rows,
        IcExclusion ic) =>
        rows.Where(r => !IsIntercompanyRow(r, ic)).ToList();

    private static DateTime ResolveDateFrom(DateTime? dateFrom, DateTime asOfDate)
    {
        var from = (dateFrom ?? FinancialYearStart(asOfDate)).Date;
        return from > asOfDate ? asOfDate : from;
    }

    private static string AgingCustomerName(ExportBillOverdueItemDto row)
    {
        var customer = string.IsNullOrWhiteSpace(row.CustomerName) ? row.LedgerName : row.CustomerName;
        customer = (customer ?? "").Trim();
        return customer.Length == 0 ? "Unknown" : customer;
    }

    /// <summary>
    /// ERP FrmAgingReport1: signed bill lines (CR negative), age by BillDateOverdueDays slabs.
    /// On Account + day 0 stay in party/grand total only (Notoverdue); not dumped into 1–30 or 121+.
    /// </summary>
    private static ExportAgingReportDto BuildAgingReport(
        List<ExportBillOverdueItemDto> rows,
        string companyLabel,
        string selectedGroup,
        DateTime fromDate,
        DateTime asOfDate)
    {
        var buckets = AgingBucketLabels.Select((label, i) => new ExportAgingBucketDto
        {
            Key = $"b{i}",
            Label = label,
            PendingAmount = 0,
            BillCount = 0,
        }).ToList();

        var map = new Dictionary<(string Company, string Customer), ExportAgingCustomerDto>();
        var totalPending = 0d;
        var totalBills = 0;

        foreach (var row in rows)
        {
            var pending = Math.Round(row.PendingAmount, 2, MidpointRounding.AwayFromZero);
            if (Math.Round(Math.Abs(pending), 0) == 0)
                continue;

            var idx = AgingBucketIndex(AgingDays(row, asOfDate));
            totalPending += pending;
            totalBills += 1;
            if (idx >= 0)
            {
                buckets[idx].PendingAmount += pending;
                buckets[idx].BillCount += 1;
            }

            var customer = AgingCustomerName(row);
            var companyName = (row.CompanyName ?? "").Trim();
            var key = (companyName, customer);
            if (!map.TryGetValue(key, out var line))
            {
                line = new ExportAgingCustomerDto
                {
                    CompanyName = companyName,
                    CustomerName = customer,
                    Amounts = new double[AgingBucketLabels.Length],
                };
                map[key] = line;
            }

            if (idx >= 0)
                line.Amounts[idx] += pending;
            line.Total += pending;
            line.BillCount += 1;
        }

        foreach (var bucket in buckets)
            bucket.PendingAmount = Math.Round(bucket.PendingAmount, 2, MidpointRounding.AwayFromZero);

        var customers = map.Values
            .Select(c =>
            {
                for (var i = 0; i < c.Amounts.Length; i++)
                    c.Amounts[i] = Math.Round(c.Amounts[i], 2, MidpointRounding.AwayFromZero);
                c.Total = Math.Round(c.Total, 2, MidpointRounding.AwayFromZero);
                return c;
            })
            .Where(c => Math.Round(Math.Abs(c.Total), 0) != 0)
            .OrderByDescending(c => c.Total)
            .ThenBy(c => c.CustomerName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ExportAgingReportDto
        {
            Company = companyLabel,
            DateFrom = fromDate.ToString("yyyy-MM-dd"),
            AsOf = asOfDate.ToString("yyyy-MM-dd"),
            GroupName = selectedGroup,
            TotalPending = Math.Round(totalPending, 2, MidpointRounding.AwayFromZero),
            TotalBills = totalBills,
            Buckets = buckets,
            Customers = customers,
        };
    }

    /// <summary>
    /// Preload companies, groups, overdue + aging so the UI opens on a cache hit.
    /// Aging reuses the same bill universe as overdue (no second SQL when already warm).
    /// </summary>
    public async Task WarmDefaultCachesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.WhenAll(GetCompanyOptionsAsync(), GetGroupsAsync(), GetCompaniesAsync());

        var today = DateTime.Today;
        var yesterday = today.AddDays(-1);

        cancellationToken.ThrowIfCancellationRequested();
        // Load universe once via overdue, then age in-memory (and cache the report DTO).
        await GetOverdueBillsAsync("All Companies", today, DefaultGroupName, 1, DefaultPageSize);
        cancellationToken.ThrowIfCancellationRequested();
        await GetAgingReportAsync("All Companies", today, DefaultGroupName);

        cancellationToken.ThrowIfCancellationRequested();
        await GetOverdueBillsAsync("All Companies", yesterday, DefaultGroupName, 1, DefaultPageSize);
        cancellationToken.ThrowIfCancellationRequested();
        await GetAgingReportAsync("All Companies", yesterday, DefaultGroupName);
    }

    private static string UniverseCacheKey(DateTime asOfDate, string selectedGroup) =>
        $"export-bill-overdue-v26|{asOfDate:yyyy-MM-dd}|{selectedGroup}";

    private static string AgingReportCacheKey(string universeKey, string companyLabel, DateTime fromDate) =>
        $"{universeKey}|aging-signed-v1|{companyLabel}|{fromDate:yyyy-MM-dd}";

    public async Task<byte[]> BuildExportAsync(
        string company,
        DateTime asOf,
        string? groupName,
        DateTime? dateFrom = null)
    {
        var selectedGroup = string.IsNullOrWhiteSpace(groupName) ? DefaultGroupName : groupName.Trim();
        if (!IsExportReceivableGroup(selectedGroup))
            selectedGroup = DefaultGroupName;

        var companyLabel = string.IsNullOrWhiteSpace(company) ? "All Companies" : company.Trim();
        var asOfDate = asOf.Date;
        var fromDate = ResolveDateFrom(dateFrom, asOfDate);
        var universeKey = UniverseCacheKey(asOfDate, selectedGroup);
        var excelKey = $"{universeKey}|{companyLabel}|xlsx-v14|{fromDate:yyyy-MM-dd}";
        if (_cache.TryGetValue(excelKey, out byte[]? cachedExcel) && cachedExcel is { Length: > 0 })
            return cachedExcel;

        var universe = await GetOrLoadRowsAsync(universeKey, asOfDate, selectedGroup);
        var rows = await FilterRowsByCompanyAsync(universe, companyLabel);
        var ic = await GetIntercompanyExclusionAsync();
        var bytes = BuildAttractiveWorkbook(
            ExcludeIntercompany(rows, ic),
            rows,
            companyLabel,
            selectedGroup,
            fromDate,
            asOfDate);
        _cache.Set(excelKey, bytes, TimeSpan.FromMinutes(30));
        return bytes;
    }

    public async Task<byte[]> BuildOverduePdfAsync(string company, DateTime asOf, string? groupName, DateTime? dateFrom = null)
    {
        var (rows, companyLabel, selectedGroup, asOfDate) = await LoadFilteredRowsAsync(company, asOf, groupName);
        var display = DisplayCompanyLabel(companyLabel);
        var fromDate = ResolveDateFrom(dateFrom, asOfDate);
        var ic = await GetIntercompanyExclusionAsync();
        return new ExportBillOverduePdfDocument(
                OverdueOnly(ExcludeIntercompany(rows, ic)),
                display,
                selectedGroup,
                fromDate,
                asOfDate,
                ShowCompanyColumn(companyLabel))
            .GeneratePdf();
    }

    public async Task<byte[]> BuildAgingPdfAsync(string company, DateTime asOf, string? groupName, DateTime? dateFrom = null)
    {
        var report = await GetAgingReportAsync(company, asOf, groupName, refresh: false, dateFrom);
        return new ExportBillAgingPdfDocument(report, DisplayCompanyLabel(report.Company), ShowCompanyColumn(report.Company))
            .GeneratePdf();
    }

    private async Task<(List<ExportBillOverdueItemDto> Rows, string CompanyLabel, string GroupName, DateTime AsOf)> LoadFilteredRowsAsync(
        string company,
        DateTime asOf,
        string? groupName)
    {
        var selectedGroup = string.IsNullOrWhiteSpace(groupName) ? DefaultGroupName : groupName.Trim();
        if (!IsExportReceivableGroup(selectedGroup))
            selectedGroup = DefaultGroupName;
        var companyLabel = string.IsNullOrWhiteSpace(company) ? "All Companies" : company.Trim();
        var asOfDate = asOf.Date;
        var universe = await GetOrLoadRowsAsync(UniverseCacheKey(asOfDate, selectedGroup), asOfDate, selectedGroup);
        var rows = await FilterRowsByCompanyAsync(universe, companyLabel);
        return (rows, companyLabel, selectedGroup, asOfDate);
    }

    private static string DisplayCompanyLabel(string companyLabel) =>
        companyLabel.StartsWith("G-", StringComparison.OrdinalIgnoreCase)
            ? companyLabel[2..] + " (Group)"
            : companyLabel;

    private static bool ShowCompanyColumn(string companyLabel) =>
        companyLabel.Equals("All Companies", StringComparison.OrdinalIgnoreCase)
        || companyLabel.Contains("(All)", StringComparison.OrdinalIgnoreCase)
        || companyLabel.StartsWith("G-", StringComparison.OrdinalIgnoreCase);

    private static byte[] BuildAttractiveWorkbook(
        List<ExportBillOverdueItemDto> overdueUniverse,
        List<ExportBillOverdueItemDto> agingUniverse,
        string companyLabel,
        string selectedGroup,
        DateTime fromDate,
        DateTime asOfDate)
    {
        var displayGroup = companyLabel.StartsWith("G-", StringComparison.OrdinalIgnoreCase)
            ? companyLabel[2..] + " (Group)"
            : companyLabel;
        var overdueRows = OverdueOnly(overdueUniverse);
        var totalPending = overdueRows.Sum(r => r.PendingAmount);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Overdue bills");
        sheet.Style.Font.FontName = "Calibri";
        sheet.Style.Font.FontSize = 11;
        sheet.ShowGridLines = false;

        var navy = XLColor.FromHtml("#0B3A5B");
        var headerBlue = XLColor.FromHtml("#1565A8");
        var gold = XLColor.FromHtml("#C9A227");
        var kpiFill = XLColor.FromHtml("#F4F8FC");

        sheet.Range(1, 1, 1, 10).Merge();
        var title = sheet.Cell(1, 1);
        title.Value = "EXPORT BILL OVERDUE";
        title.Style.Font.Bold = true;
        title.Style.Font.FontSize = 20;
        title.Style.Font.FontColor = XLColor.White;
        title.Style.Fill.BackgroundColor = navy;
        title.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        title.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        title.Style.Alignment.Indent = 1;
        sheet.Row(1).Height = 32;

        sheet.Range(2, 1, 2, 10).Merge();
        var subtitle = sheet.Cell(2, 1);
        subtitle.Value = $"Overseas receivables  ·  Opening + Debit − Credit  ·  {fromDate:dd-MMM-yyyy} to {asOfDate:dd-MMM-yyyy}  ·  Generated {DateTime.Now:dd-MMM-yyyy HH:mm}";
        subtitle.Style.Font.FontColor = XLColor.White;
        subtitle.Style.Font.FontSize = 10;
        subtitle.Style.Fill.BackgroundColor = headerBlue;
        subtitle.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        subtitle.Style.Alignment.Indent = 1;
        sheet.Row(2).Height = 20;

        sheet.Range(3, 1, 3, 10).Merge();
        sheet.Cell(3, 1).Style.Fill.BackgroundColor = gold;
        sheet.Row(3).Height = 4;

        WriteKpi(sheet, 5, 1, "Company group", displayGroup, navy, kpiFill, 2);
        WriteKpi(sheet, 5, 3, "Ledger group", selectedGroup, navy, kpiFill, 2);
        WriteKpi(sheet, 5, 5, "From date", fromDate.ToString("dd-MMM-yyyy"), navy, kpiFill, 1);
        WriteKpi(sheet, 5, 6, "To date", asOfDate.ToString("dd-MMM-yyyy"), navy, kpiFill, 1);
        WriteKpi(sheet, 5, 7, "Bills", overdueRows.Count.ToString("N0"), navy, kpiFill, 1);
        WriteKpi(sheet, 5, 8, "Pending (INR)", totalPending, navy, kpiFill, 2);

        const int headerRow = 8;
        var headers = new[]
        {
            "Company", "Customer name", "Bill no", "Bill date",
            "Amount (INR)", "Currency", "Foreign amount", "Due date", "Overdue days", "Pending (INR)",
        };
        for (var i = 0; i < headers.Length; i++)
            sheet.Cell(headerRow, i + 1).Value = headers[i];

        var data = overdueRows.Select(r => new object?[]
        {
            r.CompanyName,
            string.IsNullOrWhiteSpace(r.CustomerName) ? r.LedgerName : r.CustomerName,
            r.BillNo,
            ParseIsoDate(r.BillDate),
            r.BillAmount,
            r.BillCurrency,
            r.ForeignAmount > 0 ? r.ForeignAmount : null,
            ParseIsoDate(r.DueDate),
            r.OverdueDays,
            r.PendingAmount,
        });
        if (overdueRows.Count > 0)
            sheet.Cell(headerRow + 1, 1).InsertData(data);

        var lastDataRow = headerRow + Math.Max(overdueRows.Count, 1);
        if (overdueRows.Count > 0)
        {
            var tableRange = sheet.Range(headerRow, 1, lastDataRow, headers.Length);
            var table = tableRange.CreateTable("OverdueBills");
            table.Theme = XLTableTheme.TableStyleMedium2;
            table.ShowAutoFilter = true;
            table.ShowTotalsRow = true;
            table.Field("Company").TotalsRowLabel = "Total";
            table.Field("Amount (INR)").TotalsRowFunction = XLTotalsRowFunction.Sum;
            table.Field("Foreign amount").TotalsRowFunction = XLTotalsRowFunction.Sum;
            table.Field("Pending (INR)").TotalsRowFunction = XLTotalsRowFunction.Sum;
            table.Field("Overdue days").TotalsRowFunction = XLTotalsRowFunction.Average;
        }

        var headerRange = sheet.Range(headerRow, 1, headerRow, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Fill.BackgroundColor = navy;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        sheet.Row(headerRow).Height = 22;

        if (overdueRows.Count > 0)
        {
            sheet.Range(headerRow + 1, 5, lastDataRow, 5).Style.NumberFormat.Format = "#,##0.00";
            sheet.Range(headerRow + 1, 7, lastDataRow, 7).Style.NumberFormat.Format = "#,##0.00";
            sheet.Range(headerRow + 1, 10, lastDataRow, 10).Style.NumberFormat.Format = "#,##0.00";
            sheet.Range(headerRow + 1, 4, lastDataRow, 4).Style.DateFormat.Format = "dd-mmm-yyyy";
            sheet.Range(headerRow + 1, 8, lastDataRow, 8).Style.DateFormat.Format = "dd-mmm-yyyy";
            sheet.Range(headerRow + 1, 5, lastDataRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            sheet.Range(headerRow + 1, 7, lastDataRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            sheet.Range(headerRow + 1, 9, lastDataRow, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            sheet.Range(headerRow + 1, 6, lastDataRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            sheet.Range(headerRow + 1, 9, lastDataRow, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        sheet.Column(1).Width = 28;
        sheet.Column(2).Width = 34;
        sheet.Column(3).Width = 22;
        sheet.Column(4).Width = 14;
        sheet.Column(5).Width = 16;
        sheet.Column(6).Width = 11;
        sheet.Column(7).Width = 16;
        sheet.Column(8).Width = 14;
        sheet.Column(9).Width = 14;
        sheet.Column(10).Width = 16;

        sheet.SheetView.FreezeRows(headerRow);
        sheet.SheetView.FreezeColumns(2);
        sheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        sheet.PageSetup.FitToPages(1, 0);
        sheet.PageSetup.Header.Left.AddText("Export Bill Overdue");
        sheet.PageSetup.Footer.Right.AddText("Page &P of &N");

        WriteAgingSheet(workbook, agingUniverse, companyLabel, selectedGroup, fromDate, asOfDate, navy, headerBlue, gold, kpiFill);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteAgingSheet(
        XLWorkbook workbook,
        List<ExportBillOverdueItemDto> rows,
        string companyLabel,
        string selectedGroup,
        DateTime fromDate,
        DateTime asOfDate,
        XLColor navy,
        XLColor headerBlue,
        XLColor gold,
        XLColor kpiFill)
    {
        var report = BuildAgingReport(rows, companyLabel, selectedGroup, fromDate, asOfDate);
        var showCompany = companyLabel.Equals("All Companies", StringComparison.OrdinalIgnoreCase)
            || companyLabel.StartsWith("G-", StringComparison.OrdinalIgnoreCase);
        var sheet = workbook.Worksheets.Add("Aging");
        sheet.Style.Font.FontName = "Calibri";
        sheet.Style.Font.FontSize = 11;
        sheet.ShowGridLines = false;

        var lastCol = (showCompany ? 3 : 2) + AgingBucketLabels.Length;
        sheet.Range(1, 1, 1, lastCol).Merge();
        var title = sheet.Cell(1, 1);
        title.Value = "EXPORT BILL AGING";
        title.Style.Font.Bold = true;
        title.Style.Font.FontSize = 20;
        title.Style.Font.FontColor = XLColor.White;
        title.Style.Fill.BackgroundColor = navy;
        title.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        title.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        title.Style.Alignment.Indent = 1;
        sheet.Row(1).Height = 32;

        sheet.Range(2, 1, 2, lastCol).Merge();
        var subtitle = sheet.Cell(2, 1);
        subtitle.Value = $"ERP aging (bill date, signed)  ·  Opening + Debit − Credit  ·  {fromDate:dd-MMM-yyyy} to {asOfDate:dd-MMM-yyyy}  ·  Generated {DateTime.Now:dd-MMM-yyyy HH:mm}";
        subtitle.Style.Font.FontColor = XLColor.White;
        subtitle.Style.Font.FontSize = 10;
        subtitle.Style.Fill.BackgroundColor = headerBlue;
        subtitle.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        subtitle.Style.Alignment.Indent = 1;
        sheet.Row(2).Height = 20;

        sheet.Range(3, 1, 3, lastCol).Merge();
        sheet.Cell(3, 1).Style.Fill.BackgroundColor = gold;
        sheet.Row(3).Height = 4;

        WriteKpi(sheet, 5, 1, "Company group", companyLabel.StartsWith("G-", StringComparison.OrdinalIgnoreCase) ? companyLabel[2..] + " (Group)" : companyLabel, navy, kpiFill, 2);
        WriteKpi(sheet, 5, 3, "Ledger group", selectedGroup, navy, kpiFill, 2);
        WriteKpi(sheet, 5, 5, "Customers", report.Customers.Count.ToString("N0"), navy, kpiFill, 1);
        WriteKpi(sheet, 5, 6, "Pending (INR)", report.TotalPending, navy, kpiFill, 2);

        const int headerRow = 8;
        var headers = new List<string>();
        if (showCompany) headers.Add("Company");
        headers.Add("Customer");
        headers.AddRange(AgingBucketLabels.Select(AgingBucketHeading));
        headers.Add("Total (INR)");

        for (var i = 0; i < headers.Count; i++)
            sheet.Cell(headerRow, i + 1).Value = headers[i];

        var data = report.Customers.Select(c =>
        {
            var cells = new List<object?>();
            if (showCompany) cells.Add(c.CompanyName);
            cells.Add(c.CustomerName);
            cells.AddRange(c.Amounts.Cast<object?>());
            cells.Add(c.Total);
            return cells.ToArray();
        });
        if (report.Customers.Count > 0)
            sheet.Cell(headerRow + 1, 1).InsertData(data);

        var lastDataRow = headerRow + Math.Max(report.Customers.Count, 1);
        var headerRange = sheet.Range(headerRow, 1, headerRow, headers.Count);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Fill.BackgroundColor = navy;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        sheet.Row(headerRow).Height = 22;

        var firstAmtCol = showCompany ? 3 : 2;
        if (report.Customers.Count > 0)
        {
            sheet.Range(headerRow + 1, firstAmtCol, lastDataRow, headers.Count).Style.NumberFormat.Format = "#,##0.00";
            sheet.Range(headerRow + 1, firstAmtCol, lastDataRow, headers.Count).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        }

        sheet.Column(1).Width = showCompany ? 28 : 36;
        if (showCompany) sheet.Column(2).Width = 34;
        for (var c = firstAmtCol; c <= headers.Count; c++)
            sheet.Column(c).Width = 14;

        sheet.SheetView.FreezeRows(headerRow);
        sheet.SheetView.FreezeColumns(showCompany ? 2 : 1);
        sheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        sheet.PageSetup.FitToPages(1, 0);
    }

    private static void WriteKpi(
        IXLWorksheet sheet,
        int row,
        int col,
        string label,
        object value,
        XLColor navy,
        XLColor fill,
        int mergeCols)
    {
        var lastCol = col + mergeCols - 1;
        sheet.Range(row, col, row, lastCol).Merge();
        sheet.Range(row + 1, col, row + 1, lastCol).Merge();
        var labelCell = sheet.Cell(row, col);
        labelCell.Value = label.ToUpperInvariant();
        labelCell.Style.Font.FontSize = 8;
        labelCell.Style.Font.Bold = true;
        labelCell.Style.Font.FontColor = navy;
        labelCell.Style.Fill.BackgroundColor = fill;
        labelCell.Style.Alignment.Indent = 1;
        var valueCell = sheet.Cell(row + 1, col);
        if (value is double d)
        {
            valueCell.Value = d;
            valueCell.Style.NumberFormat.Format = "#,##0.00";
        }
        else
        {
            valueCell.Value = Convert.ToString(value) ?? "";
        }
        valueCell.Style.Font.Bold = true;
        valueCell.Style.Font.FontSize = 12;
        valueCell.Style.Font.FontColor = navy;
        valueCell.Style.Fill.BackgroundColor = fill;
        valueCell.Style.Alignment.Indent = 1;
        sheet.Range(row, col, row + 1, lastCol).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        sheet.Range(row, col, row + 1, lastCol).Style.Border.OutsideBorderColor = XLColor.FromHtml("#D5E3EF");
        sheet.Row(row).Height = 16;
        sheet.Row(row + 1).Height = 22;
    }

    private static bool MatchesSearch(ExportBillOverdueItemDto row, string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return true;
        var q = search.Trim();
        if (ContainsIgnoreCase(row.CompanyName, q) ||
            ContainsIgnoreCase(row.CustomerName, q) ||
            ContainsIgnoreCase(row.LedgerName, q) ||
            ContainsIgnoreCase(row.BillNo, q) ||
            ContainsIgnoreCase(row.BillCurrency, q) ||
            ContainsIgnoreCase(row.BillDate, q) ||
            ContainsIgnoreCase(row.DueDate, q) ||
            ContainsIgnoreCase(row.OverdueDays.ToString(CultureInfo.InvariantCulture), q))
            return true;

        if (DateTime.TryParse(row.BillDate, out var billDate) &&
            (ContainsIgnoreCase(billDate.ToString("dd-MM-yyyy"), q) ||
             ContainsIgnoreCase(billDate.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture), q)))
            return true;
        if (DateTime.TryParse(row.DueDate, out var dueDate) &&
            (ContainsIgnoreCase(dueDate.ToString("dd-MM-yyyy"), q) ||
             ContainsIgnoreCase(dueDate.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture), q)))
            return true;

        return AmountMatches(row.PendingAmount, q) ||
               AmountMatches(row.BillAmount, q) ||
               AmountMatches(row.ForeignAmount, q);
    }

    private static bool AmountMatches(double amount, string query)
    {
        var invariant = amount.ToString("0.##", CultureInfo.InvariantCulture);
        var rounded = Math.Round(amount, 0, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.InvariantCulture);
        var indian = amount.ToString("N2", CultureInfo.GetCultureInfo("en-IN"));
        return ContainsIgnoreCase(invariant, query) ||
               ContainsIgnoreCase(rounded, query) ||
               ContainsIgnoreCase(indian, query) ||
               ContainsIgnoreCase(indian.Replace(",", ""), query);
    }

    private static bool ContainsIgnoreCase(string? value, string query) =>
        !string.IsNullOrEmpty(value) &&
        value.Contains(query, StringComparison.OrdinalIgnoreCase);

    private static DateTime? ParseIsoDate(string iso)
    {
        if (string.IsNullOrWhiteSpace(iso) || iso.StartsWith("1900-01-01", StringComparison.Ordinal))
            return null;
        if (DateTime.TryParseExact(iso, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return d.Date;
        if (DateTime.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return parsed.Date;
        return null;
    }

    private async Task<List<ExportBillOverdueItemDto>> GetOrLoadRowsAsync(
        string cacheKey,
        DateTime asOfDate,
        string selectedGroup)
    {
        if (_cache.TryGetValue(cacheKey, out List<ExportBillOverdueItemDto>? cached) && cached is not null)
            return cached;

        var gate = LoadLocks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            if (_cache.TryGetValue(cacheKey, out cached) && cached is not null)
                return cached;

            var loaded = await LoadAllRowsAsync(asOfDate, selectedGroup);
            _cache.Set(cacheKey, loaded, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheTtl,
            });
            return loaded;
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<List<ExportBillOverdueItemDto>> FilterRowsByCompanyAsync(
        List<ExportBillOverdueItemDto> universe,
        string companyLabel)
    {
        if (string.IsNullOrWhiteSpace(companyLabel) ||
            companyLabel.Equals("All Companies", StringComparison.OrdinalIgnoreCase) ||
            companyLabel.Contains("(All)", StringComparison.OrdinalIgnoreCase))
            return universe;

        var names = await ResolveSelectedCompaniesAsync(companyLabel);
        if (names.Count == 0)
            return new List<ExportBillOverdueItemDto>();

        var set = names.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return universe.Where(r => set.Contains(r.CompanyName)).ToList();
    }

    private async Task<List<ExportBillOverdueItemDto>> LoadAllRowsAsync(
        DateTime asOfDate,
        string selectedGroup)
    {
        var factories = await GetFactoryRowsAsync();
        if (factories.Count == 0)
            return new List<ExportBillOverdueItemDto>();

        // Keep the full ERP outstanding universe (incl. IC). Overdue grid strips IC at read time.
        var fxTask = GetFxRatesAsync(asOfDate);
        var rowsTask = QueryAllSelectedAsync(asOfDate, selectedGroup);
        await Task.WhenAll(fxTask, rowsTask);
        var fx = await fxTask;
        var rows = await rowsTask;

        return rows
            .Select(r => ApplyForeignAmount(r, fx))
            .OrderByDescending(r => r.OverdueDays)
            .ThenBy(r => r.LedgerName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.BillNo, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<IReadOnlyList<FactoryRow>> GetFactoryRowsAsync()
    {
        const string key = "export-bill-overdue-factory-rows-v1";
        if (_cache.TryGetValue(key, out IReadOnlyList<FactoryRow>? cached) && cached is not null)
            return cached;

        using var connection = _database.CreateConnection();
        var rows = (await connection.QueryAsync<FactoryRow>(
            @"SELECT fi.srno AS SrNo,
                     LTRIM(RTRIM(fi.Name)) AS Name,
                     LTRIM(RTRIM(ISNULL(fi.GroupName, N''))) AS GroupName
              FROM FactoryInfo fi WITH (NOLOCK)
              WHERE ISNULL(fi.Name, '') <> ''
              ORDER BY fi.Name")).ToList();

        _cache.Set(key, (IReadOnlyList<FactoryRow>)rows, MetaCacheTtl);
        return rows;
    }

    private async Task<List<string>> ResolveSelectedCompaniesAsync(string companyLabel)
    {
        var factories = await GetFactoryRowsAsync();
        var allNames = factories
            .Select(f => f.Name)
            .Where(n => n.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var tokens = (companyLabel ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length > 0)
            .ToList();

        if (tokens.Count == 0)
            return new List<string>();

        if (tokens.Any(t =>
                t.Equals("All Companies", StringComparison.OrdinalIgnoreCase) ||
                t.Contains("(All)", StringComparison.OrdinalIgnoreCase)))
            return allNames;

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var token in tokens)
        {
            if (token.StartsWith("G-", StringComparison.OrdinalIgnoreCase))
            {
                var group = token[2..].Trim();
                foreach (var factory in factories.Where(f =>
                    f.GroupName.Equals(group, StringComparison.OrdinalIgnoreCase)))
                    names.Add(factory.Name);
                continue;
            }

            if (token.StartsWith("C-", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(token[2..].Trim(), out var companyId) &&
                companyId > 0)
            {
                var factory = factories.FirstOrDefault(f => f.SrNo == companyId);
                if (factory is not null && factory.Name.Length > 0)
                    names.Add(factory.Name);
                continue;
            }

            var named = factories.FirstOrDefault(f =>
                f.Name.Equals(token, StringComparison.OrdinalIgnoreCase));
            if (named is not null)
                names.Add(named.Name);
        }

        return names.ToList();
    }

    private async Task<List<ExportBillOverdueItemDto>> QueryAllSelectedAsync(
        DateTime asOf,
        string groupName)
    {
        using var connection = _database.CreateConnection();
        return await QueryCompaniesFastAsync(connection, asOf, groupName);
    }

    /// <summary>
    /// ERP FrmReceivable BindGrid against SQL:
    /// pending = SUM(amount) per bill (Opening + Debit − Credit).
    /// accountbills is grouped so the join cannot duplicate rows.
    /// </summary>
    private static async Task<List<ExportBillOverdueItemDto>> QueryCompaniesFastAsync(
        SqlConnection connection,
        DateTime asOf,
        string groupName)
    {
        var filterByGroup = !string.IsNullOrWhiteSpace(groupName);

        var groupRestrict = filterByGroup
            ? @"
        INNER JOIN (
            SELECT DISTINCT
                LTRIM(RTRIM(ledgername)) AS ledgername,
                LTRIM(RTRIM(companyname)) AS companyname
            FROM vw_ledgergrouping
            WHERE @GroupName IN (expensehead, expensegrouphead, b, c, d, e, f, g)
        ) grp
            ON grp.ledgername = LTRIM(RTRIM(v1.ledgername))
           AND grp.companyname = LTRIM(RTRIM(v1.CompanyName))"
            : "";

        var sql = $@"
SELECT
    CompanyName,
    LedgerName,
    LedgerName AS CustomerName,
    billno AS BillNo,
    BillDate,
    ROUND(BillAmount, 2) AS BillAmount,
    DueDate,
    CASE
        WHEN DueDate IS NULL OR DueDate <= '1900-01-01' THEN 0
        WHEN DATEDIFF(DAY, DueDate, @AsOf) < 0 THEN 0
        ELSE DATEDIFF(DAY, DueDate, @AsOf)
    END AS OverdueDays,
    ROUND(dueamount, 2) AS PendingAmount,
    DisplayCurrency AS BillCurrency,
    CAST(0 AS float) AS ForeignAmount
FROM (
    SELECT
        CompanyName,
        LedgerName,
        billno,
        MAX(BillDate) AS BillDate,
        MAX(DueDate) AS DueDate,
        MAX(DisplayCurrency) AS DisplayCurrency,
        MAX(BillAmount) AS BillAmount,
        SUM(amount) AS dueamount
    FROM (
        SELECT
            v1.companyname AS CompanyName,
            v1.ledgername AS LedgerName,
            CASE WHEN ISNULL(v1.billno, '') = '' THEN 'On Account' ELSE v1.billno END AS billno,
            CASE WHEN ISNULL(v1.billno, '') = '' THEN CAST('1900-01-01' AS datetime) ELSE bills.billdate END AS BillDate,
            CASE WHEN ISNULL(v1.billno, '') = '' THEN CAST('1900-01-01' AS datetime) ELSE bills.duedate END AS DueDate,
            ISNULL(NULLIF(LTRIM(RTRIM(bills.BillCurrency)), ''), ISNULL(NULLIF(LTRIM(RTRIM(v1.Currency)), ''), 'Rs.')) AS DisplayCurrency,
            CASE WHEN ISNULL(v1.billno, '') = '' THEN 0 ELSE ISNULL(bills.billamount, 0) END AS BillAmount,
            ISNULL(v1.amount, 0) AS amount
        FROM vw_billwisetransactionwithonaccount v1
        {groupRestrict}
        LEFT JOIN (
            SELECT
                companyid,
                ledgername,
                billno,
                CompanyName,
                LedgerId,
                MAX(billdate) AS billdate,
                MAX(duedate) AS duedate,
                MAX(BillCurrency) AS BillCurrency,
                MAX(billamount) AS billamount
            FROM accountbills
            GROUP BY companyid, ledgername, billno, CompanyName, LedgerId
        ) bills
            ON v1.companyid = bills.companyid
           AND v1.ledgername = bills.ledgername
           AND v1.billno = bills.billno
           AND v1.CompanyName = bills.CompanyName
           AND v1.ledgerid = bills.LedgerId
        WHERE v1.isbillwise = 'yes'
          AND v1.voucherdate <= @AsOf
    ) AS t1
    GROUP BY CompanyName, LedgerName, billno
    HAVING ROUND(ABS(SUM(amount)), 0) <> 0
) AS BillWiseDetail";

        var rows = await connection.QueryAsync<ExportBillOverdueRow>(
            sql,
            new
            {
                AsOf = asOf,
                GroupName = groupName,
            },
            commandTimeout: CommandTimeoutSeconds);

        return rows.Select(r => new ExportBillOverdueItemDto
        {
            CompanyName = (r.CompanyName ?? "").Trim(),
            LedgerName = (r.LedgerName ?? "").Trim(),
            CustomerName = (r.CustomerName ?? r.LedgerName ?? "").Trim(),
            BillNo = (r.BillNo ?? "").Trim(),
            BillDate = r.BillDate?.ToString("yyyy-MM-dd") ?? "",
            BillAmount = r.BillAmount,
            DueDate = r.DueDate?.ToString("yyyy-MM-dd") ?? "",
            OverdueDays = r.OverdueDays,
            PendingAmount = Math.Round(r.PendingAmount, 2, MidpointRounding.AwayFromZero),
            BillCurrency = NormalizeCurrency(r.BillCurrency),
            ForeignAmount = 0,
        }).ToList();
    }

    private async Task<IcExclusion> GetIntercompanyExclusionAsync()
    {
        const string key = "export-bill-overdue-ic-excl-v1";
        if (_cache.TryGetValue(key, out IcExclusion? cached) && cached is not null)
            return cached;

        using var connection = _database.CreateConnection();

        var flagged = await connection.QueryAsync<(string CompanyName, string LedgerName)>(
            @"
SELECT LTRIM(RTRIM(CompanyName)) AS CompanyName, LTRIM(RTRIM(LedgerName)) AS LedgerName
FROM CommonLedgerMaster WITH (NOLOCK)
WHERE LOWER(LTRIM(RTRIM(CONVERT(nvarchar(20), ISNULL(IsInterCompany, 'no'))))) IN ('yes', 'y', '1', 'true')

UNION

SELECT LTRIM(RTRIM(l.CompanyName)), LTRIM(RTRIM(l.LedgerName))
FROM ac_interCompanyLedger icl WITH (NOLOCK)
INNER JOIN LedgerMaster l WITH (NOLOCK) ON icl.LedgerId = l.srno",
            commandTimeout: 60);

        var factoryNames = await connection.QueryAsync<string>(
            @"SELECT LTRIM(RTRIM(Name))
              FROM FactoryInfo WITH (NOLOCK)
              WHERE ISNULL(Name, '') <> ''",
            commandTimeout: 30);

        var pairKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in flagged)
        {
            if (string.IsNullOrWhiteSpace(row.CompanyName) || string.IsNullOrWhiteSpace(row.LedgerName))
                continue;
            pairKeys.Add(IcKey(row.CompanyName, row.LedgerName));
        }

        var factories = new HashSet<string>(
            factoryNames.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n.Trim()),
            StringComparer.OrdinalIgnoreCase);

        var excl = new IcExclusion { PairKeys = pairKeys, FactoryNames = factories };
        _cache.Set(key, excl, MetaCacheTtl);
        return excl;
    }

    private async Task<FxRates> GetFxRatesAsync(DateTime asOf)
    {
        var key = $"export-bill-overdue-fx|{asOf:yyyy-MM-dd}";
        if (_cache.TryGetValue(key, out FxRates? cached) && cached is not null)
            return cached;

        using var connection = _database.CreateConnection();
        var row = await connection.QueryFirstOrDefaultAsync<FxRates>(
            @"SELECT TOP 1
                  CAST(ISNULL(Dollar, 1) AS float) AS Dollar,
                  CAST(ISNULL(Euro, 1) AS float) AS Euro,
                  CAST(ISNULL(Pound, 1) AS float) AS Pound,
                  CAST(ISNULL(CHF, 1) AS float) AS CHF
              FROM currency_rbi WITH (NOLOCK)
              WHERE @AsOf BETWEEN sysdate AND ISNULL(todate, DATEADD(YEAR, 5, GETDATE()))
              ORDER BY sysdate DESC",
            new { AsOf = asOf.Date });

        var rates = row ?? new FxRates { Dollar = 1, Euro = 1, Pound = 1, CHF = 1 };
        _cache.Set(key, rates, MetaCacheTtl);
        return rates;
    }

    private static bool IsIntercompanyRow(ExportBillOverdueItemDto row, IcExclusion ic) =>
        ic.PairKeys.Contains(IcKey(row.CompanyName, row.LedgerName)) ||
        ic.FactoryNames.Contains(row.LedgerName);

    private sealed class IcExclusion
    {
        public HashSet<string> PairKeys { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> FactoryNames { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private static ExportBillOverdueItemDto ApplyForeignAmount(ExportBillOverdueItemDto row, FxRates fx)
    {
        var rate = row.BillCurrency switch
        {
            "$" => fx.Dollar,
            "€" => fx.Euro,
            "GBP" => fx.Pound,
            "CHF" => fx.CHF,
            _ => 1d,
        };

        if (rate > 1 && !IsInr(row.BillCurrency))
            row.ForeignAmount = Math.Round(Math.Abs(row.PendingAmount) / rate, 3);
        else
            row.ForeignAmount = 0;

        return row;
    }

    private static bool IsInr(string currency) =>
        string.IsNullOrWhiteSpace(currency) ||
        currency.StartsWith("Rs", StringComparison.OrdinalIgnoreCase) ||
        currency.Equals("INR", StringComparison.OrdinalIgnoreCase) ||
        currency == "₹";

    private static string IcKey(string company, string ledger) =>
        $"{company.Trim()}|{ledger.Trim()}";

    private static string NormalizeCurrency(string? currency)
    {
        var c = (currency ?? "").Trim();
        if (string.IsNullOrEmpty(c)) return "Rs.";
        if (c.StartsWith("Rs", StringComparison.OrdinalIgnoreCase)) return "Rs.";
        if (c is "$" or "USD" or "US$") return "$";
        if (c is "€" or "?" or "EUR" or "Euro") return "€";
        if (c.Equals("GBP", StringComparison.OrdinalIgnoreCase)) return "GBP";
        if (c.Equals("CHF", StringComparison.OrdinalIgnoreCase)) return "CHF";
        return c;
    }

    private sealed class ExportBillOverdueRow
    {
        public string? CompanyName { get; set; }
        public string? LedgerName { get; set; }
        public string? CustomerName { get; set; }
        public string? BillNo { get; set; }
        public DateTime? BillDate { get; set; }
        public double BillAmount { get; set; }
        public DateTime? DueDate { get; set; }
        public int OverdueDays { get; set; }
        public double PendingAmount { get; set; }
        public string? BillCurrency { get; set; }
        public double ForeignAmount { get; set; }
    }

    private sealed class FactoryRow
    {
        public int SrNo { get; set; }
        public string Name { get; set; } = "";
        public string GroupName { get; set; } = "";
    }

    private sealed class FxRates
    {
        public double Dollar { get; set; } = 1;
        public double Euro { get; set; } = 1;
        public double Pound { get; set; } = 1;
        public double CHF { get; set; } = 1;
    }
}

public class ExportCompanyOptionDto
{
    public string Value { get; set; } = "";
    public string Label { get; set; } = "";
    public string Kind { get; set; } = "company";
}

public class ExportBillOverdueItemDto
{
    public string CompanyName { get; set; } = "";
    public string LedgerName { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string BillNo { get; set; } = "";
    public string BillDate { get; set; } = "";
    public double BillAmount { get; set; }
    public string DueDate { get; set; } = "";
    public int OverdueDays { get; set; }
    public double PendingAmount { get; set; }
    public string BillCurrency { get; set; } = "Rs.";
    public double ForeignAmount { get; set; }
}

public class ExportBillOverdueResultDto
{
    public List<ExportBillOverdueItemDto> Items { get; set; } = new();
    public string Company { get; set; } = "";
    public string DateFrom { get; set; } = "";
    public string AsOf { get; set; } = "";
    public string GroupName { get; set; } = "";
    public string Source { get; set; } = "";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = ExportBillOverdueService.DefaultPageSize;
    public int TotalCount { get; set; }
}

public class ExportAgingBucketDto
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public double PendingAmount { get; set; }
    public int BillCount { get; set; }
}

public class ExportAgingCustomerDto
{
    public string CompanyName { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public double[] Amounts { get; set; } = Array.Empty<double>();
    public double Total { get; set; }
    public int BillCount { get; set; }
}

public class ExportAgingReportDto
{
    public string Company { get; set; } = "";
    public string DateFrom { get; set; } = "";
    public string AsOf { get; set; } = "";
    public string GroupName { get; set; } = "";
    public double TotalPending { get; set; }
    public int TotalBills { get; set; }
    public List<ExportAgingBucketDto> Buckets { get; set; } = new();
    public List<ExportAgingCustomerDto> Customers { get; set; } = new();
}
