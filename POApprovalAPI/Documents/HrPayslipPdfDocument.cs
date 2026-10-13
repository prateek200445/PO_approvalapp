using System.Globalization;
using POApprovalAPI.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace POApprovalAPI.Documents;

public sealed class HrPayslipPdfDocument : IDocument
{
    private static readonly CultureInfo In = CultureInfo.GetCultureInfo("en-IN");
    private const string Indigo = "#4338CA";
    private const string Fuchsia = "#C026D3";
    private const string Ink = "#1E1B4B";
    private const string Muted = "#6B7280";
    private const string EarnBg = "#ECFDF5";
    private const string EarnInk = "#047857";
    private const string DedBg = "#FEF2F2";
    private const string DedInk = "#B91C1C";

    private readonly HrPayslipDto _slip;
    private readonly byte[]? _photo;
    private readonly byte[]? _logo;

    public HrPayslipPdfDocument(HrPayslipDto slip, byte[]? photo)
    {
        _slip = slip;
        _photo = photo;
        _logo = TryLoadLogo();
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Salary slip {_slip.Employee.EmpCode} {_slip.PeriodLabel}",
        Author = _slip.Employee.Company ?? "HCP Group",
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(24);
            page.DefaultTextStyle(x => x.FontSize(9).FontFamily(Fonts.Arial).FontColor(Ink));
            page.Header().Element(ComposeHeader);
            page.Content().PaddingTop(10).Element(ComposeBody);
            page.Footer().AlignCenter().Text(t =>
            {
                t.Span("This is a computer-generated salary slip and does not require a signature. ").FontSize(7).FontColor(Muted);
                t.Span($"Generated {_slip.GeneratedAt:dd-MMM-yyyy HH:mm}").FontSize(7).FontColor(Muted);
            });
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Background(Indigo).Padding(12).Row(row =>
            {
                row.ConstantItem(52).Height(44).Element(c =>
                {
                    if (_logo != null) c.Background(Colors.White).Padding(3).Image(_logo).FitArea();
                });
                row.RelativeItem().PaddingLeft(10).AlignMiddle().Column(c =>
                {
                    c.Item().Text(_slip.Employee.Company ?? "HCP Group").FontColor(Colors.White).Bold().FontSize(14);
                    if (_slip.Employee.Branch is { } branch)
                        c.Item().Text(branch).FontColor("#E0E7FF").FontSize(8);
                });
                row.ConstantItem(150).AlignRight().AlignMiddle().Column(c =>
                {
                    c.Item().AlignRight().Text("SALARY SLIP").FontColor(Colors.White).Bold().FontSize(13).LetterSpacing(0.08f);
                    c.Item().AlignRight().Text(_slip.PeriodLabel).FontColor("#FDE68A").SemiBold().FontSize(10);
                });
            });
            col.Item().Height(4).Row(r =>
            {
                r.RelativeItem().Background(Fuchsia);
                r.RelativeItem().Background("#EC4899");
                r.RelativeItem().Background("#F59E0B");
            });
        });
    }

    private void ComposeBody(IContainer container)
    {
        var e = _slip.Employee;
        container.Column(col =>
        {
            col.Spacing(10);

            col.Item().Border(1).BorderColor("#E0E7FF").Background("#F8FAFF").Padding(10).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(e.Name).Bold().FontSize(14).FontColor(Indigo);
                    c.Item().PaddingBottom(6).Text($"{e.Designation ?? "—"}  ·  {e.Department ?? "—"}").FontColor(Muted);
                    c.Item().Row(r =>
                    {
                        r.RelativeItem().Column(k =>
                        {
                            Field(k, "Employee code", e.EmpCode);
                            Field(k, "Date of joining", e.DateOfJoining);
                            Field(k, "Pay type", _slip.PayType);
                            Field(k, "PAN", e.Pan);
                        });
                        r.RelativeItem().Column(k =>
                        {
                            Field(k, "PF account", e.PfAccount);
                            Field(k, "UAN", e.Uan);
                            Field(k, "ESIC no", e.Esic);
                            Field(k, "Bank", Join(e.BankName, e.BankAccountMasked));
                        });
                    });
                });
                row.ConstantItem(92).PaddingLeft(10).Column(c =>
                {
                    c.Item().Height(108).Border(2).BorderColor(Fuchsia).Background(Colors.White).AlignCenter().AlignMiddle().Element(p =>
                    {
                        if (_photo is { Length: > 0 })
                            p.Image(_photo).FitArea();
                        else
                            p.Text(Initials(e.Name)).FontSize(26).Bold().FontColor(Indigo);
                    });
                });
            });

            col.Item().Row(row =>
            {
                row.Spacing(6);
                Chip(row, "Days in month", Num(_slip.DaysInMonth), "#EEF2FF", Indigo);
                Chip(row, "Paid days", Num(_slip.PaidDays), "#ECFDF5", EarnInk);
                if (_slip.LeaveDays is > 0) Chip(row, "Leave", Num(_slip.LeaveDays.Value), "#EFF6FF", "#1D4ED8");
                if (_slip.OvertimeHours is > 0) Chip(row, "Overtime", Num(_slip.OvertimeHours.Value), "#FFF7ED", "#C2410C");
                if (_slip.RatePerDay is > 0) Chip(row, "Rate / day", Money(_slip.RatePerDay.Value), "#FDF4FF", Fuchsia);
            });

            col.Item().Row(row =>
            {
                row.Spacing(10);
                row.RelativeItem().Element(c => Lines(c, "Earnings", _slip.Earnings, "Gross earnings", _slip.GrossEarnings, EarnBg, EarnInk));
                row.RelativeItem().Element(c => Lines(c, "Deductions", _slip.Deductions, "Total deductions", _slip.TotalDeductions, DedBg, DedInk));
            });

            col.Item().Background(Indigo).Padding(12).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("NET PAY").FontColor("#C7D2FE").SemiBold().FontSize(9).LetterSpacing(0.1f);
                    c.Item().Text(_slip.NetPayInWords).FontColor(Colors.White).Italic().FontSize(8.5f);
                });
                row.ConstantItem(170).AlignRight().AlignMiddle().Text($"₹ {Money(_slip.NetPay)}").FontColor(Colors.White).Bold().FontSize(20);
            });

            if (_slip.Notes.Count > 0)
            {
                col.Item().Border(1).BorderColor("#FDE68A").Background("#FFFBEB").Padding(8).Column(c =>
                {
                    c.Item().Text("Other amounts recorded in payroll for this month (for reference)").SemiBold().FontColor("#92400E").FontSize(8);
                    foreach (var n in _slip.Notes)
                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Text(n.Label).FontSize(8);
                            r.ConstantItem(90).AlignRight().Text(Money(n.Amount)).FontSize(8);
                        });
                });
            }
        });
    }

    private static void Lines(IContainer container, string title, List<HrPayslipLineDto> lines, string totalLabel, decimal total, string bg, string ink)
    {
        container.Border(1).BorderColor(bg).Column(col =>
        {
            col.Item().Background(ink).PaddingVertical(5).PaddingHorizontal(8).Row(r =>
            {
                r.RelativeItem().Text(title).FontColor(Colors.White).Bold();
                r.ConstantItem(80).AlignRight().Text("Amount (₹)").FontColor(Colors.White).FontSize(8);
            });
            var i = 0;
            foreach (var l in lines)
            {
                col.Item().Background(i++ % 2 == 0 ? Colors.White : bg).PaddingVertical(4).PaddingHorizontal(8).Row(r =>
                {
                    r.RelativeItem().Text(l.Label);
                    r.ConstantItem(80).AlignRight().Text(Money(l.Amount));
                });
            }
            if (lines.Count == 0)
                col.Item().Padding(8).Text("—").FontColor(Muted);
            col.Item().Background(bg).BorderTop(1).BorderColor(ink).PaddingVertical(5).PaddingHorizontal(8).Row(r =>
            {
                r.RelativeItem().Text(totalLabel).Bold().FontColor(ink);
                r.ConstantItem(80).AlignRight().Text(Money(total)).Bold().FontColor(ink);
            });
        });
    }

    private static void Field(ColumnDescriptor col, string label, string? value) =>
        col.Item().PaddingBottom(3).Text(t =>
        {
            t.Span($"{label}: ").FontColor(Muted).FontSize(8);
            t.Span(string.IsNullOrWhiteSpace(value) ? "—" : value).SemiBold().FontSize(8.5f);
        });

    private static void Chip(RowDescriptor row, string label, string value, string bg, string ink) =>
        row.RelativeItem().Background(bg).Border(1).BorderColor(bg).PaddingVertical(5).PaddingHorizontal(8).Column(c =>
        {
            c.Item().Text(label).FontSize(7).FontColor(Muted);
            c.Item().Text(value).Bold().FontSize(11).FontColor(ink);
        });

    private static string Money(decimal v) => v.ToString("#,##,##0.00", In);
    private static string Num(decimal v) => v % 1 == 0 ? v.ToString("0", In) : v.ToString("0.##", In);
    private static string? Join(string? a, string? b) => string.Join(" · ", new[] { a, b }.Where(s => !string.IsNullOrWhiteSpace(s)));

    private static string Initials(string name) =>
        string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(p => char.ToUpperInvariant(p[0])));

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
