using POApprovalAPI.Models;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace POApprovalAPI.Documents;

/// <summary>
/// Work Order PDF uses the same layout as Purchase Order; title comes from the model.
/// Kept for compatibility — prefer PurchaseOrderDocument with DocumentTitle = "Work Order".
/// </summary>
public class WorkOrderDocument : IDocument
{
    private readonly PurchaseOrderDocument _inner;

    public WorkOrderDocument(PurchaseOrderPdfModel model)
    {
        if (string.IsNullOrWhiteSpace(model.DocumentTitle) ||
            model.DocumentTitle.Equals("Purchase Order", StringComparison.OrdinalIgnoreCase))
        {
            model.DocumentTitle = "Work Order";
        }

        _inner = new PurchaseOrderDocument(model);
    }

    public DocumentMetadata GetMetadata() => _inner.GetMetadata();

    public void Compose(IDocumentContainer container) => _inner.Compose(container);
}
