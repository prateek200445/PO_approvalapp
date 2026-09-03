using System.Globalization;
using POApprovalAPI.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace POApprovalAPI.Documents;

public sealed class BankRequirementsPdfDocument : IDocument
{
    private static readonly CultureInfo In = CultureInfo.GetCultureInfo("en-IN");
    private const string Navy = "#0B3A5B";
    private const string Blue = "#1565A8";
    private const string Gold = "#C9A227";

    private readonly BankRequirementsSalesProfileDto _data;
    private readonly byte[]? _logoBytes;

    public BankRequirementsPdfDocument(BankRequirementsSalesProfileDto data)
    {
        _data = data;
        _logoBytes = TryLoadLogo();
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(28);
            page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial).FontColor(Navy));
            page.Header().Element(ComposeHeader);
            page.Content().PaddingTop(16).Element(ComposeTable);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Background(Navy).Padding(10).Row(row =>
            {
                row.ConstantItem(46).Height(36).Element(logo =>
                {
                    if (_logoBytes != null)
                        logo.Image(_logoBytes).FitArea();
                });
                row.RelativeItem().PaddingLeft(10).AlignMiddle().Column(c =>
                {
                    c.Item().Text("HCP Plastene Bulkpack Ltd").FontColor(Colors.White).Bold().FontSize(13);
                    c.Item().Text("Bank Requirements — Profile of Sales").FontColor(Colors.White).FontSize(10);
                });
                row.ConstantItem(170).AlignRight().AlignMiddle().Column(c =>
                {
                    c.Item().Text(_data.PeriodLabel).FontColor(Colors.White).SemiBold();
                    c.Item().Text($"Generated {DateTime.Now:dd-MMM-yyyy HH:mm}").FontColor(Colors.White).FontSize(7);
                });
            });
            col.Item().Height(3).Background(Gold);
            col.Item().Background(Blue).PaddingHorizontal(10).PaddingVertical(8).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("COMPANY").FontColor("#D6E6F5").FontSize(7);
                    c.Item().Text(_data.Company).FontColor(Colors.White).SemiBold();
                });
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("PERIOD").FontColor("#D6E6F5").FontSize(7);
                    c.Item().Text($"{_data.DateFrom} to {_data.DateTo}").FontColor(Colors.White).SemiBold();
                });
            });
        });
    }

    private void ComposeTable(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Text("Profile of Sales (Details may be provided on approximate basis)")
                .Bold().FontSize(12);
            col.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(55);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(1.4f);
                    columns.RelativeColumn(1.2f);
                });

                table.Header(header =>
                {
                    header.Cell().RowSpan(2).Element(Head).AlignCenter().AlignMiddle().Text("Sr. No.").FontColor(Colors.White).Bold();
                    header.Cell().RowSpan(2).Element(Head).AlignCenter().AlignMiddle().Text("Revenue Streams").FontColor(Colors.White).Bold();
                    header.Cell().ColumnSpan(2).Element(Head).AlignCenter().Text(_data.PeriodLabel).FontColor(Colors.White).Bold();
                    header.Cell().Element(SubHead).AlignCenter().Text("Amt (INR Cr)").FontColor(Colors.White).SemiBold();
                    header.Cell().Element(SubHead).AlignCenter().Text("% Share").FontColor(Colors.White).SemiBold();
                });

                StreamRow(table, "1", "Export", _data.ExportAmountCr, _data.ExportShare);
                StreamRow(table, "2", "Domestic", _data.DomesticAmountCr, _data.DomesticShare);

                table.Cell().Element(TotalCell).AlignCenter().Text("");
                table.Cell().Element(TotalCell).Text("Total").FontColor(Colors.White).Bold();
                table.Cell().Element(TotalCell).AlignRight()
                    .Text(_data.TotalAmountCr.ToString("#,##0.00", In)).FontColor(Colors.White).Bold();
                table.Cell().Element(TotalCell).AlignRight()
                    .Text(_data.TotalAmount > 0 ? "100.00%" : "0.00%").FontColor(Colors.White).Bold();
            });

            col.Item().PaddingTop(12).Text(_data.Note).FontSize(8).FontColor("#64748B").Italic();
        });
    }

    private static void StreamRow(TableDescriptor table, string sr, string name, double mn, double share)
    {
        table.Cell().Element(Body).AlignCenter().Text(sr);
        table.Cell().Element(Body).Text(name);
        table.Cell().Element(Body).AlignRight().Text(mn.ToString("#,##0.00", In));
        table.Cell().Element(Body).AlignRight().Text(share.ToString("0.00", In) + "%");
    }

    private static IContainer Head(IContainer cell) =>
        cell.Background(Navy).Border(0.5f).BorderColor("#083049").Padding(6);

    private static IContainer SubHead(IContainer cell) =>
        cell.Background(Blue).Border(0.5f).BorderColor("#0B4F86").Padding(6);

    private static IContainer Body(IContainer cell) =>
        cell.Border(0.5f).BorderColor("#D6DEE8").Padding(7).AlignMiddle();

    private static IContainer TotalCell(IContainer cell) =>
        cell.Background(Navy).Border(0.5f).BorderColor("#083049").Padding(7).AlignMiddle();

    private static void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text("Amounts in INR Crore from country-wise sales (India vs export).")
                .FontSize(7).FontColor("#64748B");
            row.ConstantItem(80).AlignRight().Text(text =>
            {
                text.Span("Page ").FontSize(7);
                text.CurrentPageNumber().FontSize(7);
            });
        });
    }

    private static byte[]? TryLoadLogo()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Assets", "hcp_logo.jpeg"),
            Path.Combine(Directory.GetCurrentDirectory(), "Assets", "hcp_logo.jpeg"),
        };
        foreach (var path in candidates)
        {
            if (File.Exists(path))
                return File.ReadAllBytes(path);
        }
        return null;
    }
}
