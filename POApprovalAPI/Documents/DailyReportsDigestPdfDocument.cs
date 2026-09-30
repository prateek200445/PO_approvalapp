using POApprovalAPI.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace POApprovalAPI.Documents;

public sealed class DailyReportsDigestPdfDocument : IDocument
{
    private const string Navy = "#0B3A5B";
    private const string Blue = "#1565A8";
    private const string Gold = "#C9A227";
    private const string Muted = "#5B6B7C";
    private const string Line = "#D5DEE8";

    private readonly DateTime _date;
    private readonly DateTime _cutoff;
    private readonly IReadOnlyList<DailyReportModel> _reports;
    private readonly DateTime _generatedAt;
    private readonly byte[]? _logoBytes;

    public DailyReportsDigestPdfDocument(
        DateTime date,
        DateTime cutoff,
        DateTime generatedAt,
        IReadOnlyList<DailyReportModel> reports)
    {
        _date = date.Date;
        _cutoff = cutoff;
        _generatedAt = generatedAt;
        _reports = reports;
        _logoBytes = TryLoadLogo();
    }

    private static string F(FormattableString value) => FormattableString.Invariant(value);

    public DocumentMetadata GetMetadata() => new()
    {
        Title = F($"Daily Reports {_date:dd-MMM-yyyy}"),
        Author = "PO Portal",
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.MarginHorizontal(28);
            page.MarginVertical(22);
            page.DefaultTextStyle(x => x.FontSize(9.5f).FontFamily(Fonts.Arial).FontColor(Navy));
            page.Header().Element(ComposeHeader);
            page.Content().PaddingTop(10).Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Background(Navy).Padding(8).Row(row =>
            {
                row.ConstantItem(46).Height(34).Element(logo =>
                {
                    if (_logoBytes != null)
                        logo.Image(_logoBytes).FitArea();
                });
                row.RelativeItem().PaddingLeft(8).AlignMiddle().Column(c =>
                {
                    c.Item().Text("HCP Plastene Bulkpack Ltd").FontColor(Colors.White).Bold().FontSize(13);
                    c.Item().Text("Daily Work Reports").FontColor(Colors.White).FontSize(10);
                });
                row.ConstantItem(170).AlignRight().AlignMiddle().Column(c =>
                {
                    c.Item().AlignRight().Text(F($"{_date:dddd, dd-MMM-yyyy}")).FontColor(Colors.White).SemiBold();
                    c.Item().AlignRight().Text(F($"Submitted till {_cutoff:hh:mm tt}")).FontColor(Colors.White).FontSize(7.5f);
                });
            });
            col.Item().Height(3).Background(Gold);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(col =>
        {
            col.Spacing(10);

            if (_reports.Count == 0)
            {
                col.Item().PaddingTop(30).AlignCenter()
                    .Text(F($"No daily reports were submitted on {_date:dd-MMM-yyyy} before {_cutoff:hh:mm tt}."))
                    .FontSize(11).FontColor(Muted);
                return;
            }

            col.Item().Element(ComposeSummary);

            for (var i = 0; i < _reports.Count; i++)
            {
                var report = _reports[i];
                var number = i + 1;
                col.Item().Element(c => ComposeReport(c, report, number));
            }
        });
    }

    private void ComposeSummary(IContainer container)
    {
        container.Border(0.6f).BorderColor(Line).Column(col =>
        {
            col.Item().Background(Blue).PaddingHorizontal(8).PaddingVertical(5)
                .Text($"{_reports.Count} report{(_reports.Count == 1 ? "" : "s")} submitted")
                .FontColor(Colors.White).SemiBold();

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(26);
                    c.RelativeColumn(3);
                    c.RelativeColumn(3);
                    c.ConstantColumn(70);
                });

                table.Header(h =>
                {
                    foreach (var title in new[] { "#", "Employee", "Department", "Submitted" })
                        h.Cell().Background("#EEF3F8").Padding(4).Text(title).SemiBold().FontSize(8.5f);
                });

                for (var i = 0; i < _reports.Count; i++)
                {
                    var r = _reports[i];
                    table.Cell().BorderTop(0.4f).BorderColor(Line).Padding(4).Text($"{i + 1}").FontSize(8.5f);
                    table.Cell().BorderTop(0.4f).BorderColor(Line).Padding(4).Text(r.EmployeeName).FontSize(8.5f);
                    table.Cell().BorderTop(0.4f).BorderColor(Line).Padding(4).Text(Dash(r.Department)).FontSize(8.5f);
                    table.Cell().BorderTop(0.4f).BorderColor(Line).Padding(4).Text(F($"{r.SubmittedOn:hh:mm tt}")).FontSize(8.5f);
                }
            });
        });
    }

    private static void ComposeReport(IContainer container, DailyReportModel report, int number)
    {
        container.Border(0.6f).BorderColor(Line).Column(col =>
        {
            col.Item().Background("#EEF3F8").PaddingHorizontal(8).PaddingVertical(5).Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.Span($"{number}. {report.EmployeeName}").Bold().FontSize(10.5f);
                    if (!string.IsNullOrWhiteSpace(report.Department))
                        text.Span($"  ·  {report.Department}").FontColor(Muted);
                });
                row.ConstantItem(150).AlignRight().AlignMiddle()
                    .Text(F($"For {report.SubmittedForDate:dd-MMM} · sent {report.SubmittedOn:hh:mm tt}"))
                    .FontSize(8).FontColor(Muted);
            });

            col.Item().Padding(8).Column(body =>
            {
                body.Spacing(6);
                Section(body, "First half", report.FirstHalf);
                Section(body, "Second half", report.SecondHalf);

                body.Item().Column(tasks =>
                {
                    tasks.Item().Text("To do tomorrow").SemiBold().FontColor(Blue).FontSize(9);
                    if (report.TomorrowTasks.Count == 0)
                    {
                        tasks.Item().Text("—").FontColor(Muted);
                    }
                    else
                    {
                        foreach (var task in report.TomorrowTasks)
                            tasks.Item().PaddingLeft(6).Text($"•  {task}");
                    }
                });
            });
        });
    }

    private static void Section(ColumnDescriptor body, string title, string? content)
    {
        body.Item().Column(c =>
        {
            c.Item().Text(title).SemiBold().FontColor(Blue).FontSize(9);
            c.Item().Text(Dash(content));
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.PaddingTop(6).Row(row =>
        {
            row.RelativeItem().Text(F($"Generated {_generatedAt:dd-MMM-yyyy hh:mm tt} · PO Portal")).FontSize(7).FontColor(Muted);
            row.ConstantItem(90).AlignRight().Text(text =>
            {
                text.Span("Page ").FontSize(7).FontColor(Muted);
                text.CurrentPageNumber().FontSize(7).FontColor(Muted);
                text.Span(" of ").FontSize(7).FontColor(Muted);
                text.TotalPages().FontSize(7).FontColor(Muted);
            });
        });
    }

    private static string Dash(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();

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
