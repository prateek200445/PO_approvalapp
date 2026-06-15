using Dapper;
using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Services;
using POApprovalAPI.Documents;
using POApprovalAPI.Models;
using QuestPDF.Fluent;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PdfController : ControllerBase
{
    private readonly DatabaseService _database;

    public PdfController(DatabaseService database)
    {
        _database = database;
    }

    [HttpGet("testdb")]
    public async Task<IActionResult> TestDb()
    {
        using var connection = _database.CreateConnection();

        var result = await connection.QueryFirstOrDefaultAsync(
            "SELECT DB_NAME() AS DatabaseName");

        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetPdf([FromQuery] string poNo)
    {
        try
        {
            using var connection = _database.CreateConnection();

            // Header Information
            var header = await connection.QueryFirstOrDefaultAsync(
                @"SELECT TOP 1 *
                  FROM PurchasePayment
                  WHERE PurchaseCode = @poNo",
                new { poNo });

            if (header == null)
                return NotFound($"PO not found: {poNo}");

            // All Line Items
            var items = await connection.QueryAsync(
                @"SELECT
                    PurchaseCode,
                    FirmName,
                    ItemDesc,
                    Unit,
                    Qty,
                    Rate,
                    Total,
                    Discount
                  FROM FinalQuotation
                  WHERE PurchaseCode = @poNo",
                new { poNo });

            // GST Summary
            var gst = await connection.QueryFirstOrDefaultAsync(
    @"SELECT
        SUM(ISNULL(CGSTAmount,0)) AS CGSTAmount,
        SUM(ISNULL(SGSTAmount,0)) AS SGSTAmount,
        SUM(ISNULL(IGSTAmount,0)) AS IGSTAmount
      FROM Vw_PurchaseOrder
      WHERE PurchaseCode = @poNo",
    new { poNo });

   var model = new PurchaseOrderPdfModel
{
    PoNo = header.PurchaseCode ?? "",
    CompanyName = header.CompanyName ?? "",
    VendorName = items.FirstOrDefault()?.FirmName ?? "",
    DeliveryTerms = header.Delivery ?? "",
    PaymentTerms = header.Payment ?? "",
    DispatchAddress = header.DispatchAdd ?? "",
    Note = header.PONote ?? "",
    TotalAmount = Convert.ToDecimal(header.TotalAmount),

    CGSTAmount = Convert.ToDecimal(gst?.CGSTAmount ?? 0),
    SGSTAmount = Convert.ToDecimal(gst?.SGSTAmount ?? 0),
    IGSTAmount = Convert.ToDecimal(gst?.IGSTAmount ?? 0)
};

foreach (var item in items)
{
    model.Items.Add(new PurchaseOrderItem
    {
        ItemDesc = item.ItemDesc ?? "",
        Unit = item.Unit ?? "",
        Qty = Convert.ToDecimal(item.Qty),
        Rate = Convert.ToDecimal(item.Rate),
        Discount = Convert.ToDecimal(item.Discount ?? 0),
        Amount = Convert.ToDecimal(item.Total)
    });
}       

var document = new PurchaseOrderDocument(model);

var pdfBytes = document.GeneratePdf();

Response.Headers["Content-Disposition"] = "inline";
return File(pdfBytes, "application/pdf");
        }
        catch (Exception ex)
{
    return BadRequest(new
    {
        Success = false,
        Error = ex.ToString()
    });
}
    }
}