using System.Globalization;
using POApprovalAPI.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace POApprovalAPI.Documents;

public sealed class ExportBillOverduePdfDocument : IDocument
{
    private static readonly CultureInfo In = CultureInfo.GetCultureInfo("en-IN");
    private const string Navy = "#0B3A5B";
    private const string Blue = "#1565A8";
    private const string Gold = "#C9A227";

    private readonly IReadOnlyList<ExportBillOverdueItemDto> _rows;
    private readonly string _companyLabel;
    private readonly string _groupName;
    private readonly DateTime _fromDate;
    private readonly DateTime _asOf;
    private readonly bool _showCompany;
    private readonly byte[]? _logoBytes;

    public ExportBillOverduePdfDocument(
        IReadOnlyList<ExportBillOverdueItemDto> rows,
        string companyLabel,
        string groupName,
        DateTime fromDate,
        DateTime asOf,
        bool showCompany)
    {
        _rows = rows;
        _companyLabel = companyLabel;
        _groupName = groupName;
        _fromDate = fromDate;
        _asOf = asOf;
        _showCompany = showCompany;
        _logoBytes = TryLoadLogo();
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.MarginHorizontal(16);
            page.MarginVertical(12);
            page.DefaultTextStyle(x => x.FontSize(8).FontFamily(Fonts.Arial).FontColor(Navy));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeTable);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        var pending = _rows.Sum(r => r.PendingAmount);
        container.Column(col =>
        {
            col.Item().Background(Navy).Padding(8).Row(row =>
            {
                row.ConstantItem(46).Height(36).Element(logo =>
                {
                    if (_logoBytes != null)
                        logo.Image(_logoBytes).FitArea();
                });
                row.RelativeItem().PaddingLeft(8).AlignMiddle().Column(c =>
                {
                    c.Item().Text("HCP Plastene Bulkpack Ltd").FontColor(Colors.White).Bold().FontSize(13);
                    c.Item().Text("Export Bill Overdue").FontColor(Colors.White).FontSize(10);
                });
                row.ConstantItem(180).AlignRight().AlignMiddle().Column(c =>
                {
                    c.Item().Text($"{_fromDate:dd-MMM-yyyy} to {_asOf:dd-MMM-yyyy}").FontColor(Colors.White).SemiBold();
                    c.Item().Text($"Generated {DateTime.Now:dd-MMM-yyyy HH:mm}").FontColor(Colors.White).FontSize(7);
                });
            });
            col.Item().Height(3).Background(Gold);
            col.Item().Background(Blue).PaddingHorizontal(10).PaddingVertical(6).Row(row =>
            {
                Kpi(row, "Company", _companyLabel);
                Kpi(row, "Group", _groupName);
                Kpi(row, "Bills", _rows.Count.ToString("N0", In));
                Kpi(row, "Pending (INR)", Money(pending));
            });
            col.Item().PaddingTop(8);
        });
    }

    private void ComposeTable(IContainer container)
    {
        container.Table(table =>
        {
            if (_showCompany)
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(2.2f);
                    c.RelativeColumn(2.6f);
                    c.RelativeColumn(1.4f);
                    c.RelativeColumn(1.2f);
                    c.RelativeColumn(1.4f);
                    c.RelativeColumn(1.2f);
                    c.RelativeColumn(0.9f);
                    c.RelativeColumn(1.4f);
                });
            }
            else
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(2.8f);
                    c.RelativeColumn(1.4f);
                    c.RelativeColumn(1.2f);
                    c.RelativeColumn(1.4f);
                    c.RelativeColumn(1.2f);
                    c.RelativeColumn(0.9f);
                    c.RelativeColumn(1.4f);
                });
            }

            table.Header(header =>
            {
                void Head(string text, bool right = false)
                {
                    var cell = header.Cell().Background(Navy).Padding(4);
                    if (right)
                        cell.AlignRight().Text(text).FontColor(Colors.White).SemiBold().FontSize(7.5f);
                    else
                        cell.Text(text).FontColor(Colors.White).SemiBold().FontSize(7.5f);
                }

                if (_showCompany) Head("Company");
                Head("Customer");
                Head("Bill no");
                Head("Bill date");
                Head("Amount (INR)", true);
                Head("Due date");
                Head("Days", true);
                Head("Pending (INR)", true);
            });

            var alt = false;
            foreach (var r in _rows)
            {
                var bg = alt ? "#F4F8FC" : "#FFFFFF";
                alt = !alt;
                void Cell(string text, bool right = false)
                {
                    var cell = table.Cell().Background(bg).BorderBottom(0.4f).BorderColor("#D7E3EE")
                        .PaddingVertical(3).PaddingHorizontal(4);
                    if (right)
                        cell.AlignRight().Text(text).FontSize(7.5f);
                    else
                        cell.Text(text).FontSize(7.5f);
                }

                if (_showCompany) Cell(r.CompanyName);
                Cell(string.IsNullOrWhiteSpace(r.CustomerName) ? r.LedgerName : r.CustomerName);
                Cell(r.BillNo);
                Cell(FormatDate(r.BillDate));
                Cell(Money(r.BillAmount), true);
                Cell(FormatDate(r.DueDate));
                Cell(r.OverdueDays.ToString("N0", In), true);
                Cell(Money(r.PendingAmount), true);
            }
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text("Confidential · Overseas receivables (Opening + Debit − Credit)").FontSize(7).FontColor("#5B6B7C");
            row.RelativeItem().AlignRight().Text(text =>
            {
                text.Span("Page ").FontSize(7).FontColor("#5B6B7C");
                text.CurrentPageNumber().FontSize(7).FontColor("#5B6B7C");
                text.Span(" of ").FontSize(7).FontColor("#5B6B7C");
                text.TotalPages().FontSize(7).FontColor("#5B6B7C");
            });
        });
    }

    private static void Kpi(RowDescriptor row, string label, string value)
    {
        row.RelativeItem().Column(c =>
        {
            c.Item().Text(label.ToUpperInvariant()).FontColor("#D6E6F5").FontSize(6.5f);
            c.Item().Text(value).FontColor(Colors.White).SemiBold().FontSize(8.5f);
        });
    }

    private static string Money(double n) => n.ToString("#,##0.00", In);

    private static string FormatDate(string iso)
    {
        if (string.IsNullOrWhiteSpace(iso) || iso.StartsWith("1900-01-01", StringComparison.Ordinal))
            return "—";
        return DateTime.TryParse(iso, out var d) ? d.ToString("dd-MMM-yyyy") : iso;
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
