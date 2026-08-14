using System.Globalization;
using POApprovalAPI.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace POApprovalAPI.Documents;

public class BillOfMaterialDocument : IDocument
{
    private static readonly CultureInfo NumberCulture = CultureInfo.GetCultureInfo("en-US");
    private static readonly string AccentBlue = "#1D4ED8";

    private readonly BomPdfModel _model;
    private readonly byte[]? _logoBytes;

    public BillOfMaterialDocument(BomPdfModel model)
    {
        _model = model;
        _logoBytes = TryLoadLogo();
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.MarginHorizontal(24);
            page.MarginVertical(18);
            page.DefaultTextStyle(x => x.FontSize(8).FontFamily(Fonts.Arial));

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
                row.ConstantItem(52).Height(46).Element(logo =>
                {
                    if (_logoBytes != null)
                        logo.Image(_logoBytes).FitArea();
                });

                row.RelativeItem().PaddingHorizontal(8).Column(c =>
                {
                    c.Item().AlignCenter().Text("HCP Plastene Bulkpack Ltd").Bold().FontSize(13);
                    c.Item().AlignCenter()
                        .Text("(Formerly known as Gopala Polyplast Limited)")
                        .FontSize(7);
                    c.Item().PaddingTop(2).AlignCenter()
                        .Text("H. B.Jirawala House, 13, Navbharat Society, Usmanpura, Ahmedabad - 380013")
                        .FontSize(7);
                });

                row.ConstantItem(110).AlignRight().AlignMiddle().Column(c =>
                {
                    c.Item().Text(text =>
                    {
                        text.Span("Date: ").SemiBold();
                        text.Span(FormatDate(_model.Date));
                    });
                    if (!string.IsNullOrWhiteSpace(_model.User))
                    {
                        c.Item().Text(text =>
                        {
                            text.Span("User: ").SemiBold();
                            text.Span(_model.User);
                        });
                    }
                });
            });

            col.Item().PaddingTop(8).AlignCenter()
                .Text("BILL OF MATERIAL")
                .Bold()
                .FontSize(14)
                .FontColor(AccentBlue);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(8).Column(col =>
        {
            col.Item().Element(ComposeMetaSection);
            col.Item().PaddingTop(8).Element(ComposeSpecSection);
            col.Item().PaddingTop(10).Element(ComposeLinesTable);

            if (!string.IsNullOrWhiteSpace(_model.PrintingRemarks))
            {
                col.Item().PaddingTop(8).Text(text =>
                {
                    text.Span("Printing remarks: ").SemiBold();
                    text.Span(CleanText(_model.PrintingRemarks));
                });
            }

            if (!string.IsNullOrWhiteSpace(_model.BodyRemarks))
            {
                col.Item().PaddingTop(4).Text(text =>
                {
                    text.Span("Body remarks: ").SemiBold();
                    text.Span(CleanText(_model.BodyRemarks));
                });
            }

            if (!string.IsNullOrWhiteSpace(_model.Instruction))
            {
                col.Item().PaddingTop(8).Border(0.5f).Padding(6).Column(instructionCol =>
                {
                    instructionCol.Item().Text("Instructions").Bold().FontSize(9);
                    instructionCol.Item().PaddingTop(4).Text(CleanText(_model.Instruction)).FontSize(7.5f);
                });
            }
        });
    }

    private void ComposeMetaSection(IContainer container)
    {
        container.Border(0.5f).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(1.1f);
                columns.RelativeColumn(2.2f);
                columns.RelativeColumn(1.1f);
                columns.RelativeColumn(2.2f);
            });

            MetaRow(table, "Quotation No.", _model.QtnNo, "Party", _model.PartyName);
            MetaRow(table, "Ref No.", _model.RefNo, "PO No.", FirstNonEmpty(_model.PoNos, _model.PoNo));
            if (!string.IsNullOrWhiteSpace(_model.MarketingInvNo))
                MetaRow(table, "Marketing Inv.", _model.MarketingInvNo, "Total kg / bag", FormatNumber(_model.TotalKgPerBag, 4));
            else
                MetaRow(table, "Total kg / bag", FormatNumber(_model.TotalKgPerBag, 4), "Print type", _model.PrintType);
        });
    }

    private void ComposeSpecSection(IContainer container)
    {
        container.Border(0.5f).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(1.1f);
                columns.RelativeColumn(2.2f);
                columns.RelativeColumn(1.1f);
                columns.RelativeColumn(2.2f);
            });

            MetaRow(table, "Bag type", _model.BagType, "Dimensions (L×W×H)",
                $"{FormatNumber(_model.SizeL, 0)} × {FormatNumber(_model.SizeW, 0)} × {FormatNumber(_model.SizeH, 0)} {_model.SizeType}");
            MetaRow(table, "SWL", _model.Swl, "Qty",
                string.IsNullOrWhiteSpace(_model.QtyUnit)
                    ? _model.Qty
                    : $"{_model.Qty} {_model.QtyUnit}".Trim());
            MetaRow(table, "SF / L", FirstNonEmpty(_model.SfRatio, _model.Swl), "Fabric color", _model.FabColor);

            if (!string.IsNullOrWhiteSpace(_model.TopSpoutType))
                MetaRow(table, "Top / FS", _model.TopSpoutType, "Bottom / DS", _model.BottomType);
            if (!string.IsNullOrWhiteSpace(_model.LinerSpec))
                MetaRow(table, "Liner", _model.LinerSpec, "Loop", _model.LoopSpec);
            else if (!string.IsNullOrWhiteSpace(_model.LoopSpec))
                MetaRow(table, "Loop", _model.LoopSpec, "Doc pouch", _model.Doc);

            if (!string.IsNullOrWhiteSpace(_model.Doc1) && _model.Doc1 != "N/A")
                MetaRow(table, "Doc pouch 1", _model.Doc1, "Doc pouch 2", _model.Doc2 != "N/A" ? _model.Doc2 : "");
            else if (!string.IsNullOrWhiteSpace(_model.Doc) && _model.Doc != "N/A")
                MetaRow(table, "Doc pouch", _model.Doc, "Drop loop", _model.IsDropLoop);

            if (!string.IsNullOrWhiteSpace(_model.RpFabric))
                MetaRow(table, "RP fabric", _model.RpFabric, "Knot type", _model.KnotType);
        });
    }

    private void ComposeLinesTable(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Background(Colors.Grey.Lighten3).Padding(4)
                .Text("Component breakdown")
                .Bold()
                .FontSize(9);

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.6f);
                    columns.RelativeColumn(0.9f);
                    columns.RelativeColumn(0.9f);
                    columns.RelativeColumn(0.9f);
                    columns.RelativeColumn(0.8f);
                    columns.RelativeColumn(0.8f);
                    columns.RelativeColumn(0.9f);
                    columns.RelativeColumn(0.8f);
                    columns.RelativeColumn(1.8f);
                });

                table.Header(header =>
                {
                    HeaderCell(header, "Component");
                    HeaderCell(header, "GSM");
                    HeaderCell(header, "Lami");
                    HeaderCell(header, "Color");
                    HeaderCell(header, "Fabric");
                    HeaderCell(header, "Cut size");
                    HeaderCell(header, "Total mtr");
                    HeaderCell(header, "Kg");
                    HeaderCell(header, "Remarks");
                });

                foreach (var line in _model.Lines)
                {
                    BodyCell(table, line.Heading, bold: true);
                    BodyCell(table, line.Gsm);
                    BodyCell(table, line.Lami);
                    BodyCell(table, line.Color);
                    BodyCell(table, line.FabricSize);
                    BodyCell(table, line.CutSize);
                    BodyCell(table, FormatNumber(line.TotalMtr, 2), alignRight: true);
                    BodyCell(table, FormatNumber(line.TotalKg, 4), alignRight: true);
                    BodyCell(table, line.Remarks);
                }
            });

            col.Item().PaddingTop(4).AlignRight().Text(text =>
            {
                text.Span("Total kg / bag: ").SemiBold();
                text.Span(FormatNumber(_model.TotalKgPerBag, 4));
            });
        });
    }

    private static void MetaRow(TableDescriptor table, string label1, string value1, string label2, string value2)
    {
        LabelCell(table, label1);
        ValueCell(table, value1);
        LabelCell(table, label2);
        ValueCell(table, value2);
    }

    private static void LabelCell(TableDescriptor table, string text)
    {
        table.Cell().Border(0.4f).Background(Colors.Grey.Lighten4).Padding(3)
            .Text(text).SemiBold().FontSize(7.5f);
    }

    private static void ValueCell(TableDescriptor table, string text)
    {
        table.Cell().Border(0.4f).Padding(3).Text(CleanText(text)).FontSize(7.5f);
    }

    private static void HeaderCell(TableCellDescriptor cell, string text)
    {
        cell.Cell().Background(AccentBlue).Padding(3)
            .DefaultTextStyle(x => x.FontColor(Colors.White).SemiBold().FontSize(7.5f))
            .Text(text);
    }

    private static void BodyCell(TableDescriptor table, string text, bool bold = false, bool alignRight = false)
    {
        var cell = table.Cell().BorderBottom(0.3f).BorderColor(Colors.Grey.Lighten2).Padding(3);
        if (alignRight)
            cell = cell.AlignRight();

        cell.Text(textBlock =>
        {
            var span = textBlock.Span(CleanText(text)).FontSize(7.5f);
            if (bold)
                span.SemiBold();
        });
    }

    private static string FormatDate(DateTime? value) =>
        value?.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture) ?? "—";

    private static string FormatNumber(double? value, int decimals) =>
        value.HasValue ? value.Value.ToString($"N{decimals}", NumberCulture) : "—";

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            var text = value?.Trim();
            if (!string.IsNullOrWhiteSpace(text))
                return text;
        }

        return "—";
    }

    private static string CleanText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "—";

        return value
            .Replace("<>", "")
            .Replace("</b>", "")
            .Replace("<b>", "")
            .Replace("\r\n", "\n")
            .Trim();
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
