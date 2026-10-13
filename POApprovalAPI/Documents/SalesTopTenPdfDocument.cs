using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace POApprovalAPI.Documents;

public sealed class SalesTopTenPdfRequest
{
    public string Category { get; set; } = "Sales";
    public string Company { get; set; } = "All Companies";
    public string? DateFrom { get; set; }
    public string? DateTo { get; set; }
    public bool IncludeIntercompany { get; set; }
    public decimal? TotalAmount { get; set; }
    public List<SalesTopTenPdfSection> Sections { get; set; } = [];
}

public sealed class SalesTopTenPdfSection
{
    public string Title { get; set; } = "";
    public string? Subtitle { get; set; }
    public string NameHeader { get; set; } = "Name";
    public decimal? BaseAmount { get; set; }
    public List<SalesTopTenPdfRow> Rows { get; set; } = [];
}

public sealed class SalesTopTenPdfRow
{
    public int Rank { get; set; }
    public string Name { get; set; } = "";
    public string? Country { get; set; }
    public decimal Amount { get; set; }
}

public sealed class SalesTopTenPdfDocument : IDocument
{
    private static readonly CultureInfo In = CultureInfo.GetCultureInfo("en-IN");
    private const string Ink = "#0F172A";
    private const string Muted = "#64748B";
    private const string Line = "#E2E8F0";
    private const string Zebra = "#F8FAFC";

    private static readonly (string Bar, string Soft, string Text)[] Palette =
    [
        ("#2563EB", "#EFF6FF", "#1D4ED8"),
        ("#059669", "#ECFDF5", "#047857"),
        ("#D97706", "#FFFBEB", "#B45309"),
        ("#7C3AED", "#F5F3FF", "#6D28D9"),
    ];

    private readonly SalesTopTenPdfRequest _request;
    private readonly byte[]? _logo;
    private readonly DateTime _generatedAt = DateTime.Now;

    public SalesTopTenPdfDocument(SalesTopTenPdfRequest request)
    {
        _request = request;
        _logo = TryLoadLogo();
    }

    private bool IsPurchase => string.Equals(_request.Category, "Purchase", StringComparison.OrdinalIgnoreCase);
    private string ReportTitle => IsPurchase ? "Purchase — Top 10 Report" : "Sales — Top 10 Report";

    public DocumentMetadata GetMetadata() => new() { Title = ReportTitle, Author = "HCP Group" };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(24);
            page.DefaultTextStyle(x => x.FontSize(9).FontFamily(Fonts.Arial).FontColor(Ink));
            page.Header().Element(ComposeHeader);
            page.Content().PaddingTop(12).Element(ComposeBody);
            page.Footer().Row(row =>
            {
                row.RelativeItem().Text($"Generated {_generatedAt:dd-MMM-yyyy HH:mm} · Live ERP figures")
                    .FontSize(7).FontColor(Muted);
                row.RelativeItem().AlignRight().Text(t =>
                {
                    t.Span("Page ").FontSize(7).FontColor(Muted);
                    t.CurrentPageNumber().FontSize(7).FontColor(Muted);
                    t.Span(" of ").FontSize(7).FontColor(Muted);
                    t.TotalPages().FontSize(7).FontColor(Muted);
                });
            });
        });
    }

    private void ComposeHeader(IContainer container)
    {
        var accent = IsPurchase ? "#B45309" : "#1D4ED8";
        container.Column(col =>
        {
            col.Item().Background(accent).Padding(12).Row(row =>
            {
                row.ConstantItem(52).Height(42).Element(c =>
                {
                    if (_logo != null) c.Background(Colors.White).Padding(3).Image(_logo).FitArea();
                });
                row.RelativeItem().PaddingLeft(10).AlignMiddle().Column(c =>
                {
                    c.Item().Text(ReportTitle).FontColor(Colors.White).Bold().FontSize(15);
                    c.Item().Text(_request.Company).FontColor("#E0E7FF").FontSize(8.5f);
                });
                row.ConstantItem(170).AlignRight().AlignMiddle().Column(c =>
                {
                    c.Item().AlignRight().Text(PeriodLabel()).FontColor("#FDE68A").SemiBold().FontSize(10);
                    c.Item().AlignRight()
                        .Text(_request.IncludeIntercompany ? "Including intercompany" : "Excluding intercompany")
                        .FontColor(Colors.White).FontSize(8);
                });
            });
            col.Item().Height(3).Row(r =>
            {
                foreach (var p in Palette) r.RelativeItem().Background(p.Bar);
            });
        });
    }

    private void ComposeBody(IContainer container)
    {
        container.Column(col =>
        {
            col.Spacing(14);
            if (_request.TotalAmount is > 0)
            {
                col.Item().Background("#F1F5F9").Padding(8).Row(row =>
                {
                    row.RelativeItem().Text(IsPurchase ? "Total purchase for the period" : "Total sales for the period")
                        .FontColor(Muted);
                    row.AutoItem().Text($"Rs. {Money(_request.TotalAmount.Value)}").Bold().FontSize(11);
                });
            }

            var sections = _request.Sections.Where(s => s.Rows.Count > 0).ToList();
            if (sections.Count == 0)
            {
                col.Item().PaddingVertical(30).AlignCenter().Text("No data for the selected filters.").FontColor(Muted);
                return;
            }

            for (var i = 0; i < sections.Count; i++)
            {
                var section = sections[i];
                var colors = Palette[i % Palette.Length];
                col.Item().ShowEntire().Element(c => ComposeSection(c, section, colors));
            }
        });
    }

    private void ComposeSection(IContainer container, SalesTopTenPdfSection section, (string Bar, string Soft, string Text) colors)
    {
        var showCountry = section.Rows.Any(r => !string.IsNullOrWhiteSpace(r.Country));
        var sectionTotal = section.Rows.Sum(r => r.Amount);

        container.Border(1).BorderColor(Line).Column(col =>
        {
            col.Item().Background(colors.Soft).BorderLeft(4).BorderColor(colors.Bar).PaddingVertical(6).PaddingHorizontal(8).Column(h =>
            {
                h.Item().Text(section.Title).Bold().FontSize(11).FontColor(colors.Text);
                if (!string.IsNullOrWhiteSpace(section.Subtitle))
                    h.Item().Text(section.Subtitle).FontSize(7.5f).FontColor(Muted);
            });

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(28);
                    c.RelativeColumn(5);
                    if (showCountry) c.RelativeColumn(2);
                    c.RelativeColumn(2.2f);
                    c.ConstantColumn(80);
                });

                table.Header(header =>
                {
                    HeaderCell(header.Cell(), "#");
                    HeaderCell(header.Cell(), section.NameHeader);
                    if (showCountry) HeaderCell(header.Cell(), "Country");
                    HeaderCell(header.Cell(), "Amount (Rs.)", right: true);
                    HeaderCell(header.Cell(), "Share of top 10", right: true);
                });

                for (var r = 0; r < section.Rows.Count; r++)
                {
                    var row = section.Rows[r];
                    var bg = r % 2 == 1 ? Zebra : "#FFFFFF";
                    var share = sectionTotal > 0 ? row.Amount / sectionTotal * 100 : 0;
                    BodyCell(table.Cell(), bg).Text(row.Rank.ToString(In)).FontColor(Muted);
                    BodyCell(table.Cell(), bg).Text(row.Name).SemiBold();
                    if (showCountry) BodyCell(table.Cell(), bg).Text(row.Country ?? "—").FontColor(Muted);
                    BodyCell(table.Cell(), bg).AlignRight().Text(Money(row.Amount));
                    BodyCell(table.Cell(), bg).AlignRight().Text($"{share:0.0}%").FontColor(colors.Text);
                }

                var span = (uint)(showCountry ? 3 : 2);
                table.Cell().ColumnSpan(span).Background(colors.Soft).PaddingVertical(5).PaddingHorizontal(6)
                    .Text($"Total of top {section.Rows.Count}").Bold();
                table.Cell().Background(colors.Soft).PaddingVertical(5).PaddingHorizontal(6).AlignRight()
                    .Text(Money(sectionTotal)).Bold();
                table.Cell().Background(colors.Soft).PaddingVertical(5).PaddingHorizontal(6).AlignRight()
                    .Text(TotalShare(sectionTotal, section.BaseAmount)).Bold().FontColor(colors.Text);
            });
        });
    }

    private static string TotalShare(decimal sectionTotal, decimal? baseAmount) =>
        baseAmount is > 0 ? $"{sectionTotal / baseAmount.Value * 100:0.0}% of total" : "100%";

    private static void HeaderCell(IContainer cell, string text, bool right = false)
    {
        var c = cell.Background("#F1F5F9").BorderBottom(1).BorderColor(Line).PaddingVertical(5).PaddingHorizontal(6);
        (right ? c.AlignRight() : c).Text(text).FontSize(8).SemiBold().FontColor(Muted);
    }

    private static IContainer BodyCell(IContainer cell, string bg) =>
        cell.Background(bg).BorderBottom(1).BorderColor(Line).PaddingVertical(4).PaddingHorizontal(6);

    private string PeriodLabel()
    {
        static string Fmt(string? iso) =>
            DateTime.TryParse(iso, In, DateTimeStyles.None, out var d) ? d.ToString("dd-MMM-yyyy", In) : iso ?? "";
        return $"{Fmt(_request.DateFrom)} to {Fmt(_request.DateTo)}";
    }

    private static string Money(decimal v) => v.ToString("#,##,##0.00", In);

    private static byte[]? TryLoadLogo()
    {
        foreach (var path in new[]
                 {
                     Path.Combine(AppContext.BaseDirectory, "Assets", "hcp_logo.jpeg"),
                     Path.Combine(Directory.GetCurrentDirectory(), "Assets", "hcp_logo.jpeg"),
                 })
        {
            if (File.Exists(path)) return File.ReadAllBytes(path);
        }
        return null;
    }
}
