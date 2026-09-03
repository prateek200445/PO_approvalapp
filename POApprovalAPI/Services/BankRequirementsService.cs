using System.Globalization;
using ClosedXML.Excel;
using Microsoft.Extensions.Caching.Memory;
using POApprovalAPI.Documents;
using QuestPDF.Fluent;

namespace POApprovalAPI.Services;

public sealed class BankRequirementsSalesProfileDto
{
    public string Company { get; set; } = "All Companies";
    public string DateFrom { get; set; } = "";
    public string DateTo { get; set; } = "";
    public string PeriodLabel { get; set; } = "";
    public string[] Months { get; set; } = [];
    public double ExportAmount { get; set; }
    public double DomesticAmount { get; set; }
    public double TotalAmount { get; set; }
    public double ExportAmountCr { get; set; }
    public double DomesticAmountCr { get; set; }
    public double TotalAmountCr { get; set; }
    public double ExportShare { get; set; }
    public double DomesticShare { get; set; }
    public string Source { get; set; } = "SalesDashboard";
    public string Note { get; set; } =
        "Profile of Sales (Details may be provided on approximate basis). Amounts in INR Crore from sales invoices (taxable), excluding InterUnit and job/other sales.";
}

public sealed class BankRequirementsService
{
    private const double Crore = 10_000_000d;

    private readonly SalesDashboardService _sales;
    private readonly IMemoryCache _cache;

    public BankRequirementsService(SalesDashboardService sales, IMemoryCache cache)
    {
        _sales = sales;
        _cache = cache;
    }

    public async Task<BankRequirementsSalesProfileDto> GetSalesProfileAsync(
        string company,
        IReadOnlyList<string> months,
        bool refresh = false)
    {
        var selectedMonths = NormalizeMonths(months);
        if (selectedMonths.Count == 0)
            selectedMonths = DefaultFyMonths();

        var companyKey = string.IsNullOrWhiteSpace(company) ? "All Companies" : company.Trim();
        var monthKey = string.Join(",", selectedMonths);
        var cacheKey = $"bank-sales-profile-v16-taxable-sales-export:{companyKey}:{monthKey}";

        if (!refresh && _cache.TryGetValue(cacheKey, out BankRequirementsSalesProfileDto? hit) && hit != null)
            return hit;

        double exportAmt = 0;
        double domesticAmt = 0;
        string source = "SalesDashboard";
        foreach (var range in ContiguousRanges(selectedMonths))
        {
            var split = await _sales.GetBankSalesSplitAsync(companyKey, range.From, range.To, refresh);
            exportAmt += split.ExportSales;
            domesticAmt += split.DomesticSales;
            source = split.Source;
        }

        if (exportAmt < 0) exportAmt = 0;
        if (domesticAmt < 0) domesticAmt = 0;
        var total = exportAmt + domesticAmt;
        var dateFrom = CapToToday(FirstDay(selectedMonths[0]));
        var dateTo = CapToToday(LastDay(selectedMonths[^1]));

        var dto = new BankRequirementsSalesProfileDto
        {
            Company = companyKey,
            DateFrom = dateFrom.ToString("yyyy-MM-dd"),
            DateTo = dateTo.ToString("yyyy-MM-dd"),
            PeriodLabel = FormatPeriodLabel(selectedMonths),
            Months = selectedMonths.ToArray(),
            ExportAmount = Math.Round(exportAmt, 0),
            DomesticAmount = Math.Round(domesticAmt, 0),
            TotalAmount = Math.Round(total, 0),
            ExportAmountCr = RoundCr(exportAmt),
            DomesticAmountCr = RoundCr(domesticAmt),
            TotalAmountCr = RoundCr(total),
            ExportShare = Share(exportAmt, total),
            DomesticShare = Share(domesticAmt, total),
            Source = source,
        };

        _cache.Set(cacheKey, dto, TimeSpan.FromHours(4));
        return dto;
    }

    public async Task<byte[]> BuildExcelAsync(string company, IReadOnlyList<string> months, bool refresh = false)
    {
        var data = await GetSalesProfileAsync(company, months, refresh);
        var navy = XLColor.FromHtml("#0B3A5B");
        var headerBlue = XLColor.FromHtml("#1565A8");
        var gold = XLColor.FromHtml("#C9A227");

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Profile of Sales");
        sheet.Cell(1, 1).Value = "Profile of Sales (Details may be provided on approximate basis)";
        sheet.Range(1, 1, 1, 4).Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(navy);
        sheet.Cell(2, 1).Value = $"Company: {data.Company}    Period: {data.PeriodLabel}";
        sheet.Range(2, 1, 2, 4).Merge().Style.Font.SetFontColor(headerBlue);

        sheet.Cell(4, 1).Value = "Sr. No.";
        sheet.Cell(4, 2).Value = "Revenue Streams";
        sheet.Cell(4, 3).Value = data.PeriodLabel;
        sheet.Range(4, 3, 4, 4).Merge();
        sheet.Range(4, 1, 4, 4).Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(navy);
        sheet.Range(4, 1, 4, 4).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        sheet.Cell(5, 1).Value = "";
        sheet.Cell(5, 2).Value = "";
        sheet.Cell(5, 3).Value = "Amt (INR Cr)";
        sheet.Cell(5, 4).Value = "% Share";
        sheet.Range(5, 1, 5, 4).Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(headerBlue);
        sheet.Range(5, 1, 5, 4).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        WriteRow(sheet, 6, 1, "Export", data.ExportAmountCr, data.ExportShare);
        WriteRow(sheet, 7, 2, "Domestic", data.DomesticAmountCr, data.DomesticShare);

        sheet.Cell(8, 1).Value = "";
        sheet.Cell(8, 2).Value = "Total";
        sheet.Cell(8, 3).Value = data.TotalAmountCr;
        sheet.Cell(8, 4).Value = data.TotalAmount > 0 ? 100d : 0d;
        sheet.Range(8, 1, 8, 4).Style.Font.SetBold().Fill.SetBackgroundColor(gold);
        sheet.Cell(8, 3).Style.NumberFormat.Format = "#,##0.00";
        sheet.Cell(8, 4).Style.NumberFormat.Format = "0.00\"%\"";
        sheet.Cell(8, 3).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
        sheet.Cell(8, 4).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

        sheet.Cell(10, 1).Value = data.Note;
        sheet.Range(10, 1, 10, 4).Merge().Style.Font.SetItalic().Font.SetFontColor(XLColor.FromHtml("#64748B"));
        sheet.Columns().AdjustToContents();
        sheet.Column(2).Width = 28;
        sheet.Column(3).Width = 16;
        sheet.Column(4).Width = 14;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> BuildPdfAsync(string company, IReadOnlyList<string> months, bool refresh = false)
    {
        var data = await GetSalesProfileAsync(company, months, refresh);
        return new BankRequirementsPdfDocument(data).GeneratePdf();
    }

    private static void WriteRow(
        IXLWorksheet sheet,
        int excelRow,
        int sr,
        string stream,
        double amountMn,
        double share)
    {
        sheet.Cell(excelRow, 1).Value = sr;
        sheet.Cell(excelRow, 2).Value = stream;
        sheet.Cell(excelRow, 3).Value = amountMn;
        sheet.Cell(excelRow, 4).Value = share;
        sheet.Cell(excelRow, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        sheet.Cell(excelRow, 3).Style.NumberFormat.Format = "#,##0.00";
        sheet.Cell(excelRow, 4).Style.NumberFormat.Format = "0.00\"%\"";
        sheet.Cell(excelRow, 3).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
        sheet.Cell(excelRow, 4).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
    }

    private static List<(DateTime From, DateTime To)> ContiguousRanges(IReadOnlyList<string> months)
    {
        var ranges = new List<(DateTime From, DateTime To)>();
        DateTime? start = null;
        DateTime? prev = null;
        foreach (var key in months)
        {
            var month = FirstDay(key);
            if (start == null || prev == null || month != prev.Value.AddMonths(1))
            {
                if (start != null && prev != null)
                    ranges.Add((start.Value, CapToToday(prev.Value.AddMonths(1).AddDays(-1))));
                start = month;
            }
            prev = month;
        }
        if (start != null && prev != null)
            ranges.Add((start.Value, CapToToday(prev.Value.AddMonths(1).AddDays(-1))));
        return ranges;
    }

    private static DateTime CapToToday(DateTime value) =>
        value.Date > DateTime.Today ? DateTime.Today : value.Date;

    public static List<string> NormalizeMonths(IReadOnlyList<string>? months)
    {
        var parsed = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var raw in months ?? [])
        {
            foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (DateTime.TryParseExact(part, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    parsed.Add(dt.ToString("yyyy-MM"));
            }
        }
        return parsed.ToList();
    }

    public static List<string> DefaultFyMonths()
    {
        var today = DateTime.Today;
        var fyStart = today.Month >= 4 ? today.Year : today.Year - 1;
        var months = new List<string>(12);
        for (var i = 0; i < 12; i++)
        {
            var d = new DateTime(fyStart, 4, 1).AddMonths(i);
            if (d > today) break;
            months.Add(d.ToString("yyyy-MM"));
        }
        return months;
    }

    public static string FormatPeriodLabel(IReadOnlyList<string> months)
    {
        if (months.Count == 0) return "";
        var dates = months.Select(FirstDay).OrderBy(d => d).ToList();
        if (IsFullIndianFy(dates, out var fyStart) || IsFyYearToDate(dates, out fyStart))
            return $"{fyStart}-{(fyStart + 1) % 100:00}";

        if (dates.Count == 1)
            return dates[0].ToString("MMM yyyy", CultureInfo.GetCultureInfo("en-IN"));

        var contiguous = true;
        for (var i = 1; i < dates.Count; i++)
        {
            if (dates[i] != dates[i - 1].AddMonths(1))
            {
                contiguous = false;
                break;
            }
        }
        if (contiguous)
        {
            if (dates[0].Year == dates[^1].Year)
                return $"{dates[0]:MMM}–{dates[^1]:MMM yyyy}";
            return $"{dates[0]:MMM yyyy}–{dates[^1]:MMM yyyy}";
        }

        return string.Join(", ", dates.Select(d => d.ToString("MMM yyyy", CultureInfo.GetCultureInfo("en-IN"))));
    }

    private static bool IsFullIndianFy(List<DateTime> dates, out int fyStartYear)
    {
        fyStartYear = dates[0].Month >= 4 ? dates[0].Year : dates[0].Year - 1;
        if (dates.Count != 12) return false;
        for (var i = 0; i < 12; i++)
        {
            var expected = new DateTime(fyStartYear, 4, 1).AddMonths(i);
            if (dates[i].Year != expected.Year || dates[i].Month != expected.Month)
                return false;
        }
        return true;
    }

    private static bool IsFyYearToDate(List<DateTime> dates, out int fyStartYear)
    {
        fyStartYear = dates[0].Month >= 4 ? dates[0].Year : dates[0].Year - 1;
        if (dates.Count == 0 || dates[0].Month != 4 || dates[0].Year != fyStartYear)
            return false;
        var today = DateTime.Today;
        var currentFy = today.Month >= 4 ? today.Year : today.Year - 1;
        if (fyStartYear != currentFy)
            return false;
        var expectedCount = ((today.Year - fyStartYear) * 12) + (today.Month - 4) + 1;
        if (dates.Count != expectedCount || expectedCount is < 1 or >= 12)
            return false;
        for (var i = 0; i < dates.Count; i++)
        {
            var expected = new DateTime(fyStartYear, 4, 1).AddMonths(i);
            if (dates[i].Year != expected.Year || dates[i].Month != expected.Month)
                return false;
        }
        return true;
    }

    private static DateTime FirstDay(string yyyyMm) =>
        DateTime.ParseExact(yyyyMm + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static DateTime LastDay(string yyyyMm) => FirstDay(yyyyMm).AddMonths(1).AddDays(-1);

    private static double RoundCr(double amount) => Math.Round(amount / Crore, 2);

    private static double Share(double part, double total) =>
        total <= 0 ? 0 : Math.Round(part / total * 100, 2);
}
