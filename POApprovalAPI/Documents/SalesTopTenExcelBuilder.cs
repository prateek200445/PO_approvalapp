using ClosedXML.Excel;

namespace POApprovalAPI.Documents;

/// <summary>
/// Builds an .xlsx for sales/purchase ranking exports (same payload shape as the PDF).
/// One worksheet per section; includes a period total / summary row.
/// </summary>
public static class SalesTopTenExcelBuilder
{
    private static readonly XLColor Navy = XLColor.FromHtml("#0B3A5B");
    private static readonly XLColor HeaderBlue = XLColor.FromHtml("#1565A8");
    private static readonly XLColor Muted = XLColor.FromHtml("#64748B");
    private static readonly XLColor Zebra = XLColor.FromHtml("#F8FAFC");

    public static byte[] Build(SalesTopTenPdfRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Sections is null || request.Sections.Count == 0)
            throw new ArgumentException("Nothing to export.");

        using var workbook = new XLWorkbook();
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var section in request.Sections)
        {
            if (section?.Rows is null || section.Rows.Count == 0)
                continue;

            var sheetName = UniqueSheetName(usedNames, section.Title);
            var sheet = workbook.Worksheets.Add(sheetName);
            WriteSection(sheet, request, section);
        }

        if (workbook.Worksheets.Count == 0)
            throw new ArgumentException("Nothing to export.");

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteSection(IXLWorksheet sheet, SalesTopTenPdfRequest request, SalesTopTenPdfSection section)
    {
        var hasCompany = section.Rows.Any(r => !string.IsNullOrWhiteSpace(r.CompanyName));
        var hasCountry = section.Rows.Any(r => !string.IsNullOrWhiteSpace(r.Country));

        // Columns: # | [Group company] | Name | [Country] | Amount
        var colRank = 1;
        var colCompany = hasCompany ? 2 : -1;
        var colName = hasCompany ? 3 : 2;
        var colCountry = hasCountry ? colName + 1 : -1;
        var colAmount = (hasCountry ? colCountry : colName) + 1;
        var lastCol = colAmount;

        var row = 1;
        sheet.Cell(row, 1).Value = string.IsNullOrWhiteSpace(request.ReportTitle)
            ? section.Title
            : request.ReportTitle!.Trim();
        sheet.Range(row, 1, row, lastCol).Merge()
            .Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(Navy);

        row = 2;
        var meta = $"Company: {request.Company}    Period: {request.DateFrom} → {request.DateTo}";
        if (request.IncludeIntercompany)
            meta += "    Incl. intercompany";
        else
            meta += "    Excl. intercompany";
        sheet.Cell(row, 1).Value = meta;
        sheet.Range(row, 1, row, lastCol).Merge()
            .Style.Font.SetFontColor(HeaderBlue);

        row = 3;
        if (!string.IsNullOrWhiteSpace(section.Subtitle))
        {
            sheet.Cell(row, 1).Value = section.Subtitle;
            sheet.Range(row, 1, row, lastCol).Merge()
                .Style.Font.SetItalic().Font.SetFontColor(Muted);
            row++;
        }

        var headerRow = row;
        sheet.Cell(headerRow, colRank).Value = "#";
        if (hasCompany)
            sheet.Cell(headerRow, colCompany).Value = "Group company";
        sheet.Cell(headerRow, colName).Value =
            string.IsNullOrWhiteSpace(section.NameHeader) ? "Name" : section.NameHeader;
        if (hasCountry)
            sheet.Cell(headerRow, colCountry).Value = "Country";
        sheet.Cell(headerRow, colAmount).Value = "Amount (₹)";
        sheet.Range(headerRow, 1, headerRow, lastCol).Style
            .Font.SetBold().Font.SetFontColor(XLColor.White)
            .Fill.SetBackgroundColor(Navy)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        var dataStart = headerRow + 1;
        for (var i = 0; i < section.Rows.Count; i++)
        {
            var r = section.Rows[i];
            var excelRow = dataStart + i;
            sheet.Cell(excelRow, colRank).Value = r.Rank > 0 ? r.Rank : i + 1;
            sheet.Cell(excelRow, colRank).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            if (hasCompany)
                sheet.Cell(excelRow, colCompany).Value = r.CompanyName ?? "";
            sheet.Cell(excelRow, colName).Value = r.Name ?? "";
            if (hasCountry)
                sheet.Cell(excelRow, colCountry).Value = r.Country ?? "";
            sheet.Cell(excelRow, colAmount).Value = (double)r.Amount;
            sheet.Cell(excelRow, colAmount).Style.NumberFormat.Format = "#,##0.00";
            sheet.Cell(excelRow, colAmount).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            if (i % 2 == 1)
                sheet.Range(excelRow, 1, excelRow, lastCol).Style.Fill.SetBackgroundColor(Zebra);
        }

        var totalRow = dataStart + section.Rows.Count;
        var periodSum = section.Rows.Sum(r => r.Amount);
        var totalLabel = string.IsNullOrWhiteSpace(section.TotalLabel)
            ? $"Period total ({section.Rows.Count} rows)"
            : section.TotalLabel!;
        sheet.Cell(totalRow, 1).Value = totalLabel;
        sheet.Range(totalRow, 1, totalRow, colAmount - 1).Merge();
        sheet.Cell(totalRow, colAmount).Value = (double)periodSum;
        sheet.Cell(totalRow, colAmount).Style.NumberFormat.Format = "#,##0.00";
        sheet.Range(totalRow, 1, totalRow, lastCol).Style
            .Font.SetBold().Font.SetFontColor(XLColor.White)
            .Fill.SetBackgroundColor(Navy);
        sheet.Cell(totalRow, colAmount).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

        if (request.TotalAmount is { } bannerTotal)
        {
            var noteRow = totalRow + 2;
            var label = string.IsNullOrWhiteSpace(request.TotalAmountLabel)
                ? "Export sales total for the period"
                : request.TotalAmountLabel!;
            sheet.Cell(noteRow, 1).Value = $"{label}: {bannerTotal:N2}";
            sheet.Range(noteRow, 1, noteRow, lastCol).Merge()
                .Style.Font.SetItalic().Font.SetFontColor(Muted);
        }

        sheet.Columns().AdjustToContents();
        if (hasCompany)
            sheet.Column(colCompany).Width = Math.Max(sheet.Column(colCompany).Width, 18);
        sheet.Column(colName).Width = Math.Max(sheet.Column(colName).Width, 28);
        if (hasCountry)
            sheet.Column(colCountry).Width = Math.Max(sheet.Column(colCountry).Width, 14);
        sheet.Column(colAmount).Width = Math.Max(sheet.Column(colAmount).Width, 16);
    }

    private static string UniqueSheetName(HashSet<string> used, string? title)
    {
        var baseName = SanitizeSheetName(string.IsNullOrWhiteSpace(title) ? "Export" : title!);
        var name = baseName;
        var n = 2;
        while (!used.Add(name))
        {
            var suffix = $" ({n++})";
            var maxBase = Math.Max(1, 31 - suffix.Length);
            name = (baseName.Length > maxBase ? baseName[..maxBase] : baseName) + suffix;
        }
        return name;
    }

    private static string SanitizeSheetName(string title)
    {
        var cleaned = string.Concat(title.Select(c =>
            c is ':' or '\\' or '/' or '?' or '*' or '[' or ']' ? ' ' : c)).Trim();
        if (string.IsNullOrWhiteSpace(cleaned))
            cleaned = "Export";
        return cleaned.Length <= 31 ? cleaned : cleaned[..31];
    }
}
