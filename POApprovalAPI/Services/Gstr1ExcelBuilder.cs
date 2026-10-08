using System.Globalization;
using ClosedXML.Excel;

namespace POApprovalAPI.Services;

/// <summary>
/// GSTR-1 workbook in the GST offline tool Excel template layout: rows 1–3 hold the section summary,
/// row 4 the template headers and data starts on row 5. Columns the template does not have (tax amounts,
/// company, ERP references) are appended to the right under grey headers so a copy of the template
/// columns pastes straight into the offline tool.
/// </summary>
internal static class Gstr1ExcelBuilder
{
    private static readonly XLColor TitleFill = XLColor.FromHtml("#1F6FB2");
    private static readonly XLColor HeaderFill = XLColor.FromHtml("#F8CBAD");
    private static readonly XLColor ExtraFill = XLColor.FromHtml("#D9D9D9");
    private static readonly XLColor SummaryFill = XLColor.FromHtml("#DDEBF7");

    public static byte[] Build(Gstr1ReportDto r)
    {
        using var wb = new XLWorkbook();

        var b2bInvoices = r.B2b.GroupBy(x => (x.Company, x.ErpInvoiceNo)).ToList();
        Sheet(wb, "b2b,sez,de", "Summary For B2B, SEZ, DE (4A, 4B, 6B, 6C)",
            [
                ("No. of Recipients", r.B2b.Select(x => x.Gstin).Distinct().Count()),
                ("No. of Invoices", b2bInvoices.Count),
                ("Total Invoice Value", b2bInvoices.Sum(g => g.First().InvoiceValue)),
                ("Total Taxable Value", r.B2b.Sum(x => x.Taxable)),
                ("Total Cess", 0m),
                ("Total TCS", r.B2b.Sum(x => x.Tcs)),
                ("Total Other Charges", r.B2b.Sum(x => x.OtherCharges)),
            ],
            ["GSTIN/UIN of Recipient", "Receiver Name", "Invoice Number", "Invoice date", "Invoice Value", "Place Of Supply",
             "Reverse Charge", "Applicable % of Tax Rate", "Invoice Type", "E-Commerce GSTIN", "Rate", "Taxable Value", "Cess Amount"],
            ["Integrated Tax", "Central Tax", "State/UT Tax", "TCS", "Other Charges (Freight + Insurance)", "Company", "ERP Invoice No", "Approved"],
            r.B2b.Select(x => new object?[]
            {
                x.VoucherType, x.SalesLedger,
                x.Gstin, x.ReceiverName, x.InvoiceNo, Date(x.InvoiceDate), x.InvoiceValue, x.PlaceOfSupply, x.ReverseCharge, null,
                x.InvoiceType, null, x.Rate, x.Taxable, x.Cess,
                x.Igst, x.Cgst, x.Sgst, x.Tcs, x.OtherCharges, x.Company, x.ErpInvoiceNo, YesNo(x.Approved),
            }),
            ["Voucher Type", "Sales Ledger"]);

        var b2clInvoices = r.B2cl.GroupBy(x => (x.Company, x.ErpInvoiceNo)).ToList();
        Sheet(wb, "b2cl", "Summary For B2CL (5)",
            [
                ("No. of Invoices", b2clInvoices.Count),
                ("Total Invoice Value", b2clInvoices.Sum(g => g.First().InvoiceValue)),
                ("Total Taxable Value", r.B2cl.Sum(x => x.Taxable)),
                ("Total Cess", 0m),
            ],
            ["Invoice Number", "Invoice date", "Invoice Value", "Place Of Supply", "Applicable % of Tax Rate", "Rate",
             "Taxable Value", "Cess Amount", "E-Commerce GSTIN"],
            ["Integrated Tax", "Receiver Name", "Company", "ERP Invoice No", "ERP Voucher Type", "Approved"],
            r.B2cl.Select(x => new object?[]
            {
                x.InvoiceNo, Date(x.InvoiceDate), x.InvoiceValue, x.PlaceOfSupply, null, x.Rate, x.Taxable, x.Cess, null,
                x.Igst, x.ReceiverName, x.Company, x.ErpInvoiceNo, x.VoucherType, YesNo(x.Approved),
            }));

        Sheet(wb, "b2cs", "Summary For B2CS (7)",
            [
                ("Total Taxable Value", r.B2cs.Sum(x => x.Taxable)),
                ("Total Cess", 0m),
            ],
            ["Type", "Place Of Supply", "Applicable % of Tax Rate", "Rate", "Taxable Value", "Cess Amount", "E-Commerce GSTIN"],
            ["Integrated Tax", "Central Tax", "State/UT Tax", "Documents"],
            r.B2cs.Select(x => new object?[]
            {
                x.Type, x.PlaceOfSupply, null, x.Rate, x.Taxable, x.Cess, null, x.Igst, x.Cgst, x.Sgst, x.Documents,
            }));

        var cdnrNotes = r.Cdnr.GroupBy(x => (x.Company, x.ErpNoteNo)).ToList();
        Sheet(wb, "cdnr", "Summary For CDNR (9B)",
            [
                ("No. of Recipients", r.Cdnr.Select(x => x.Gstin).Distinct().Count()),
                ("No. of Notes", cdnrNotes.Count),
                ("Total Note Value", cdnrNotes.Sum(g => g.First().NoteValue)),
                ("Total Taxable Value", r.Cdnr.Sum(x => x.Taxable)),
                ("Total Cess", 0m),
            ],
            ["GSTIN/UIN of Recipient", "Receiver Name", "Note Number", "Note Date", "Note Type", "Place Of Supply",
             "Reverse Charge", "Note Supply Type", "Note Value", "Applicable % of Tax Rate", "Rate", "Taxable Value", "Cess Amount"],
            ["Integrated Tax", "Central Tax", "State/UT Tax", "Original Invoice No", "Original Invoice Date", "Company",
             "ERP Note No", "ERP Note Type", "Approved"],
            r.Cdnr.Select(x => new object?[]
            {
                x.Gstin, x.ReceiverName, x.NoteNo, Date(x.NoteDate), x.NoteType, x.PlaceOfSupply, x.ReverseCharge, x.SupplyType,
                x.NoteValue, null, x.Rate, x.Taxable, x.Cess,
                x.Igst, x.Cgst, x.Sgst, x.OriginalInvoiceNo, Date(x.OriginalInvoiceDate), x.Company, x.ErpNoteNo, x.ErpType, YesNo(x.Approved),
            }));

        var cdnurNotes = r.Cdnur.GroupBy(x => (x.Company, x.ErpNoteNo)).ToList();
        Sheet(wb, "cdnur", "Summary For CDNUR (9B)",
            [
                ("No. of Notes", cdnurNotes.Count),
                ("Total Note Value", cdnurNotes.Sum(g => g.First().NoteValue)),
                ("Total Taxable Value", r.Cdnur.Sum(x => x.Taxable)),
                ("Total Cess", 0m),
            ],
            ["UR Type", "Note Number", "Note Date", "Note Type", "Place Of Supply", "Note Value", "Applicable % of Tax Rate",
             "Rate", "Taxable Value", "Cess Amount"],
            ["Integrated Tax", "Receiver Name", "Original Invoice No", "Original Invoice Date", "Company", "ERP Note No", "ERP Note Type", "Approved"],
            r.Cdnur.Select(x => new object?[]
            {
                x.SupplyType, x.NoteNo, Date(x.NoteDate), x.NoteType, x.PlaceOfSupply, x.NoteValue, null, x.Rate, x.Taxable, x.Cess,
                x.Igst, x.ReceiverName, x.OriginalInvoiceNo, Date(x.OriginalInvoiceDate), x.Company, x.ErpNoteNo, x.ErpType, YesNo(x.Approved),
            }));

        var expInvoices = r.Exp.GroupBy(x => (x.Company, x.ErpInvoiceNo)).ToList();
        Sheet(wb, "exp", "Summary For EXP (6)",
            [
                ("No. of Invoices", expInvoices.Count),
                ("Total Invoice Value", expInvoices.Sum(g => g.First().InvoiceValue)),
                ("No. of Shipping Bill", expInvoices.Select(g => g.First().ShippingBillNo).Where(s => s.Length > 0).Distinct().Count()),
                ("Total Taxable Value", r.Exp.Sum(x => x.Taxable)),
                ("Total Cess", 0m),
            ],
            ["Export Type", "Invoice Number", "Invoice date", "Invoice Value", "Port Code", "Shipping Bill Number",
             "Shipping Bill Date", "Rate", "Taxable Value", "Cess Amount"],
            ["Integrated Tax", "Receiver Name", "Company", "ERP Invoice No", "ERP Voucher Type", "Approved",
             "FOB Value (ICEGATE)", "EGM No", "EGM Date", "ICEGATE Invoice No", "ICEGATE Invoice Date", "IGST Paid (ICEGATE)",
             "ICEGATE Match", "ICEGATE Differences", "Port/SB from ICEGATE"],
            r.Exp.Select(x => new object?[]
            {
                x.ExportType, x.InvoiceNo, Date(x.InvoiceDate), x.InvoiceValue, x.PortCode, x.ShippingBillNo,
                Date(x.ShippingBillDate), x.Rate, x.Taxable, x.Cess,
                x.Igst, x.ReceiverName, x.Company, x.ErpInvoiceNo, x.VoucherType, YesNo(x.Approved),
                x.Fob, x.EgmNo, x.EgmDate, x.IcegateInvoiceNo, x.IcegateInvoiceDate, x.IcegateIgst,
                x.IcegateStatus, x.IcegateNote, x.FilledFromIcegate ? "Y" : "",
            }));

        if (r.Icegate is { NotInErp.Count: > 0 } ice)
            Sheet(wb, "icegate-not-in-erp", "ICEGATE shipping bills with no ERP export invoice in this period",
                [("Shipping bills", ice.NotInErp.Count), ("Total FOB", ice.NotInErp.Sum(x => x.Fob ?? 0))],
                ["Company", "ICEGATE Company", "Port Code", "Shipping Bill No", "Shipping Bill Date", "Invoice No", "Invoice Date",
                 "FOB Value", "IGST Paid", "EGM No", "EGM Date"],
                [],
                ice.NotInErp.Select(x => new object?[]
                {
                    x.Company, x.CompanyLabel, x.PortCode, x.ShippingBillNo, x.ShippingBillDate, x.InvoiceNo, x.InvoiceDate,
                    x.Fob, x.IgstPaid, x.EgmNo, x.EgmDate,
                }));

        Sheet(wb, "exemp", "Summary For Nil rated, exempted and non GST outward supplies (8)",
            [
                ("Total Nil Rated Supplies", r.Nil.Sum(x => x.NilRated)),
                ("Total Exempted Supplies", r.Nil.Sum(x => x.Exempted)),
                ("Total Non-GST Supplies", r.Nil.Sum(x => x.NonGst)),
            ],
            ["Description", "Nil Rated Supplies", "Exempted(other than nil rated/non GST supply)", "Non-GST Supplies"],
            [],
            r.Nil.Select(x => new object?[] { x.Description, x.NilRated, x.Exempted, x.NonGst }));

        HsnSheet(wb, "hsn(b2b)", "Summary For HSN(12) — B2B", r.HsnB2b);
        HsnSheet(wb, "hsn(b2c)", "Summary For HSN(12) — B2C", r.HsnB2c);
        HsnSummarySheet(wb, r.HsnSummary ?? []);
        SalesRegisterSheet(wb, r.SalesRegister ?? []);
        TrialBalanceSheet(wb, r);
        LedgerReconSheet(wb, r);

        Sheet(wb, "docs", "Summary of documents issued during the tax period (13)",
            [
                ("Total Number", r.Docs.Sum(x => x.TotalNumber)),
                ("Total Cancelled", r.Docs.Sum(x => x.Cancelled)),
            ],
            ["Nature of Document", "Sr. No. From", "Sr. No. To", "Total Number", "Cancelled"],
            ["Company", "Series", "Net Issued"],
            r.Docs.Select(x => new object?[]
            {
                x.Nature, x.FromNo, x.ToNo, x.TotalNumber, x.Cancelled, x.Company, x.Series, x.NetIssued,
            }));

        SummarySheet(wb, r);
        ExceptionsSheet(wb, r);

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }

    private static void HsnSheet(XLWorkbook wb, string name, string title, IReadOnlyList<Gstr1HsnRowDto> rows) =>
        Sheet(wb, name, title,
            [
                ("No. of HSN", rows.Select(x => x.Hsn).Distinct().Count()),
                ("Total Value", rows.Sum(x => x.TotalValue)),
                ("Total Taxable Value", rows.Sum(x => x.Taxable)),
                ("Total Integrated Tax", rows.Sum(x => x.Igst)),
                ("Total Central Tax", rows.Sum(x => x.Cgst)),
                ("Total State/UT Tax", rows.Sum(x => x.Sgst)),
                ("Total Cess", 0m),
            ],
            ["HSN", "Description", "UQC", "Total Quantity", "Total Value", "Rate", "Taxable Value",
             "Integrated Tax Amount", "Central Tax Amount", "State/UT Tax Amount", "Cess Amount"],
            [],
            rows.Select(x => new object?[]
            {
                x.Hsn, x.Description, x.Uqc, x.Quantity, x.TotalValue, x.Rate, x.Taxable, x.Igst, x.Cgst, x.Sgst, x.Cess,
            }));

    /// <summary>B2B + B2C combined, with the ERP commodity names behind each HSN.</summary>
    private static void HsnSummarySheet(XLWorkbook wb, IReadOnlyList<Gstr1HsnRowDto> rows)
    {
        const string name = "hsn summary";
        Sheet(wb, name, "HSN-wise Summary (B2B + B2C) — Description limited to 30 characters",
            [
                ("No. of HSN", rows.Select(x => x.Hsn).Distinct().Count()),
                ("Total Value", rows.Sum(x => x.TotalValue)),
                ("Total Taxable Value", rows.Sum(x => x.Taxable)),
                ("Total Integrated Tax", rows.Sum(x => x.Igst)),
                ("Total Central Tax", rows.Sum(x => x.Cgst)),
                ("Total State/UT Tax", rows.Sum(x => x.Sgst)),
                ("Total Cess", rows.Sum(x => x.Cess)),
            ],
            ["HSN", "Commodity", "Description", "UQC", "Total Quantity", "Total Value", "Rate", "Taxable Value",
             "Integrated Tax Amount", "Central Tax Amount", "State/UT Tax Amount", "Cess Amount"],
            [],
            rows.Select(x => new object?[]
            {
                x.Hsn, x.Commodity, x.Description, x.Uqc, x.Quantity, x.TotalValue, x.Rate, x.Taxable, x.Igst, x.Cgst, x.Sgst, x.Cess,
            }));
        if (rows.Count == 0) return;

        var ws = wb.Worksheet(name);
        var total = 5 + rows.Count;
        ws.Cell(total, 1).Value = "Total";
        SetValue(ws.Cell(total, 6), rows.Sum(x => x.TotalValue));
        SetValue(ws.Cell(total, 8), rows.Sum(x => x.Taxable));
        SetValue(ws.Cell(total, 9), rows.Sum(x => x.Igst));
        SetValue(ws.Cell(total, 10), rows.Sum(x => x.Cgst));
        SetValue(ws.Cell(total, 11), rows.Sum(x => x.Sgst));
        SetValue(ws.Cell(total, 12), rows.Sum(x => x.Cess));
        ws.Range(total, 1, total, 12).Style.Font.SetBold().Fill.SetBackgroundColor(SummaryFill)
            .Border.SetTopBorder(XLBorderStyleValues.Thin);
    }

    private static void Sheet(XLWorkbook wb, string name, string title, (string Label, object Value)[] summary,
        string[] headers, string[] extraHeaders, IEnumerable<object?[]> rows, string[]? leadingHeaders = null)
    {
        var leading = leadingHeaders ?? [];
        var ws = wb.Worksheets.Add(name);
        var width = Math.Max(leading.Length + headers.Length + extraHeaders.Length, summary.Length);

        ws.Cell(1, 1).Value = title;
        ws.Range(1, 1, 1, Math.Max(width, 1)).Style.Fill.SetBackgroundColor(TitleFill)
            .Font.SetFontColor(XLColor.White).Font.SetBold();
        for (var i = 0; i < summary.Length; i++)
        {
            ws.Cell(2, i + 1).Value = summary[i].Label;
            SetValue(ws.Cell(3, i + 1), summary[i].Value);
        }
        ws.Range(2, 1, 3, Math.Max(summary.Length, 1)).Style.Fill.SetBackgroundColor(SummaryFill);
        ws.Range(2, 1, 2, Math.Max(summary.Length, 1)).Style.Font.SetBold();

        if (leading.Length > 0)
        {
            for (var i = 0; i < leading.Length; i++)
                ws.Cell(4, i + 1).Value = leading[i];
            ws.Range(4, 1, 4, leading.Length).Style.Fill.SetBackgroundColor(ExtraFill).Font.SetBold().Font.SetItalic();
        }
        var offset = leading.Length;
        for (var i = 0; i < headers.Length; i++)
            ws.Cell(4, offset + i + 1).Value = headers[i];
        ws.Range(4, offset + 1, 4, offset + headers.Length).Style.Fill.SetBackgroundColor(HeaderFill).Font.SetBold();
        if (extraHeaders.Length > 0)
        {
            for (var i = 0; i < extraHeaders.Length; i++)
                ws.Cell(4, offset + headers.Length + i + 1).Value = extraHeaders[i];
            ws.Range(4, offset + headers.Length + 1, 4, offset + headers.Length + extraHeaders.Length).Style
                .Fill.SetBackgroundColor(ExtraFill).Font.SetBold().Font.SetItalic();
        }

        var r = 5;
        foreach (var row in rows)
        {
            for (var c = 0; c < row.Length; c++)
                SetValue(ws.Cell(r, c + 1), row[c]);
            r++;
        }

        ws.SheetView.FreezeRows(4);
        if (r > 5)
            ws.Range(4, 1, r - 1, leading.Length + headers.Length + extraHeaders.Length).SetAutoFilter();
        ws.Columns(1, Math.Max(width, 1)).AdjustToContents(4, Math.Min(r, 400));
        foreach (var col in ws.ColumnsUsed())
            if (col.Width > 48) col.Width = 48;
    }

    private static void SummarySheet(XLWorkbook wb, Gstr1ReportDto r)
    {
        var ws = wb.Worksheets.Add("Summary");
        ws.Cell(1, 1).Value = $"GSTR-1 — {r.ScopeLabel}";
        ws.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(14);
        ws.Cell(2, 1).Value = $"Period {r.From:dd-MMM-yyyy} to {r.To:dd-MMM-yyyy}"
                              + (r.Gstins.Count > 0 ? $" · GSTIN {string.Join(", ", r.Gstins)}" : "")
                              + $" · Unapproved vouchers {(r.IncludeUnapproved ? "included" : "excluded")}";
        ws.Cell(3, 1).Value = $"Generated {r.GeneratedAtUtc.ToLocalTime():dd-MMM-yyyy HH:mm}";

        string[] head = ["Section", "Documents", "Invoice / Note Value", "Taxable Value", "Integrated Tax", "Central Tax", "State/UT Tax", "Cess"];
        var row = 5;
        for (var i = 0; i < head.Length; i++) ws.Cell(row, i + 1).Value = head[i];
        ws.Range(row, 1, row, head.Length).Style.Fill.SetBackgroundColor(HeaderFill).Font.SetBold();
        row++;
        foreach (var s in r.Summary)
        {
            ws.Cell(row, 1).Value = s.Label;
            SetValue(ws.Cell(row, 2), s.Documents);
            SetValue(ws.Cell(row, 3), s.InvoiceValue);
            SetValue(ws.Cell(row, 4), s.Taxable);
            SetValue(ws.Cell(row, 5), s.Igst);
            SetValue(ws.Cell(row, 6), s.Cgst);
            SetValue(ws.Cell(row, 7), s.Sgst);
            SetValue(ws.Cell(row, 8), s.Cess);
            if (s.Section == "NET") ws.Range(row, 1, row, head.Length).Style.Font.SetBold().Fill.SetBackgroundColor(SummaryFill);
            row++;
        }

        row += 2;
        ws.Cell(row, 1).Value = "Reconciliation with ERP sales lines (invoices)";
        ws.Cell(row, 1).Style.Font.SetBold();
        row++;
        string[] rhead = ["", "Taxable Value", "Integrated Tax", "Central Tax", "State/UT Tax"];
        for (var i = 0; i < rhead.Length; i++) ws.Cell(row, i + 1).Value = rhead[i];
        ws.Range(row, 1, row, rhead.Length).Style.Fill.SetBackgroundColor(HeaderFill).Font.SetBold();
        row++;
        foreach (var x in r.Reconciliation)
        {
            ws.Cell(row, 1).Value = x.Label;
            SetValue(ws.Cell(row, 2), x.Taxable);
            SetValue(ws.Cell(row, 3), x.Igst);
            SetValue(ws.Cell(row, 4), x.Cgst);
            SetValue(ws.Cell(row, 5), x.Sgst);
            if (x.Label is "Expected in return" or "Difference") ws.Range(row, 1, row, rhead.Length).Style.Font.SetBold();
            row++;
        }

        ws.Column(1).Width = 62;
        ws.Columns(2, 8).Width = 18;
    }

    private static void ExceptionsSheet(XLWorkbook wb, Gstr1ReportDto r)
    {
        var ws = wb.Worksheets.Add("Exceptions");
        string[] head = ["Severity", "Check", "Company", "Document No", "Date", "Party", "Detail", "Amount"];
        ws.Cell(1, 1).Value = $"Exceptions — {r.Exceptions.Count}";
        ws.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(13);
        for (var i = 0; i < head.Length; i++) ws.Cell(3, i + 1).Value = head[i];
        ws.Range(3, 1, 3, head.Length).Style.Fill.SetBackgroundColor(HeaderFill).Font.SetBold();
        var row = 4;
        foreach (var e in r.Exceptions)
        {
            ws.Cell(row, 1).Value = e.Severity;
            ws.Cell(row, 2).Value = e.Title;
            ws.Cell(row, 3).Value = e.Company;
            ws.Cell(row, 4).Value = e.DocumentNo;
            ws.Cell(row, 5).Value = Date(e.DocumentDate) ?? "";
            ws.Cell(row, 6).Value = e.Party;
            ws.Cell(row, 7).Value = e.Detail;
            SetValue(ws.Cell(row, 8), e.Amount);
            if (e.Severity == "error") ws.Cell(row, 1).Style.Font.SetFontColor(XLColor.FromHtml("#B91C1C")).Font.SetBold();
            else if (e.Severity == "warning") ws.Cell(row, 1).Style.Font.SetFontColor(XLColor.FromHtml("#B45309"));
            row++;
        }
        ws.SheetView.FreezeRows(3);
        if (row > 4) ws.Range(3, 1, row - 1, head.Length).SetAutoFilter();
        ws.Column(1).Width = 10;
        ws.Column(2).Width = 34;
        ws.Column(3).Width = 34;
        ws.Column(4).Width = 20;
        ws.Column(5).Width = 13;
        ws.Column(6).Width = 34;
        ws.Column(7).Width = 90;
        ws.Column(8).Width = 15;
        ws.Column(7).Style.Alignment.SetWrapText();
    }

    private const string IndianNumber = "#,##,##0.00";
    private static readonly XLColor DiffFill = XLColor.FromHtml("#FFF2A8");

    private static void TotalRow(IXLWorksheet ws, int row, int lastCol, string label, params (int Col, decimal Value)[] values)
    {
        ws.Cell(row, 1).Value = label;
        foreach (var (col, value) in values) SetValue(ws.Cell(row, col), value);
        ws.Range(row, 1, row, lastCol).Style.Font.SetBold().Fill.SetBackgroundColor(SummaryFill)
            .Border.SetTopBorder(XLBorderStyleValues.Thin);
    }

    private static void SalesRegisterSheet(XLWorkbook wb, IReadOnlyList<Gstr1SalesRegisterRowDto> rows)
    {
        const string name = "sales register";
        var invoices = rows.Select(x => (x.Company, x.ErpInvoiceNo, x.InvoiceDate)).Distinct().Count();
        Sheet(wb, name, "Sales Register — invoice lines (Gross Amount, TCS and Other Charges on the first line of each invoice)",
            [
                ("No. of Invoices", invoices),
                ("Sum of GrossAmount", rows.Sum(x => x.GrossAmount)),
                ("Sum of Value", rows.Sum(x => x.Value)),
                ("IGST", rows.Sum(x => x.Igst)),
                ("CGST", rows.Sum(x => x.Cgst)),
                ("SGST", rows.Sum(x => x.Sgst)),
                ("TCS", rows.Sum(x => x.Tcs)),
                ("Other Charges", rows.Sum(x => x.OtherCharges)),
            ],
            ["Sales Ledger", "Voucher Type", "Invoice No", "Date", "Party", "GSTIN", "POS", "HSN", "Commodity", "Qty", "UQC", "Rate",
             "Value", "IGST", "CGST", "SGST", "TCS", "Other Charges", "GrossAmount"],
            ["GSTR-1 Status", "Company", "ERP Invoice No"],
            rows.Select(x => new object?[]
            {
                x.SalesLedger, x.VoucherType, x.InvoiceNo, x.InvoiceDate, x.Party, x.Gstin, x.PlaceOfSupply, x.Hsn, x.Commodity,
                x.Quantity, x.Uqc, x.Rate, x.Value, x.Igst, x.Cgst, x.Sgst, x.Tcs, x.OtherCharges, x.GrossAmount,
                x.Gstr1Status, x.Company, x.ErpInvoiceNo,
            }));
        if (rows.Count == 0) return;
        var ws = wb.Worksheet(name);
        TotalRow(ws, 5 + rows.Count, 22, "Total",
            (13, rows.Sum(x => x.Value)), (14, rows.Sum(x => x.Igst)), (15, rows.Sum(x => x.Cgst)), (16, rows.Sum(x => x.Sgst)),
            (17, rows.Sum(x => x.Tcs)), (18, rows.Sum(x => x.OtherCharges)), (19, rows.Sum(x => x.GrossAmount)));
        ws.Range(5, 13, 5 + rows.Count, 19).Style.NumberFormat.Format = IndianNumber;
    }

    private static readonly XLColor GroupFill = XLColor.FromHtml("#EEF3FA");

    /// <summary>Full trial balance grouped by primary head › group, with group subtotals and grand totals.</summary>
    private static void TrialBalanceSheet(XLWorkbook wb, Gstr1ReportDto r)
    {
        var rows = r.TrialBalance ?? [];
        var ws = wb.Worksheets.Add("trial balance");
        ws.Cell(1, 1).Value = $"Trial Balance — {r.ScopeLabel}, {r.From:dd-MMM-yyyy} to {r.To:dd-MMM-yyyy}";
        ws.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(13);
        var od = rows.Sum(x => x.OpeningDebit);
        var oc = rows.Sum(x => x.OpeningCredit);
        var cd = rows.Sum(x => x.ClosingDebit);
        var cc = rows.Sum(x => x.ClosingCredit);
        ws.Cell(2, 1).Value = $"{rows.Count(x => !x.IsAdjustment)} ledgers · source: ERP ledger postings (vw_LedgerSummary) · " +
                              "P&L groups open at 1 April of the financial year";

        string[] head = ["Group", "Sub-group (Under)", "Ledger", "Company", "Opening Dr", "Opening Cr", "Debit", "Credit", "Closing Dr", "Closing Cr"];
        for (var i = 0; i < head.Length; i++) ws.Cell(4, i + 1).Value = head[i];
        ws.Range(4, 1, 4, head.Length).Style.Font.SetBold().Fill.SetBackgroundColor(SummaryFill);

        void Amounts(int row, decimal a, decimal b, decimal c, decimal d, decimal e, decimal f)
        {
            var values = new[] { a, b, c, d, e, f };
            for (var i = 0; i < values.Length; i++)
                if (values[i] != 0) SetValue(ws.Cell(row, 5 + i), values[i]);
        }

        var row = 5;
        foreach (var g in rows.GroupBy(x => (x.Primary, x.Group)))
        {
            ws.Cell(row, 1).Value = $"{g.Key.Primary} › {g.Key.Group}";
            ws.Range(row, 1, row, head.Length).Style.Font.SetBold().Fill.SetBackgroundColor(GroupFill);
            row++;
            foreach (var x in g)
            {
                ws.Cell(row, 1).Value = x.Group;
                ws.Cell(row, 2).Value = x.Under;
                ws.Cell(row, 3).Value = x.Ledger;
                ws.Cell(row, 4).Value = x.Company;
                Amounts(row, x.OpeningDebit, x.OpeningCredit, x.Debit, x.Credit, x.ClosingDebit, x.ClosingCredit);
                if (x.IsAdjustment) ws.Range(row, 1, row, head.Length).Style.Fill.SetBackgroundColor(DiffFill);
                row++;
            }
            ws.Cell(row, 3).Value = $"Total {g.Key.Group}";
            Amounts(row, g.Sum(x => x.OpeningDebit), g.Sum(x => x.OpeningCredit), g.Sum(x => x.Debit), g.Sum(x => x.Credit),
                g.Sum(x => x.ClosingDebit), g.Sum(x => x.ClosingCredit));
            ws.Range(row, 1, row, head.Length).Style.Font.SetBold().Font.SetItalic()
                .Border.SetTopBorder(XLBorderStyleValues.Hair);
            row++;
        }
        TotalRow(ws, row, head.Length, "Grand Total", (5, od), (6, oc), (7, rows.Sum(x => x.Debit)), (8, rows.Sum(x => x.Credit)), (9, cd), (10, cc));
        ws.Range(5, 5, row, 10).Style.NumberFormat.Format = IndianNumber;
        ws.Cell(row + 2, 1).Value = rows.Any(x => x.IsAdjustment)
            ? "Highlighted rows are not ERP ledgers: previous years' P&L carried forward, and any difference in the ERP postings so that Dr = Cr."
            : "Opening, period and closing totals balance (Dr = Cr).";

        ws.SheetView.FreezeRows(4);
        if (row > 5) ws.Range(4, 1, row - 1, head.Length).SetAutoFilter();
        ws.Column(1).Width = 30;
        ws.Column(2).Width = 30;
        ws.Column(3).Width = 46;
        ws.Column(4).Width = 30;
        for (var c = 5; c <= 10; c++) ws.Column(c).Width = 18;
    }

    /// <summary>
    /// Two pivots side by side like the tax team's sheet — "Sales Reg." (A–C) and "HSN" (F–H) by sales ledger —
    /// then a differences table with the trial balance and the causes.
    /// </summary>
    private static void LedgerReconSheet(XLWorkbook wb, Gstr1ReportDto r)
    {
        var all = r.LedgerRecon ?? [];
        var rows = all.Where(x => x.RegisterGross != 0 || x.RegisterValue != 0 || x.HsnInvoiceValue != 0 || x.HsnTaxable != 0).ToList();
        var ws = wb.Worksheets.Add("salesreg vs hsn");

        int Pivot(int col, string title, string h1, string h2, Func<Gstr1LedgerReconRowDto, decimal> v1, Func<Gstr1LedgerReconRowDto, decimal> v2)
        {
            ws.Cell(1, col).Value = title;
            ws.Cell(1, col).Style.Font.SetBold();
            ws.Cell(2, col).Value = "Row Labels";
            ws.Cell(2, col + 1).Value = h1;
            ws.Cell(2, col + 2).Value = h2;
            ws.Range(2, col, 2, col + 2).Style.Font.SetBold().Fill.SetBackgroundColor(SummaryFill)
                .Border.SetBottomBorder(XLBorderStyleValues.Thin);
            var row = 3;
            foreach (var x in rows)
            {
                ws.Cell(row, col).Value = x.SalesLedger;
                SetValue(ws.Cell(row, col + 1), v1(x));
                SetValue(ws.Cell(row, col + 2), v2(x));
                if (HasPivotDiff(x))
                    ws.Range(row, col, row, col + 2).Style.Fill.SetBackgroundColor(DiffFill);
                row++;
            }
            ws.Cell(row, col).Value = "Grand Total";
            SetValue(ws.Cell(row, col + 1), rows.Sum(v1));
            SetValue(ws.Cell(row, col + 2), rows.Sum(v2));
            ws.Range(row, col, row, col + 2).Style.Font.SetBold().Fill.SetBackgroundColor(SummaryFill)
                .Border.SetTopBorder(XLBorderStyleValues.Thin);
            ws.Range(3, col + 1, row, col + 2).Style.NumberFormat.Format = IndianNumber;
            ws.Column(col).Width = 44;
            ws.Column(col + 1).Width = 22;
            ws.Column(col + 2).Width = 22;
            return row;
        }

        var last = Pivot(1, "Sales Reg.", "Sum of GrossAmount", "Sum of Value", x => x.RegisterGross, x => x.RegisterValue);
        Pivot(6, "HSN", "Sum of Invoice Value", "Sum of Taxable Value", x => x.HsnInvoiceValue, x => x.HsnTaxable);
        Pivot(11, "Difference (Sales Reg. − HSN)", "Diff in Invoice Value", "Diff in Taxable Value",
            x => x.RegisterGross - x.HsnInvoiceValue, x => x.RegisterValue - x.HsnTaxable);
        foreach (var c in new[] { 4, 5, 9, 10 }) ws.Column(c).Width = 4;

        var diffs = all.Where(x => HasPivotDiff(x) || Math.Abs(x.DiffRegisterTb) > 1).ToList();
        var top = last + 3;
        ws.Cell(top, 1).Value = "Differences explained (Sales Reg. vs HSN vs Trial Balance)";
        ws.Cell(top, 1).Style.Font.SetBold();
        (int Col, string Head)[] head =
        [
            (1, "Row Labels"), (2, "Diff in Invoice Value"), (3, "Diff in Taxable Value"), (6, "Invoice value difference — causes"),
            (7, "Trial Balance (net credit)"), (8, "Sales Reg. Value − TB"), (11, "Taxable value / trial balance — remarks"),
        ];
        foreach (var (col, text) in head)
        {
            ws.Cell(top + 1, col).Value = text;
            ws.Cell(top + 1, col).Style.Font.SetBold().Fill.SetBackgroundColor(SummaryFill).Alignment.SetWrapText();
        }
        var row2 = top + 2;
        if (diffs.Count == 0)
        {
            ws.Cell(row2, 1).Value = "No ledger differs by more than ₹1.";
            row2++;
        }
        foreach (var x in diffs)
        {
            ws.Cell(row2, 1).Value = x.SalesLedger;
            SetValue(ws.Cell(row2, 2), x.DiffInvoiceValue);
            SetValue(ws.Cell(row2, 3), x.DiffRegisterHsn);
            ws.Cell(row2, 6).Value = x.InvoiceRemarks;
            SetValue(ws.Cell(row2, 7), x.TrialBalance);
            SetValue(ws.Cell(row2, 8), x.DiffRegisterTb);
            ws.Cell(row2, 11).Value = x.Remarks;
            ws.Range(row2, 1, row2, 13).Style.Alignment.SetVertical(XLAlignmentVerticalValues.Top);
            ws.Range(row2, 11, row2, 13).Merge();
            ws.Cell(row2, 6).Style.Alignment.SetWrapText();
            ws.Cell(row2, 11).Style.Alignment.SetWrapText();
            if (HasPivotDiff(x)) ws.Cell(row2, 1).Style.Fill.SetBackgroundColor(DiffFill);
            row2++;
        }
        ws.Range(top + 2, 2, row2, 3).Style.NumberFormat.Format = IndianNumber;
        ws.Range(top + 2, 7, row2, 8).Style.NumberFormat.Format = IndianNumber;

        string[] lines =
        [
            "Notes",
            "Sales Reg.: every invoice in SalesVoucher for the period (approved or not, reported or not). GrossAmount = invoice bill amount; Value = line amount × exchange rate.",
            "HSN: the same documents and netting as the HSN summary (B2B + B2C) — reported invoices plus credit/debit notes (notes sit under the note's own ledger). Invoice Value = taxable value + GST.",
            "Diff in Invoice Value = Sales Reg. GrossAmount − HSN Invoice Value; Diff in Taxable Value = Sales Reg. Value − HSN Taxable Value. Causes are signed as they move the difference.",
            "Trial Balance: net credit (credit − debit) posted to the ledger for the period.",
            "Highlighted rows: invoice value or taxable value differs by more than ₹1.",
        ];
        for (var i = 0; i < lines.Length; i++)
        {
            ws.Cell(row2 + 1 + i, 1).Value = lines[i];
            if (i == 0) ws.Cell(row2 + 1 + i, 1).Style.Font.SetBold();
        }
        ws.SheetView.FreezeRows(2);
    }

    private static bool HasPivotDiff(Gstr1LedgerReconRowDto x) =>
        Math.Abs(x.RegisterGross - x.HsnInvoiceValue) > 1 || Math.Abs(x.RegisterValue - x.HsnTaxable) > 1;

    private static string? Date(DateTime? d) => d?.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture);

    private static string YesNo(bool v) => v ? "Yes" : "No";

    private static void SetValue(IXLCell cell, object? value)
    {
        switch (value)
        {
            case null:
                return;
            case decimal d:
                cell.Value = d;
                cell.Style.NumberFormat.Format = "0.00";
                return;
            case int i:
                cell.Value = i;
                return;
            case long l:
                cell.Value = l;
                return;
            case DateTime dt:
                cell.Value = dt;
                cell.Style.DateFormat.Format = "dd-MMM-yyyy";
                return;
            default:
                cell.Value = value.ToString();
                return;
        }
    }
}
