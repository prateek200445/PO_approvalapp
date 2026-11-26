using ClosedXML.Excel;
using POApprovalAPI.Services;

namespace POApprovalAPI.Documents;

/// <summary>
/// Builds an .xlsx for Sales / Purchase month-on-month (same rows as monthly-trend API).
/// </summary>
public static class SalesMomExcelBuilder
{
    private static readonly XLColor Navy = XLColor.FromHtml("#0B3A5B");
    private static readonly XLColor HeaderBlue = XLColor.FromHtml("#1565A8");
    private static readonly XLColor Muted = XLColor.FromHtml("#64748B");
    private static readonly XLColor Zebra = XLColor.FromHtml("#F8FAFC");

    public static byte[] Build(
        SalesMonthlyMomDto data,
        string company,
        bool includeIntercompany)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Months is null || data.Months.Count == 0)
            throw new ArgumentException("Nothing to export.");

        var category = string.IsNullOrWhiteSpace(data.Category) ? "Sales" : data.Category.Trim();
        var amountLabel = string.Equals(category, "Purchase", StringComparison.OrdinalIgnoreCase)
            ? "Purchase (₹)"
            : "Sales (₹)";

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Month on Month");

        var lastCol = 6;
        var row = 1;
        sheet.Cell(row, 1).Value = $"{category} — Month on Month";
        sheet.Range(row, 1, row, lastCol).Merge()
            .Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(Navy);

        row = 2;
        var meta =
            $"Company: {company}    Period: {data.DateFrom} → {data.DateTo}    " +
            (includeIntercompany ? "Incl. intercompany" : "Excl. intercompany");
        sheet.Cell(row, 1).Value = meta;
        sheet.Range(row, 1, row, lastCol).Merge()
            .Style.Font.SetFontColor(HeaderBlue);

        row = 3;
        sheet.Cell(row, 1).Value =
            $"Source: {data.Source} · Amount matches Sales Dashboard KPI totals (EBIDTA Amount)";
        sheet.Range(row, 1, row, lastCol).Merge()
            .Style.Font.SetItalic().Font.SetFontColor(Muted);

        row = 5;
        sheet.Cell(row, 1).Value = "Month";
        sheet.Cell(row, 2).Value = amountLabel;
        sheet.Cell(row, 3).Value = "MoM %";
        sheet.Cell(row, 4).Value = "Quantity (Kg)";
        sheet.Cell(row, 5).Value = "Qty MoM %";
        sheet.Cell(row, 6).Value = "Avg rate (₹/Kg)";
        sheet.Range(row, 1, row, lastCol).Style
            .Font.SetBold().Font.SetFontColor(XLColor.White)
            .Fill.SetBackgroundColor(Navy)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        var dataStart = row + 1;
        for (var i = 0; i < data.Months.Count; i++)
        {
            var m = data.Months[i];
            var excelRow = dataStart + i;
            sheet.Cell(excelRow, 1).Value = m.Period;
            sheet.Cell(excelRow, 2).Value = m.Amount;
            sheet.Cell(excelRow, 2).Style.NumberFormat.Format = "#,##0.00";
            sheet.Cell(excelRow, 3).Value = m.AmountChangePercent / 100.0;
            sheet.Cell(excelRow, 3).Style.NumberFormat.Format = "0.0%";
            sheet.Cell(excelRow, 4).Value = m.Quantity;
            sheet.Cell(excelRow, 4).Style.NumberFormat.Format = "#,##0.00";
            sheet.Cell(excelRow, 5).Value = m.QuantityChangePercent / 100.0;
            sheet.Cell(excelRow, 5).Style.NumberFormat.Format = "0.0%";
            sheet.Cell(excelRow, 6).Value = m.AverageRate;
            sheet.Cell(excelRow, 6).Style.NumberFormat.Format = "#,##0.00";
            for (var c = 2; c <= lastCol; c++)
                sheet.Cell(excelRow, c).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            if (i % 2 == 1)
                sheet.Range(excelRow, 1, excelRow, lastCol).Style.Fill.SetBackgroundColor(Zebra);
        }

        var totalRow = dataStart + data.Months.Count;
        sheet.Cell(totalRow, 1).Value = $"Period total ({data.Months.Count} months)";
        sheet.Cell(totalRow, 2).Value = data.TotalAmount;
        sheet.Cell(totalRow, 2).Style.NumberFormat.Format = "#,##0.00";
        sheet.Cell(totalRow, 4).Value = data.TotalQuantity;
        sheet.Cell(totalRow, 4).Style.NumberFormat.Format = "#,##0.00";
        sheet.Cell(totalRow, 6).Value = data.AverageRate;
        sheet.Cell(totalRow, 6).Style.NumberFormat.Format = "#,##0.00";
        sheet.Range(totalRow, 1, totalRow, lastCol).Style
            .Font.SetBold().Font.SetFontColor(XLColor.White)
            .Fill.SetBackgroundColor(Navy);
        for (var c = 2; c <= lastCol; c++)
            sheet.Cell(totalRow, c).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

        sheet.Columns().AdjustToContents();
        sheet.Column(1).Width = Math.Max(sheet.Column(1).Width, 12);
        sheet.Column(2).Width = Math.Max(sheet.Column(2).Width, 16);
        sheet.Column(4).Width = Math.Max(sheet.Column(4).Width, 14);
        sheet.Column(6).Width = Math.Max(sheet.Column(6).Width, 14);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
