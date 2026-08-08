using System.Globalization;
using POApprovalAPI.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace POApprovalAPI.Documents;

public class PurchaseOrderDocument : IDocument
{
    // Western thousands separators to match ERP PO print (2,305,322.56)
    private static readonly CultureInfo MoneyCulture = CultureInfo.GetCultureInfo("en-US");
    private static readonly string AccentRed = "#C00000";

    private readonly PurchaseOrderPdfModel _model;
    private readonly byte[]? _logoBytes;

    public PurchaseOrderDocument(PurchaseOrderPdfModel model)
    {
        _model = model;
        _logoBytes = TryLoadLogo();
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.MarginHorizontal(14);
            page.MarginVertical(10);
            page.DefaultTextStyle(x => x.FontSize(7).FontFamily(Fonts.Arial));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().AlignRight().DefaultTextStyle(x => x.FontSize(7)).Text(text =>
            {
                text.Span("Page ");
                text.CurrentPageNumber();
                text.Span(" of ");
                text.TotalPages();
            });
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.ConstantItem(54).Height(48).Element(logo =>
                {
                    if (_logoBytes != null)
                        logo.Image(_logoBytes).FitArea();
                });

                row.RelativeItem().PaddingHorizontal(8).Column(c =>
                {
                    c.Item().AlignCenter().Text(_model.CompanyName).Bold().FontSize(14);
                    if (!string.IsNullOrWhiteSpace(_model.FormerlyKnownAs))
                    {
                        c.Item().AlignCenter()
                            .Text($"(Formerly known as {_model.FormerlyKnownAs})")
                            .FontSize(7);
                    }

                    c.Item().PaddingTop(2).AlignCenter().Text(text =>
                    {
                        text.Span("Corporate Office: ").SemiBold();
                        text.Span(
                            "H. B.Jirawala House,13, Navbharat Society, Opp . Panchsheel Bus Stop, Usmanpura, Ahmedabad,Gujarat,India-380013");
                    });
                    c.Item().AlignCenter()
                        .Text("Phone No. (079) 27550764 / 27561000  Fax No. 27551764")
                        .FontSize(6.5f);
                });

                row.ConstantItem(130).AlignRight().AlignMiddle().Text(text =>
                {
                    text.Span("Created by: ").SemiBold();
                    // Match ERP print: "Name ()"
                    var createdBy = string.IsNullOrWhiteSpace(_model.CreatedBy)
                        ? ""
                        : _model.CreatedBy.Contains('(')
                            ? _model.CreatedBy
                            : $"{_model.CreatedBy} ()";
                    text.Span(createdBy);
                });
            });

            col.Item().PaddingTop(4).AlignCenter()
                .Text(string.IsNullOrWhiteSpace(_model.DocumentTitle) ? "Purchase Order" : _model.DocumentTitle)
                .Bold().FontSize(13);
            col.Item().AlignCenter().Text("Pur/04 R-0").FontSize(7);

            // Reference layout:
            // Row1: P.O. No. | Purchase Ref No. | Date
            // Row2: Indent's Name | Indent Deptt: | Indent No. | Indent Date:
            col.Item().PaddingTop(4).Border(0.6f).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.0f);
                    columns.RelativeColumn(1.5f);
                    columns.RelativeColumn(1.15f);
                    columns.RelativeColumn(1.35f);
                    columns.RelativeColumn(1.0f);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(1.0f);
                    columns.RelativeColumn(1.2f);
                });

                MetaLabel(table, "P.O. No.");
                MetaValue(table, _model.PoNo);
                MetaLabel(table, "Purchase Ref No.");
                MetaValue(table, _model.PurchaseRefNo);
                MetaLabel(table, "Date");
                table.Cell().ColumnSpan(3).Border(0.4f).Padding(2)
                    .Text(FormatDate(_model.PoDate)).FontSize(7);

                MetaLabel(table, "Indent's Name");
                MetaValue(table, _model.IndentName);
                MetaLabel(table, "Indent Deptt:");
                MetaValue(table, _model.DepttName);
                MetaLabel(table, "Indent No.");
                MetaValue(table, _model.IndentNo);
                MetaLabel(table, "Indent Date:");
                MetaValue(table, FormatDate(_model.IndentDate));
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(6).Column(col =>
        {
            col.Spacing(5);

            col.Item().Row(row =>
            {
                row.RelativeItem().Border(0.6f).Padding(4).Column(c =>
                {
                    c.Item().Text("Name and Address of Vendor").Bold().FontSize(8);
                    c.Item().PaddingTop(2).Text($"M/s {_model.VendorName}").SemiBold();
                    c.Item().Text(CleanMultiline(_model.VendorAddress));
                    if (!string.IsNullOrWhiteSpace(_model.VendorGst))
                        c.Item().PaddingTop(2).Text($"GST NO: {_model.VendorGst}");
                    if (!string.IsNullOrWhiteSpace(_model.ContactName))
                        c.Item().Text($"Kind Attn MR. {_model.ContactName}");
                    if (!string.IsNullOrWhiteSpace(_model.ContactNo))
                        c.Item().Text($"Contact No. {_model.ContactNo}");
                });

                row.RelativeItem().Border(0.6f).BorderLeft(0).Padding(4).Column(c =>
                {
                    c.Item().Text("Delivery & Billing Address").Bold().FontSize(8);
                    c.Item().PaddingTop(2).Text(_model.CompanyName).SemiBold();
                    foreach (var line in SplitAddressLines(_model.CompanyAddress, _model.CompanyGst))
                        c.Item().Text(line);
                });
            });

            // GST No. under delivery box (as on ERP print)
            if (!string.IsNullOrWhiteSpace(_model.CompanyGst))
            {
                col.Item().AlignRight().PaddingRight(4)
                    .Text($"GST No. {_model.CompanyGst}")
                    .FontSize(7);
            }

            col.Item().Text("We are pleased to place an our valued order for the following item /.s");

            col.Item().Element(ComposeItemsTable);

            col.Item().Row(row =>
            {
                row.RelativeItem(3).Border(0.6f).MinHeight(42).Padding(4).Column(c =>
                {
                    c.Item().Text("Note").Bold();
                });

                row.RelativeItem(2).Border(0.6f).BorderLeft(0).Padding(4).AlignRight().Column(c =>
                {
                    TotalsLine(c, "Sub Total", _model.SubTotal, false);
                    TotalsLine(c, "C.G.S.T. Amount", _model.CGSTAmount, false);
                    TotalsLine(c, "S.G.S.T. Amount", _model.SGSTAmount, false);
                    if (_model.IGSTAmount > 0)
                        TotalsLine(c, "I.G.S.T. Amount", _model.IGSTAmount, false);
                    TotalsLine(c, "Total Amount", _model.TotalAmount, true);
                });
            });

            col.Item().Border(0.6f).Row(row =>
            {
                row.RelativeItem(2.2f).Padding(3).Column(c =>
                {
                    c.Item().Text("Terms And Condition").Bold().FontSize(8);

                    c.Item().PaddingTop(3).Text(text =>
                    {
                        text.Span("Delivery Terms").SemiBold();
                        text.Span($"    {_model.DeliveryTerms}");
                    });

                    // Schedule + delivery date on same row (date nearer center of terms column)
                    c.Item().Row(r =>
                    {
                        r.RelativeItem(2).Text(text =>
                        {
                            text.Span("Delivery Date On or Before").SemiBold();
                            if (!string.IsNullOrWhiteSpace(_model.DeliverySchedule))
                                text.Span($"    {_model.DeliverySchedule}");
                        });
                        r.RelativeItem(1).AlignCenter()
                            .Text(FormatDeliveryDateShort(_model.DeliveryDate));
                    });

                    c.Item().Text(text =>
                    {
                        text.Span("Payment Terms").SemiBold();
                        text.Span($"    {_model.PaymentTerms}");
                    });
                    c.Item().Text(text =>
                    {
                        text.Span("Mode of Transport").SemiBold();
                        text.Span($"    {_model.ModeOfTransport}");
                    });
                    // PO note is shown below TDS (matches ERP print), not here
                });

                row.RelativeItem(1.4f).BorderLeft(0.5f).Padding(3).Column(c =>
                {
                    c.Item().Text("Postal Address").Bold().FontSize(8);
                    c.Item().PaddingTop(3);
                    foreach (var line in SplitAddressLines(_model.PostalAddress, _model.CompanyGst))
                        c.Item().Text(line);
                });
            });

            // ERP print: bordered block with TDS, then Note, then E & O.E
            col.Item().Border(0.6f).Padding(4).Column(block =>
            {
                if (_model.TdsAmount > 0)
                {
                    block.Item()
                        .Text(
                            $"As Total Purchase Amount is exceeded from 50 lacs in current financial yr. TDS will be deducted as 0.1% : {FormatMoney(_model.TdsAmount)}")
                        .FontColor(AccentRed)
                        .SemiBold()
                        .FontSize(7.5f);
                }

                var noteText = FirstNonEmptyNote(_model.SpecialNote, _model.Note);
                if (!string.IsNullOrWhiteSpace(noteText))
                {
                    block.Item().PaddingTop(_model.TdsAmount > 0 ? 4 : 0).Text(text =>
                    {
                        text.Span("Note : ").Bold();
                        text.Span(noteText);
                    });
                }

                block.Item().PaddingTop(4).Text("E & O.E");
            });

            // Left: static terms 1-8 | Right: approval + signature block (right-aligned as a unit)
            col.Item().Row(row =>
            {
                row.RelativeItem(2.35f).Column(tc =>
                {
                    tc.Spacing(1.5f);
                    foreach (var (line, emphasize) in GetStaticTerms(_model.VendorName, _model.StoreEmail))
                    {
                        var item = tc.Item().Text(line).FontSize(6.5f);
                        if (emphasize)
                            item.FontColor(AccentRed);
                    }
                });

                row.RelativeItem(1.25f).PaddingLeft(12).AlignRight().Column(c =>
                {
                    c.Item().AlignRight().Text(text =>
                    {
                        text.Span("Final Approval Date: ").SemiBold();
                        text.Span(FormatDate(_model.ApprovalDate));
                    });
                    c.Item().PaddingTop(22).AlignRight()
                        .Text($"For, {_model.CompanyName}").SemiBold();
                    c.Item().PaddingTop(40).AlignRight()
                        .Text("Authorized Signature");
                });
            });
        });
    }

    private void ComposeItemsTable(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(16);   // SN
                columns.RelativeColumn(1.15f); // Item Code
                columns.RelativeColumn(2.0f);  // Description
                columns.RelativeColumn(0.85f); // HSN
                columns.RelativeColumn(0.55f); // Unit
                columns.RelativeColumn(0.85f); // Qty
                columns.RelativeColumn(0.85f); // Rate
                columns.RelativeColumn(0.7f);  // Discount
                columns.RelativeColumn(1.15f); // Amount
                columns.RelativeColumn(0.65f); // GST%
                columns.RelativeColumn(0.95f); // CGST
                columns.RelativeColumn(0.95f); // SGST
                columns.RelativeColumn(0.85f); // IGST
            });

            table.Header(header =>
            {
                HeaderCell(header, "SN");
                HeaderCell(header, "Item Code");
                HeaderCell(header, "Description");
                HeaderCell(header, "HSNCODE");
                HeaderCell(header, "Unit");
                HeaderCell(header, "Qty");
                HeaderCell(header, $"Rate({CurrencyLabel()})");
                HeaderCell(header, "Discount");
                HeaderCell(header, $"Amount({CurrencyLabel()})");
                HeaderCell(header, "GST Per");
                HeaderCell(header, "CGST Amount");
                HeaderCell(header, "SGST Amount");
                HeaderCell(header, "IGST Amount");
            });

            foreach (var item in _model.Items)
            {
                BodyCell(table, item.SerialNo.ToString(), false);
                BodyCell(table, item.ItemCode, false);
                BodyCell(table, item.ItemDesc, false);
                BodyCell(table, item.HsnCode, false);
                BodyCell(table, item.Unit, false);
                BodyCell(table, FormatMoney(item.Qty), true);
                BodyCell(table, FormatRate(item.Rate), true);
                BodyCell(table, FormatMoney(item.Discount), true);
                // ERP print shows Amount(Rs) without thousand separators
                BodyCell(table, FormatAmountRaw(item.Amount), true);
                BodyCell(table, FormatMoney(item.GstPer), true);
                BodyCell(table, FormatMoney(item.CGSTAmount), true);
                BodyCell(table, FormatMoney(item.SGSTAmount), true);
                BodyCell(table, FormatMoney(item.IGSTAmount), true);
            }
        });
    }

    private static IEnumerable<string> SplitAddressLines(string? address, string? companyGst)
    {
        var cleaned = CleanMultiline(address);
        if (string.IsNullOrWhiteSpace(cleaned))
            yield break;

        var gstIdx = cleaned.IndexOf("GST IN:", StringComparison.OrdinalIgnoreCase);
        var cinIdx = cleaned.IndexOf("CIN:", StringComparison.OrdinalIgnoreCase);

        if (gstIdx < 0)
        {
            yield return cleaned;
            if (!string.IsNullOrWhiteSpace(companyGst))
                yield return $"GST IN: {companyGst}";
            yield break;
        }

        var street = cleaned[..gstIdx].Trim().TrimEnd(',');
        if (!string.IsNullOrWhiteSpace(street))
            yield return street;

        if (cinIdx > gstIdx)
        {
            yield return cleaned[gstIdx..cinIdx].Trim();
            yield return cleaned[cinIdx..].Trim();
        }
        else
        {
            yield return cleaned[gstIdx..].Trim();
        }
    }

    private static void MetaLabel(TableDescriptor table, string text)
        => table.Cell().Border(0.4f).Background(Colors.Grey.Lighten3).Padding(2)
            .Text(text).SemiBold().FontSize(6.5f);

    private static void MetaValue(TableDescriptor table, string text)
        => table.Cell().Border(0.4f).Padding(2).Text(text ?? "").FontSize(7);

    private static void TotalsLine(ColumnDescriptor c, string label, decimal amount, bool bold)
    {
        var item = c.Item().AlignRight().Text($"{label}    {FormatMoney(amount)}");
        if (bold)
            item.Bold().FontSize(8.5f);
    }

    private static void HeaderCell(TableCellDescriptor header, string text)
    {
        header.Cell()
            .Border(0.5f)
            .Background(Colors.Grey.Lighten3)
            .Padding(2)
            .AlignCenter()
            .Text(text)
            .SemiBold()
            .FontSize(5.8f);
    }

    private static void BodyCell(TableDescriptor table, string text, bool rightAlign)
    {
        var cell = table.Cell().Border(0.5f).Padding(2);
        if (rightAlign)
            cell.AlignRight().Text(text).FontSize(6.3f);
        else
            cell.Text(text).FontSize(6.3f);
    }

    private static IEnumerable<(string Line, bool Emphasize)> GetStaticTerms(string vendorName, string storeEmail)
    {
        var vendor = string.IsNullOrWhiteSpace(vendorName) ? "the vendor" : vendorName;
        var email = string.IsNullOrWhiteSpace(storeEmail)
            ? "gppstore@champalalgroup.com"
            : storeEmail.Trim();

        yield return ("1. Please mention the P.O. No. in your Invoice and send Invoice Two copy along with the materials.", false);
        yield return ("2. Acceptance : If the ordered Materials is not as per the technical specification / drawings, then we reserve our right to reject the materials and return the same on your cost & expenses.", false);
        yield return ($"3. Email address : {email} () ()", false);
        yield return ("4. Please send E-way bill copy along with material, dispatch from out of Gujarat", false);
        yield return ("5. Please ensure to mention PONo, GST No. and HSN Code in Invoice, otherwise we will deduc 10% of Bill Amount", false);
        yield return ("6. Qty. of supply must be as per Purchase Order, No excess material will be accepted.", true);
        yield return ($"7. You M/s {vendor} will not give any commission, brokerage, valuable thing, pecuniary advantage or any undue advantage or benefits by any way directly or indirectly to our any employee or any other person belongs to employee. If such type of activity found, your outstanding payment will be forfeited and you will be declared blacklisted.", true);
        yield return ("If our any employee makes any type of undue demands in cash or kind for any reason, you can inform us on mail id grievances@champalalgroup.com immediately.", true);
        yield return ("8. All disputes are subject To Ahmedabad Jurisdiction.", true);
    }

    private string CurrencyLabel()
        => string.IsNullOrWhiteSpace(_model.Currency) ? "Rs" : _model.Currency.Trim();

    private static string FirstNonEmptyNote(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return "";
    }

    private static string FormatMoney(decimal value)
        => value.ToString("N2", MoneyCulture);

    private static string FormatAmountRaw(decimal value)
        => value.ToString("0.00", MoneyCulture);

    private static string FormatRate(decimal value)
        => value.ToString("0.000", MoneyCulture);

    private static string FormatDate(DateTime? value)
        => value.HasValue ? value.Value.ToString("dd-MM-yyyy") : "";

    private static string FormatDeliveryDateShort(DateTime? value)
        => value.HasValue ? value.Value.ToString("M/d/yyyy") : "";

    private static string CleanMultiline(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        return string.Join(" ", value
            .Replace("\r\n", " ")
            .Replace('\n', ' ')
            .Replace('\r', ' ')
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static byte[]? TryLoadLogo()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Assets", "hcp_logo.jpeg"),
            Path.Combine(Directory.GetCurrentDirectory(), "Assets", "hcp_logo.jpeg"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Assets", "hcp_logo.jpeg")),
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
                return File.ReadAllBytes(path);
        }

        return null;
    }
}
