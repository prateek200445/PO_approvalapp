using ClosedXML.Excel;
using POApprovalAPI.Services;

namespace POApprovalAPI.Documents;

/// <summary>
/// Excel for ERP Next Week Pending Payment (<c>ERP.PendingPayment_n</c>).
/// </summary>
public static class PendingPaymentExcelBuilder
{
    private static readonly XLColor Navy = XLColor.FromHtml("#0B3A5B");
    private static readonly XLColor HeaderBlue = XLColor.FromHtml("#1565A8");
    private static readonly XLColor Muted = XLColor.FromHtml("#64748B");
    private static readonly XLColor Zebra = XLColor.FromHtml("#F8FAFC");

    public static byte[] Build(PendingPaymentResultDto data, string company)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Items is null || data.Items.Count == 0)
            throw new ArgumentException("Nothing to export.");

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Next Week");
        const int lastCol = 11;

        var row = 1;
        sheet.Cell(row, 1).Value = "Next Week Pending Payment";
        sheet.Range(row, 1, row, lastCol).Merge()
            .Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(Navy);

        row = 2;
        sheet.Cell(row, 1).Value =
            $"Company: {company}    Window: {data.DateFrom} → {data.DateTo}    Rows: {data.Count}";
        sheet.Range(row, 1, row, lastCol).Merge()
            .Style.Font.SetFontColor(HeaderBlue);

        row = 3;
        sheet.Cell(row, 1).Value = $"Source: {data.Source} · MM → Display → Other";
        sheet.Range(row, 1, row, lastCol).Merge()
            .Style.Font.SetItalic().Font.SetFontColor(Muted);

        row = 5;
        string[] headers =
        {
            "CompanyName", "PartyName", "BillNo", "BillDate", "MRNo", "MRNDt",
            "Year", "BillAmount", "PaymentTerms", "DueDt", "Department",
        };
        for (var c = 0; c < headers.Length; c++)
            sheet.Cell(row, c + 1).Value = headers[c];
        sheet.Range(row, 1, row, lastCol).Style
            .Font.SetBold().Font.SetFontColor(XLColor.White)
            .Fill.SetBackgroundColor(Navy)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        var dataStart = row + 1;
        for (var i = 0; i < data.Items.Count; i++)
        {
            var item = data.Items[i];
            var excelRow = dataStart + i;
            sheet.Cell(excelRow, 1).Value = item.CompanyName;
            sheet.Cell(excelRow, 2).Value = item.PartyName;
            sheet.Cell(excelRow, 3).Value = item.BillNo;
            if (item.BillDate.HasValue) sheet.Cell(excelRow, 4).Value = item.BillDate.Value;
            sheet.Cell(excelRow, 5).Value = item.MRNo;
            if (item.MRNDt.HasValue) sheet.Cell(excelRow, 6).Value = item.MRNDt.Value;
            sheet.Cell(excelRow, 7).Value = item.Year;
            sheet.Cell(excelRow, 8).Value = item.BillAmount;
            sheet.Cell(excelRow, 9).Value = item.PaymentTerms;
            if (item.DueDt.HasValue) sheet.Cell(excelRow, 10).Value = item.DueDt.Value;
            sheet.Cell(excelRow, 11).Value = item.Deptt;

            sheet.Cell(excelRow, 4).Style.DateFormat.Format = "yyyy-mm-dd";
            sheet.Cell(excelRow, 6).Style.DateFormat.Format = "yyyy-mm-dd";
            sheet.Cell(excelRow, 10).Style.DateFormat.Format = "yyyy-mm-dd";
            sheet.Cell(excelRow, 8).Style.NumberFormat.Format = "#,##0.00";
            if (i % 2 == 1)
                sheet.Range(excelRow, 1, excelRow, lastCol).Style.Fill.SetBackgroundColor(Zebra);
        }

        var totalRow = dataStart + data.Items.Count;
        sheet.Cell(totalRow, 1).Value = $"Total ({data.Count} rows)";
        sheet.Cell(totalRow, 8).Value = data.TotalBillAmount;
        sheet.Range(totalRow, 1, totalRow, lastCol).Style
            .Font.SetBold().Font.SetFontColor(XLColor.White)
            .Fill.SetBackgroundColor(Navy);
        sheet.Cell(totalRow, 8).Style.NumberFormat.Format = "#,##0.00";

        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
