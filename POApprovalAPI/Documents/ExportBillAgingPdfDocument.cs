using System.Globalization;
using POApprovalAPI.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace POApprovalAPI.Documents;

public sealed class ExportBillAgingPdfDocument : IDocument
{
    private static readonly CultureInfo In = CultureInfo.GetCultureInfo("en-IN");
    private const string Navy = "#0B3A5B";
    private const string Blue = "#1565A8";
    private const string Gold = "#C9A227";
    private static readonly string[] BucketColors = ["#16A34A", "#1565A8", "#D97706", "#C2410C", "#B91C1C"];

    private readonly ExportAgingReportDto _report;
    private readonly string _companyLabel;
    private readonly bool _showCompany;
    private readonly byte[]? _logoBytes;

    public ExportBillAgingPdfDocument(ExportAgingReportDto report, string companyLabel, bool showCompany)
    {
        _report = report;
        _companyLabel = companyLabel;
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
            page.Content().Column(col =>
            {
                col.Item().Element(ComposeBuckets);
                col.Item().PaddingTop(8).Element(ComposeTable);
            });
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        var asOf = DateTime.TryParse(_report.AsOf, out var d) ? d : DateTime.Today;
        var from = DateTime.TryParse(_report.DateFrom, out var f) ? f : asOf;
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
                    c.Item().Text("Export Bill Outstanding Aging").FontColor(Colors.White).FontSize(10);
                });
                row.ConstantItem(180).AlignRight().AlignMiddle().Column(c =>
                {
                    c.Item().Text($"{from:dd-MMM-yyyy} to {asOf:dd-MMM-yyyy}").FontColor(Colors.White).SemiBold();
                    c.Item().Text($"Generated {DateTime.Now:dd-MMM-yyyy HH:mm}").FontColor(Colors.White).FontSize(7);
                });
            });
            col.Item().Height(3).Background(Gold);
            col.Item().Background(Blue).PaddingHorizontal(10).PaddingVertical(6).Row(row =>
            {
                Kpi(row, "Company", _companyLabel);
                Kpi(row, "Group", _report.GroupName);
                Kpi(row, "Customers", _report.Customers.Count.ToString("N0", In));
                Kpi(row, "Pending (INR)", Money(_report.TotalPending));
            });
            col.Item().PaddingTop(8);
        });
    }

    private void ComposeBuckets(IContainer container)
    {
        container.Row(row =>
        {
            for (var i = 0; i < _report.Buckets.Count; i++)
            {
                var bucket = _report.Buckets[i];
                var color = BucketColors[Math.Min(i, BucketColors.Length - 1)];
                var share = _report.TotalPending > 0 ? bucket.PendingAmount / _report.TotalPending * 100 : 0;
                row.RelativeItem().PaddingRight(i == _report.Buckets.Count - 1 ? 0 : 6).Border(0.7f)
                    .BorderColor("#D7E3EE").Column(c =>
                    {
                        c.Item().Height(3).Background(color);
                        c.Item().Padding(6).Column(inner =>
                        {
                            inner.Item().Text(ExportBillOverdueService.AgingBucketHeading(bucket.Label)).FontSize(7).FontColor("#5B6B7C");
                            inner.Item().Text(Money(bucket.PendingAmount)).SemiBold().FontSize(9);
                            inner.Item().Text($"{bucket.BillCount:N0} bills · {share:0.0}%").FontSize(7).FontColor("#5B6B7C");
                        });
                    });
            }
        });
    }

    private void ComposeTable(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                if (_showCompany) c.RelativeColumn(2.1f);
                c.RelativeColumn(2.6f);
                foreach (var _ in _report.Buckets)
                    c.RelativeColumn(1.15f);
                c.RelativeColumn(1.3f);
            });

            table.Header(header =>
            {
                void Head(string text, bool right = false)
                {
                    var cell = header.Cell().Background(Navy).Padding(4);
                    if (right) cell = cell.AlignRight();
                    cell.Text(text).FontColor(Colors.White).SemiBold().FontSize(7.5f);
                }

                if (_showCompany) Head("Company");
                Head("Customer");
                foreach (var bucket in _report.Buckets)
                    Head(ExportBillOverdueService.AgingBucketHeading(bucket.Label), true);
                Head("Total (INR)", true);
            });

            var alt = false;
            foreach (var line in _report.Customers)
            {
                var bg = alt ? "#F4F8FC" : "#FFFFFF";
                alt = !alt;
                void Cell(string text, bool right = false, bool bold = false)
                {
                    var cell = table.Cell().Background(bg).BorderBottom(0.4f).BorderColor("#D7E3EE")
                        .PaddingVertical(3).PaddingHorizontal(4);
                    if (right) cell = cell.AlignRight();
                    var t = cell.Text(text).FontSize(7.5f);
                    if (bold) t.SemiBold();
                }

                if (_showCompany) Cell(line.CompanyName);
                Cell(line.CustomerName);
                for (var i = 0; i < _report.Buckets.Count; i++)
                {
                    var amt = i < line.Amounts.Length ? line.Amounts[i] : 0;
                    Cell(amt == 0 ? "—" : Money(amt), true);
                }
                Cell(Money(line.Total), true, true);
            }

            if (_report.Customers.Count > 0)
            {
                void TotalCell(string text, bool right = false)
                {
                    var cell = table.Cell().Background("#E8F1F8").Padding(4);
                    if (right) cell = cell.AlignRight();
                    cell.Text(text).SemiBold().FontSize(7.5f);
                }

                TotalCell(_showCompany ? "Total" : "Total");
                if (_showCompany) TotalCell("");
                foreach (var bucket in _report.Buckets)
                    TotalCell(Money(bucket.PendingAmount), true);
                TotalCell(Money(_report.TotalPending), true);
            }
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text("Confidential · Outstanding aging (Opening + Debit − Credit)").FontSize(7).FontColor("#5B6B7C");
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
