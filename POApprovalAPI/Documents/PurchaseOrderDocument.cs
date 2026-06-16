using POApprovalAPI.Models;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace POApprovalAPI.Documents;

public class PurchaseOrderDocument : IDocument
{
    private readonly PurchaseOrderPdfModel _model;

    public PurchaseOrderDocument(PurchaseOrderPdfModel model)
    {
        _model = model;
    }

    public DocumentMetadata GetMetadata()
        => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Margin(20);

            page.Header()
                .Text($"PURCHASE ORDER - {_model.PoNo}")
                .Bold()
                .FontSize(18);

            page.Content()
                .Column(column =>
                {
                    column.Spacing(10);

                    column.Item().Text($"Company : {_model.CompanyName}");
                    column.Item().Text($"Vendor : {_model.VendorName}");

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3.5f); // Item description
                            columns.RelativeColumn(0.8f); // Unit
                            columns.RelativeColumn(1.2f); // Qty
                            columns.RelativeColumn(1.2f); // Rate
                            columns.RelativeColumn(1.0f); // Discount
                            columns.RelativeColumn(1.8f); // Amount
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Item");
                            header.Cell().Text("Unit");
                            header.Cell().Text("Qty");
                            header.Cell().Text("Rate");
                            header.Cell().Text("Discount");
                            header.Cell().Text("Amount");
                        });

                        foreach (var item in _model.Items)
                        {
                            table.Cell().Text(item.ItemDesc);
                            table.Cell().Text(item.Unit);
                            table.Cell().Text(item.Qty.ToString("N2"));
                            table.Cell().Text(item.Rate.ToString("N2"));
                            table.Cell().Text(item.Discount.ToString("N2"));
                            table.Cell().Text(item.Amount.ToString("N2"));
                        }
                    });

                    column.Item().Text($"CGST : {_model.CGSTAmount:N2}");
                    column.Item().Text($"SGST : {_model.SGSTAmount:N2}");
                    column.Item().Text($"IGST : {_model.IGSTAmount:N2}");

                    column.Item()
                        .Text($"Total Amount : {_model.TotalAmount:N2}")
                        .Bold();

                    column.Item()
                        .Text("Terms & Conditions")
                        .Bold();

                    column.Item().Text(_model.Note);
                });

            page.Footer()
                .AlignCenter()
                .Text("Auto Generated Purchase Order");
        });
    }
}