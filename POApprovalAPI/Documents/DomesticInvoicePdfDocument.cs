using System.Globalization;
using POApprovalAPI.Models;
using POApprovalAPI.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace POApprovalAPI.Documents;

public class DomesticInvoicePdfDocument : IDocument
{
    private readonly DomesticInvoiceDto _invoice;
    private readonly List<Dictionary<string, string?>> _rows;
    private readonly string _copy;
    private readonly byte[]? _logo;

    public DomesticInvoicePdfDocument(DomesticInvoiceDto invoice, List<Dictionary<string, string?>> rows, string copy)
    {
        _invoice = invoice;
        _rows = rows;
        _copy = copy;
        _logo = TryLoadLogo(Oel());
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(12);
            page.DefaultTextStyle(x => x.FontSize(7).FontFamily(Fonts.Arial));
            page.Content().Element(ComposePage);
        });
    }

    private void ComposePage(IContainer container)
    {
        var title = Text(F("TypeOfInvoice"), _invoice.TypeOfInvoice);
        if (title.Length == 0) title = "TAX INVOICE";
        var copy = Text(F("Header"), _copy);
        var seller = Text(F("DispatchAddress"), "");
        var company = Text(F("CompanyName"), _invoice.CompanyName);
        var lines = LineRows();

        container.Border(0.8f).Column(page =>
        {
            page.Item().BorderBottom(0.6f).Padding(3).Row(row =>
            {
                row.RelativeItem();
                row.RelativeItem(2).AlignCenter().Column(c =>
                {
                    c.Item().AlignCenter().Text(title.ToUpperInvariant()).Bold().FontSize(11);
                    if (!Fibco())
                        c.Item().AlignCenter().Text("Under Section 31 of CGST Act 2017 and Rule 46 of the CGST Rules 2017").FontSize(6);
                });
                row.RelativeItem().AlignRight().AlignMiddle().Text(copy).Bold().FontSize(8);
            });

            page.Item().Row(row =>
            {
                row.RelativeItem().BorderRight(0.6f).Padding(4).Column(left =>
                {
                    left.Item().Row(r =>
                    {
                        r.ConstantItem(42).Height(42).Element(logo =>
                        {
                            if (_logo != null) logo.Image(_logo).FitArea();
                        });
                        r.RelativeItem().PaddingLeft(4).Column(c =>
                        {
                            c.Item().Text(company).Bold().FontSize(11);
                            if (seller.Length > 0) c.Item().PaddingTop(2).Text(seller).FontSize(6.5f);
                        });
                    });
                });
                row.RelativeItem().Element(c => MetaGrid(c));
            });

            page.Item().BorderTop(0.6f).Row(row =>
            {
                row.RelativeItem().BorderRight(0.6f).Padding(4).Column(c =>
                {
                    c.Item().Text("Billed To (Name & Add) :").SemiBold();
                    c.Item().Text(Text(F("BuyerName"), _invoice.BuyerName)).Bold();
                    c.Item().Text(Join(F("Address"), F("City"), F("State"), F("PinCode"), F("Country")));
                    var pan = Text(F("BuyerPanNo"), "");
                    var gst = Text(F("NewGSTNo"), "");
                    if (pan.Length > 0) c.Item().Text("PAN No : " + pan);
                    if (gst.Length > 0) c.Item().Text("GSTIN : " + gst);
                });
                row.RelativeItem();
            });

            page.Item().BorderTop(0.6f).MinHeight(52).Row(row =>
            {
                row.RelativeItem().BorderRight(0.6f).Padding(4).Column(c =>
                {
                    c.Item().Text("Shipped To (Name & Add) :").SemiBold();
                    c.Item().Text(Text(F("Consignee"), _invoice.ConsigneeAddress));
                    var notifier = Text(F("notifier"), _invoice.Notifier);
                    if (notifier.Length > 0)
                    {
                        c.Item().PaddingTop(4).Text("NOTIFIER PARTY").SemiBold();
                        c.Item().Text(notifier);
                    }
                });
                row.RelativeItem().Padding(4).Column(c =>
                {
                    if (Oel())
                    {
                        c.Item().Text("Place of Supply :  " + Text(F("State"), _invoice.State));
                        c.Item().Text("Terms of Supply :  " + Text(F("TermsOfDelivery"), _invoice.TermsOfDelivery));
                        c.Item().Text("Transporter Name :  " + Text(F("TransportName"), _invoice.TransportName));
                        c.Item().Text("L R NO :  " + Text(F("LRNo"), _invoice.LrNo));
                        c.Item().Text("L R Date :  " + DateOf(F("LRDate"), _invoice.LrDate));
                    }
                    else
                    {
                        c.Item().Text("Dispatched From (if other than billed from) :").SemiBold();
                        var from = Join(F("dispfrm"), F("dispfrm_add"));
                        if (from.Length == 0) from = _invoice.DispatchFrom ? Join(_invoice.DispatchFromName, _invoice.DispatchFromAddress) : "";
                        if (from.Length > 0) c.Item().Text(from);
                    }
                });
            });

            page.Item().Element(c => LinesTable(c, lines));
            page.Item().Element(c => Totals(c, lines));
            page.Item().Element(c => Footer(c));
        });
    }

    private void MetaGrid(IContainer container)
    {
        var number = Text(F("GSTInvNo"), string.IsNullOrWhiteSpace(_invoice.GstInvNo) ? (_invoice.InvNo == 0 ? "Preview" : _invoice.InvType) : _invoice.GstInvNo);
        container.Column(col =>
        {
            Pair(col, "Reverse Charge", "No", "Invoice No. :", number);
            Pair(col, "Delivery Note", Text(F("DelChallanNo"), _invoice.DelChallanNo), "Dated of Supply :", DateOf(F("InvDate"), _invoice.InvDate));
            Pair(col, "Buyer's Order No. :  " + Text(F("BuyerOrderNo"), _invoice.BuyerOrderNo) + "    Date :  " + DateOf(F("BuyerDate"), _invoice.BuyerOrderDate),
                "", "Terms of Payment", Text(F("TermsOfPayment"), _invoice.TermsOfPayment));
            Pair(col, "Dispatch Doc. No. :  " + Text(F("DespatchNo"), _invoice.DespatchNo), "", "Date :", DateOf(F("DespatchDate"), _invoice.DespatchDate));
            if (Oel())
            {
                Pair(col, "", "", "Vehical No. :", Text(F("VehicalNo"), _invoice.VehicleNo));
                return;
            }
            var lr = Join(Text(F("TransportName"), _invoice.TransportName), Text(F("LRNo"), _invoice.LrNo), DateOf(F("LRDate"), _invoice.LrDate));
            Pair(col, "Transporter Name, LR No. & Date", lr, "Vehical No. :", Text(F("VehicalNo"), _invoice.VehicleNo));
            Pair(col, "Place of Supply (Name of State) :", Text(F("State"), _invoice.State), "Terms of Supply", Text(F("TermsOfDelivery"), _invoice.TermsOfDelivery));
        });
    }

    private static void Pair(ColumnDescriptor col, string leftLabel, string leftValue, string rightLabel, string rightValue)
    {
        col.Item().BorderBottom(0.4f).Row(row =>
        {
            row.RelativeItem().BorderRight(0.4f).Padding(2).Column(c =>
            {
                if (leftLabel.Length > 0) c.Item().Text(leftLabel).FontSize(6);
                if (leftValue.Length > 0) c.Item().Text(leftValue).SemiBold();
            });
            row.RelativeItem().Padding(2).Column(c =>
            {
                if (rightLabel.Length > 0) c.Item().Text(rightLabel).FontSize(6);
                if (rightValue.Length > 0) c.Item().Text(rightValue).SemiBold();
            });
        });
    }

    private void LinesTable(IContainer container, List<Dictionary<string, string?>> lines)
    {
        container.Table(table =>
        {
            var fibco = Fibco();
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(36);
                columns.RelativeColumn(3);
                columns.ConstantColumn(42);
                columns.ConstantColumn(28);
                columns.ConstantColumn(42);
                columns.ConstantColumn(48);
                columns.ConstantColumn(52);
                if (fibco)
                {
                    columns.ConstantColumn(42);
                    columns.ConstantColumn(52);
                }
                else
                {
                    columns.ConstantColumn(28);
                    columns.ConstantColumn(40);
                    columns.ConstantColumn(28);
                    columns.ConstantColumn(40);
                    columns.ConstantColumn(28);
                    columns.ConstantColumn(40);
                }
            });
            table.Header(header =>
            {
                Head(header, "Nos of Pkg");
                Head(header, "Description of Goods");
                Head(header, "HSN ACS");
                Head(header, "UOM");
                Head(header, "Qty");
                Head(header, "Rate (INR) / Unit");
                Head(header, "Assessable Value (INR)");
                if (fibco)
                {
                    Head(header, "VAT %");
                    Head(header, "VAT Amt");
                }
                else
                {
                    Head(header, "CGST %");
                    Head(header, "CGST Amt");
                    Head(header, "SGST %");
                    Head(header, "SGST Amt");
                    Head(header, "IGST %");
                    Head(header, "IGST Amt");
                }
            });

            var width = fibco ? 9 : 13;
            if (lines.Count == 0)
            {
                for (var i = 0; i < width; i++)
                    table.Cell().Border(0.3f).MinHeight(28).Text("");
                return;
            }

            var first = true;
            foreach (var line in lines)
            {
                Cell(table, V(line, "NoofPkg"));
                Cell(table, Join(V(line, "Commodity"), V(line, "ItemDesc")));
                Cell(table, V(line, "HSNCode"));
                Cell(table, V(line, "Unit"));
                Cell(table, Num(V(line, "Qty"), "N3"));
                Cell(table, Num(V(line, "Rate"), "N3"));
                Cell(table, Num(V(line, "Amount"), "N2"));
                if (fibco)
                {
                    Cell(table, first ? Num(VatPercent(), "N2") : "");
                    Cell(table, first ? Num(VatAmount(lines).ToString(CultureInfo.InvariantCulture), "N2") : "");
                    first = false;
                }
                else
                {
                    Cell(table, Num(V(line, "CGSTrate"), "N2"));
                    Cell(table, Num(V(line, "CGSTamt"), "N2"));
                    Cell(table, Num(V(line, "SGSTRate"), "N2"));
                    Cell(table, Num(V(line, "SGSTAmt"), "N2"));
                    Cell(table, Num(V(line, "IGSTrate"), "N2"));
                    Cell(table, Num(V(line, "IGSTAmt"), "N2"));
                }
            }
        });
    }

    private void Totals(IContainer container, List<Dictionary<string, string?>> lines)
    {
        var taxable = lines.Sum(l => Dec(V(l, "Amount")));
        var cgst = lines.Sum(l => Dec(V(l, "CGSTamt")));
        var sgst = lines.Sum(l => Dec(V(l, "SGSTAmt")));
        var igst = lines.Sum(l => Dec(V(l, "IGSTAmt")));
        var qty = lines.Sum(l => Dec(V(l, "Qty")));
        var headerQty = Dec(F("TotalQty"));
        if (headerQty == 0) headerQty = qty;
        var grand = Dec(F("TotalAmt"));
        if (grand == 0) grand = _invoice.GrandTotal != 0 ? _invoice.GrandTotal : taxable + cgst + sgst + igst;
        var words = Text(F("TotalAmtWord"), DomesticInvoiceMath.RupeesInWords(grand));
        var policy = Text(F("Insurance"), _invoice.InsurancePolicyNo);

        container.Column(stack =>
        {
        stack.Item().Row(row =>
        {
            row.RelativeItem().BorderRight(0.6f).Padding(4).Column(c =>
            {
                if (Oel())
                {
                    var basicPct = _invoice.OelBasic;
                    var cessPct = _invoice.OelCess1;
                    var basicDuty = Math.Round(taxable * basicPct / 100m, 0, MidpointRounding.AwayFromZero);
                    var cvd = Math.Round((taxable + basicDuty) * _invoice.OelCvd / 100m, 0, MidpointRounding.AwayFromZero);
                    var sws = Math.Round((cvd + basicDuty) * cessPct / 100m, 0, MidpointRounding.AwayFromZero);
                    Money(c, "Basic Value", taxable);
                    Money(c, $"Basic CUS ({basicPct:0.##}%)", basicDuty);
                    Money(c, $"SWS CUSTOM ({cessPct:0.##}%)", sws);
                    Money(c, "Accesible Value", taxable);
                }
                else if (policy.Length > 0) c.Item().Text("Insurance Policy No. :  " + policy);
            });
            row.ConstantItem(220).Padding(4).Column(c =>
            {
                Money(c, Oel() ? "Assessable Value" : "Taxable Value", taxable);
                if (Fibco())
                {
                    Money(c, "VAT %", Dec(VatPercent()));
                    Money(c, "Add : VAT", VatAmount(lines));
                }
                else
                {
                    Money(c, "Add : CGST", cgst);
                    Money(c, "Add : SGST", sgst);
                    Money(c, "Add : IGST", igst);
                    Money(c, "Tax Amount GST", cgst + sgst + igst);
                    c.Item().Row(r =>
                    {
                        r.RelativeItem().Text("GST Payable on Reverse Charge :");
                        r.ConstantItem(50).AlignRight().Text("N.A.");
                    });
                }
            });
        });

        stack.Item().BorderTop(0.6f).Row(row =>
        {
            row.RelativeItem().BorderRight(0.6f).Padding(3).Text(text =>
            {
                text.Span("Total Qty :  ").SemiBold();
                text.Span(headerQty.ToString("N3", Indian)).Bold();
            });
            row.RelativeItem().Padding(3).Row(r =>
            {
                r.RelativeItem().Text(Oel() ? "Invoice Total" : "Invoice Total (INR) :").Bold();
                r.ConstantItem(70).AlignRight().Text(grand.ToString("N2", Indian)).Bold();
            });
        });

        if (Oel())
            stack.Item().BorderTop(0.6f).Padding(3).Text("Insurance Policy No. :  " + policy);

        stack.Item().BorderTop(0.6f).Padding(3).Row(row =>
        {
            row.RelativeItem().Text("Advance Payment Voucher No & DT :   NIL");
            row.RelativeItem().Text(text =>
            {
                text.Span("Invoice Amount (In Words) Rs.  ").SemiBold();
                text.Span(words);
            });
        });
        stack.Item().PaddingHorizontal(3).PaddingBottom(3).Text("Advance Received Amount :   NIL");
        });
    }

    private void Footer(IContainer container)
    {
        var company = Text(F("CompanyName"), _invoice.CompanyName);
        var terms = Text(F("TermsNCondition"), "");
        container.Column(stack =>
        {
        stack.Item().BorderTop(0.6f).Row(row =>
        {
            row.RelativeItem().BorderRight(0.6f).Padding(3).Column(c =>
            {
                c.Item().Text("Declaration").SemiBold();
                c.Item().Text(Text(F("InvoiceDecl"), "")).FontSize(6);
            });
            if (!Oel())
            {
                row.RelativeItem().BorderRight(0.6f).Padding(3).Column(c =>
                {
                    c.Item().AlignCenter().Text("Bank Detail :").SemiBold();
                    c.Item().Text(Text(F("nBankdetail"), "")).FontSize(6.5f);
                });
            }
            row.RelativeItem().Padding(3).Column(c =>
            {
                c.Item().AlignRight().Text("E & E O").Bold();
                c.Item().AlignRight().Text("For, " + company).SemiBold();
                c.Item().PaddingTop(28).AlignRight().Text("Authorised Signatory");
            });
        });
        if (terms.Length > 0)
            stack.Item().BorderTop(0.6f).Padding(3).Text(terms).FontSize(5.5f);
        });
    }

    private List<Dictionary<string, string?>> LineRows()
    {
        if (_rows.Count > 0)
            return _rows.Where(r => V(r, "Commodity").Length > 0 || V(r, "ItemDesc").Length > 0 || Dec(V(r, "Qty")) != 0).ToList();

        return _invoice.Lines
            .Where(l => !string.IsNullOrWhiteSpace(l.Commodity) || !string.IsNullOrWhiteSpace(l.ItemDesc))
            .Select(l => new Dictionary<string, string?>
            {
                ["NoofPkg"] = l.NoOfPkg,
                ["Commodity"] = l.Commodity,
                ["ItemDesc"] = l.ItemDesc,
                ["HSNCode"] = l.Hsn,
                ["Unit"] = l.Uom,
                ["Qty"] = l.Qty.ToString("0.###"),
                ["Rate"] = l.Rate.ToString("0.###"),
                ["Amount"] = l.Amount.ToString("0.00"),
                ["CGSTrate"] = l.CgstRate.ToString("0.##"),
                ["CGSTamt"] = l.CgstAmt.ToString("0.00"),
                ["SGSTRate"] = l.SgstRate.ToString("0.##"),
                ["SGSTAmt"] = l.SgstAmt.ToString("0.00"),
                ["IGSTrate"] = l.IgstRate.ToString("0.##"),
                ["IGSTAmt"] = l.IgstAmt.ToString("0.00"),
            }).ToList();
    }

    private string F(string key) => _rows.Count == 0 ? "" : V(_rows[0], key);

    private static string V(Dictionary<string, string?> row, string key)
    {
        foreach (var pair in row)
            if (pair.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                return pair.Value?.Trim() ?? "";
        return "";
    }

    private static string Text(string primary, string fallback) =>
        string.IsNullOrWhiteSpace(primary) ? fallback?.Trim() ?? "" : primary.Trim();

    private static string Join(params string[] parts) =>
        string.Join(", ", parts.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim()));

    private static readonly CultureInfo Indian = CultureInfo.GetCultureInfo("en-IN");

    private static decimal Dec(string raw) =>
        decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var n) ? n : 0;

    private static string Num(string raw, string format)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var n)
            ? n.ToString(format, Indian)
            : raw;
    }

    private static string DateOf(string raw, string fallback)
    {
        var value = string.IsNullOrWhiteSpace(raw) ? fallback : raw;
        return DateTime.TryParse(value, out var dt) ? dt.ToString("dd-MM-yyyy") : value ?? "";
    }

    private static void Head(TableCellDescriptor header, string text) =>
        header.Cell().Border(0.3f).Padding(2).AlignCenter().Text(text).SemiBold().FontSize(5.5f);

    private static void Cell(TableDescriptor table, string text) =>
        table.Cell().Border(0.3f).Padding(2).Text(text).FontSize(6);

    private static void Money(ColumnDescriptor column, string label, decimal amount) =>
        column.Item().Row(row =>
        {
            row.RelativeItem().Text(label);
            row.ConstantItem(70).AlignRight().Text(amount.ToString("N2", Indian));
        });

    private bool Oel() =>
        Text(F("CompanyName"), _invoice.CompanyName).Equals(DomesticInvoiceMath.OelCompany, StringComparison.OrdinalIgnoreCase);

    private bool Fibco() =>
        Text(F("CompanyName"), _invoice.CompanyName).Equals(DomesticInvoiceMath.FibcoCompany, StringComparison.OrdinalIgnoreCase);

    private string VatPercent() => Text(F("vat"), _invoice.Vat.ToString(CultureInfo.InvariantCulture));

    private decimal VatAmount(List<Dictionary<string, string?>> lines)
    {
        var stored = Dec(F("VatAmt"));
        if (stored == 0 && _rows.Count == 0) stored = _invoice.VatAmt;
        if (stored != 0) return stored;
        return Math.Round(lines.Sum(l => Dec(V(l, "Amount"))) * Dec(VatPercent()) / 100m, 0, MidpointRounding.ToEven);
    }

    private static byte[]? TryLoadLogo(bool oswal)
    {
        var file = oswal ? "oswal_logo.jpg" : "hcp_logo.jpeg";
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Assets", file),
            Path.Combine(Directory.GetCurrentDirectory(), "Assets", file),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Assets", file)),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "public", file)),
        };
        foreach (var path in candidates)
            if (File.Exists(path))
                return File.ReadAllBytes(path);
        return null;
    }
}
