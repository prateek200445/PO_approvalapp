using System.Globalization;
using POApprovalAPI.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace POApprovalAPI.Documents;

public sealed class IntercompanyBalancePdfDocument : IDocument
{
    private static readonly CultureInfo In = CultureInfo.GetCultureInfo("en-IN");
    private const string Navy = "#0B3A5B";
    private const string Blue = "#1565A8";
    private const string Gold = "#C9A227";
    private const double Crore = 10_000_000d;

    private readonly IntercompanyDashboardDto _data;
    private readonly DateTime _asOf;
    private readonly IntercompanyBalanceGrid _grid;
    private readonly byte[]? _logoBytes;

    public IntercompanyBalancePdfDocument(IntercompanyDashboardDto data, DateTime asOf)
    {
        _data = data;
        _asOf = asOf.Date;
        _grid = IntercompanyBalanceService.BuildBalanceGrid(data.Matrices);
        _logoBytes = TryLoadLogo();
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A3.Landscape());
            page.MarginHorizontal(16);
            page.MarginVertical(12);
            page.DefaultTextStyle(x => x.FontSize(7.5f).FontFamily(Fonts.Arial).FontColor(Navy));
            page.Header().Element(c => ComposeHeader(c, "Inter-Company Balance Matrix", "Amounts in ₹ Crore"));
            page.Content().Element(ComposeMatrix);
            page.Footer().Element(ComposeFooter);
        });

        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.MarginHorizontal(16);
            page.MarginVertical(12);
            page.DefaultTextStyle(x => x.FontSize(8).FontFamily(Fonts.Arial).FontColor(Navy));
            page.Header().Element(c => ComposeHeader(c, "Detailed Inter-Company Balances", "Amounts in INR"));
            page.Content().Element(ComposeDetails);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container, string title, string subtitle)
    {
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
                    c.Item().Text(title).FontColor(Colors.White).FontSize(10);
                });
                row.ConstantItem(200).AlignRight().AlignMiddle().Column(c =>
                {
                    c.Item().Text($"As on {_asOf:dd-MMM-yyyy}").FontColor(Colors.White).SemiBold();
                    c.Item().Text($"Generated {DateTime.Now:dd-MMM-yyyy HH:mm}").FontColor(Colors.White).FontSize(7);
                });
            });
            col.Item().Height(3).Background(Gold);
            col.Item().Background(Blue).PaddingHorizontal(10).PaddingVertical(6).Row(row =>
            {
                Kpi(row, "Report", subtitle);
                Kpi(row, "Companies", _grid.FromCompanies.Count.ToString("N0", In));
                Kpi(row, "Pairs", _data.Lines.Count.ToString("N0", In));
            });
            col.Item().PaddingTop(8);
        });
    }

    private void ComposeMatrix(IContainer container)
    {
        var columns = _grid.Columns;
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(2.4f);
                foreach (var _ in columns)
                    c.RelativeColumn(1);
                c.RelativeColumn(1.1f);
            });

            table.Header(header =>
            {
                HeaderCell(header.Cell(), "From \\ To");
                foreach (var name in columns)
                    HeaderCell(header.Cell(), name);
                HeaderCell(header.Cell(), "Total");
            });

            for (var r = 0; r < _grid.FromCompanies.Count; r++)
            {
                var from = _grid.FromCompanies[r];
                table.Cell().Element(NameCell).Text(from).SemiBold().FontSize(6.5f);

                for (var c = 0; c < columns.Count; c++)
                {
                    var to = columns[c];
                    var diagonal = IntercompanyBalanceService.CompanyAlignKey(from)
                        == IntercompanyBalanceService.CompanyAlignKey(to);
                    var value = !_grid.Cells.TryGetValue(from, out var row)
                        ? 0d
                        : row.TryGetValue(to, out var v) ? v : 0d;
                    AmountCell(table.Cell(), diagonal ? 0 : value, diagonal, crore: true);
                }

                AmountCell(table.Cell(), _grid.RowTotals[r], empty: false, crore: true, total: true);
            }

            table.Cell().Element(TotalNameCell).Text("Total").FontColor(Colors.White).SemiBold();
            for (var c = 0; c < _grid.ColTotals.Count; c++)
                AmountCell(table.Cell(), _grid.ColTotals[c], empty: false, crore: true, total: true);
            AmountCell(table.Cell(), _grid.GrandTotal, empty: false, crore: true, total: true);
        });
    }

    private void ComposeDetails(IContainer container)
    {
        var lines = _data.Lines
            .OrderBy(l => l.Company, StringComparer.OrdinalIgnoreCase)
            .ThenBy(l => l.Balance)
            .ToList();

        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(2.4f);
                c.RelativeColumn(2.4f);
                c.RelativeColumn(1.3f);
                c.RelativeColumn(1.2f);
            });

            table.Header(header =>
            {
                HeaderCell(header.Cell(), "Company");
                HeaderCell(header.Cell(), "Counterparty");
                HeaderCell(header.Cell(), "Balance (INR)");
                HeaderCell(header.Cell(), "Balance (₹ Cr.)");
            });

            foreach (var line in lines)
            {
                table.Cell().Element(BodyCell).Text(line.Company);
                table.Cell().Element(BodyCell).Text(line.Counterparty);
                AmountCell(table.Cell(), line.Balance, empty: false, crore: false);
                AmountCell(table.Cell(), line.BalanceCr, empty: false, crore: false);
            }

            if (lines.Count == 0)
            {
                table.Cell().ColumnSpan(4).Padding(12).AlignCenter()
                    .Text("No outstanding intercompany balances for this date.")
                    .FontColor("#5B6B7C");
            }
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text("Confidential · Intercompany balances from ERP").FontSize(7).FontColor("#5B6B7C");
            row.ConstantItem(90).AlignRight().Text(text =>
            {
                text.Span("Page ").FontSize(7).FontColor("#5B6B7C");
                text.CurrentPageNumber().FontSize(7).FontColor("#5B6B7C");
                text.Span(" of ").FontSize(7).FontColor("#5B6B7C");
                text.TotalPages().FontSize(7).FontColor("#5B6B7C");
            });
        });
    }

    private static void HeaderCell(IContainer cell, string text)
    {
        cell.Background(Navy).Border(0.4f).BorderColor("#083049").Padding(3).AlignCenter().AlignMiddle()
            .Text(text).FontColor(Colors.White).SemiBold().FontSize(6.5f);
    }

    private static IContainer NameCell(IContainer cell) =>
        cell.Background("#E8F1F8").Border(0.4f).BorderColor("#D6DEE8").Padding(3).AlignMiddle();

    private static IContainer TotalNameCell(IContainer cell) =>
        cell.Background(Navy).Border(0.4f).BorderColor("#083049").Padding(3).AlignMiddle();

    private static IContainer BodyCell(IContainer cell) =>
        cell.Border(0.4f).BorderColor("#D6DEE8").Padding(3).AlignMiddle();

    private static void AmountCell(IContainer cell, double inr, bool empty, bool crore, bool total = false)
    {
        var display = empty || Math.Abs(inr) < 0.005 ? "" : (crore ? inr / Crore : inr).ToString("#,##0.00", In);
        var bg = total ? Navy : empty ? "#F1F5F9" : Math.Abs(inr) < 0.005 ? "#FFFFFF" : "#E0F2FE";
        var color = total
            ? (inr < 0 ? "#FECACA" : Colors.White)
            : inr < 0 ? "#B91C1C" : Navy;

        cell.Background(bg).Border(0.4f).BorderColor(total ? "#083049" : "#D6DEE8").Padding(3)
            .AlignRight().AlignMiddle()
            .Text(display).FontColor(color).FontSize(7).FontFamily(Fonts.Arial);
    }

    private static void Kpi(RowDescriptor row, string label, string value)
    {
        row.RelativeItem().Column(c =>
        {
            c.Item().Text(label.ToUpperInvariant()).FontColor("#D6E6F5").FontSize(6.5f);
            c.Item().Text(value).FontColor(Colors.White).SemiBold().FontSize(8.5f);
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
