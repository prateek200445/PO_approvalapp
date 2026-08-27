using Dapper;
using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Services;
using POApprovalAPI.Documents;
using POApprovalAPI.Models;
using QuestPDF.Fluent;
using System.Data;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PdfController : ControllerBase
{
    private static readonly Dictionary<string, string> FormerlyKnownAsByCompany =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["HCP Plastene Bulkpack Ltd"] = "Gopala Polyplast Limited",
        };

    private readonly DatabaseService _database;
    private readonly BomService _bomService;
    private readonly BomCreationService _creationService;

    public PdfController(DatabaseService database, BomService bomService, BomCreationService creationService)
    {
        _database = database;
        _bomService = bomService;
        _creationService = creationService;
    }

    [HttpGet("bom")]
    public async Task<IActionResult> GetBomPdf([FromQuery] string filePoNo)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePoNo))
                return BadRequest(new { message = "Quotation number is required." });

            var model = await _bomService.BuildPdfModelAsync(filePoNo);
            if (model is null)
                return NotFound(new { message = "BOM not found for this quotation number." });

            var pdfBytes = new BillOfMaterialDocument(model).GeneratePdf();
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

    [HttpGet("bom-preview/{previewId}")]
    public async Task<IActionResult> GetBomPreviewPdf(string previewId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(previewId))
                return BadRequest(new { message = "Preview id is required." });

            var model = await _creationService.GetPreviewPdfModelAsync(previewId);
            if (model is null)
                return NotFound(new { message = "Preview PDF expired or was not found. Run preview again." });

            var pdfBytes = new BillOfMaterialDocument(model).GeneratePdf();
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
            var model = await BuildOrderPdfModelAsync(connection, poNo, "Purchase Order");
            if (model == null)
                return NotFound($"PO not found: {poNo}");

            var pdfBytes = new PurchaseOrderDocument(model).GeneratePdf();
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

    [HttpGet("workorder")]
    public async Task<IActionResult> GetWorkOrderPdf([FromQuery] string poNo)
    {
        try
        {
            using var connection = _database.CreateConnection();
            // Same layout/mapping as PO; filtered by WO number (PurchaseCode)
            var model = await BuildOrderPdfModelAsync(connection, poNo, "Work Order");
            if (model == null)
                return NotFound($"Work Order not found: {poNo}");

            var pdfBytes = new PurchaseOrderDocument(model).GeneratePdf();
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

    private async Task<PurchaseOrderPdfModel?> BuildOrderPdfModelAsync(
        IDbConnection connection,
        string poNo,
        string documentTitle)
    {
        var header = await connection.QueryFirstOrDefaultAsync(
            new CommandDefinition(
                @"SELECT TOP 1
                    PurchaseCode,
                    CompanyName,
                    FirmName,
                    GST,
                    VendorGST,
                    CompanyGST,
                    Address,
                    Expr1,
                    HOAddress,
                    DispatchAdd,
                    ContactName,
                    ContactNo,
                    TelNo1,
                    Delivery,
                    DeliverySch,
                    Payment,
                    Mode,
                    PONote,
                    SpecialNote,
                    SpecialNote1,
                    PurchaseRefNo,
                    RefNo,
                    DepttName,
                    sysdate,
                    deliverydate,
                    IndentDate,
                    ApprovalDate,
                    Cr_FullName,
                    TotalAmount,
                    TDSAmt,
                    Currency
                  FROM Vw_PurchaseOrder
                  WHERE PurchaseCode = @poNo",
                new { poNo },
                commandTimeout: 60));

        if (header == null)
            return null;

        var items = (await connection.QueryAsync(
            new CommandDefinition(
                @"SELECT
                    ItemCode,
                    ItemDesc,
                    Unit,
                    Qty,
                    Rate,
                    Discount,
                    Total,
                    HSNCODE,
                    CGSTPer,
                    CGSTAmount,
                    SGSTPer,
                    SGSTAmount,
                    IGSTPer,
                    IGSTAmount
                  FROM FinalQuotation
                  WHERE PurchaseCode = @poNo
                  ORDER BY ItemCode",
                new { poNo },
                commandTimeout: 60))).ToList();

        var companyName = Convert.ToString(header.CompanyName) ?? "";

        // Store email for PDF term 3: PO creator's email from PurchasePayment.LoginName
        var storeEmail = await connection.QueryFirstOrDefaultAsync<string>(
            new CommandDefinition(
                @"SELECT TOP 1 l.email
                  FROM PurchasePayment P
                  INNER JOIN loginentry..loginrights l ON P.LoginName = l.name
                  WHERE P.PurchaseCode = @poNo",
                new { poNo },
                commandTimeout: 30)) ?? "";

        var model = new PurchaseOrderPdfModel
        {
            DocumentTitle = documentTitle,
            PoNo = Convert.ToString(header.PurchaseCode) ?? "",
            CompanyName = companyName,
            FormerlyKnownAs = FormerlyKnownAsByCompany.TryGetValue(companyName, out string? aka) ? aka ?? "" : "",
            VendorName = Convert.ToString(header.FirmName) ?? "",
            HoAddress = Convert.ToString(header.HOAddress) ?? "",
            CompanyAddress = Convert.ToString(header.Address) ?? "",
            VendorAddress = Convert.ToString(header.Expr1) ?? "",
            PostalAddress = !string.IsNullOrWhiteSpace(Convert.ToString(header.DispatchAdd))
                ? Convert.ToString(header.DispatchAdd)!
                : Convert.ToString(header.Address) ?? "",
            CompanyGst = FirstNonEmpty(header.CompanyGST, header.GST),
            VendorGst = Convert.ToString(header.VendorGST) ?? "",
            ContactName = (Convert.ToString(header.ContactName) ?? "").Trim(),
            ContactNo = FirstNonEmpty(header.ContactNo, header.TelNo1),
            StoreEmail = storeEmail.Trim(),
            Currency = (Convert.ToString(header.Currency) ?? "").Trim(),
            PoDate = header.sysdate as DateTime?,
            DeliveryDate = header.deliverydate as DateTime?,
            IndentDate = header.IndentDate as DateTime?,
            ApprovalDate = header.ApprovalDate as DateTime?,
            CreatedBy = Convert.ToString(header.Cr_FullName) ?? "",
            DepttName = Convert.ToString(header.DepttName) ?? "",
            IndentNo = Convert.ToString(header.RefNo) ?? "",
            PurchaseRefNo = Convert.ToString(header.PurchaseRefNo) ?? "",
            DeliveryTerms = (Convert.ToString(header.Delivery) ?? "").Trim(),
            DeliverySchedule = (Convert.ToString(header.DeliverySch) ?? "").Trim(),
            PaymentTerms = Convert.ToString(header.Payment) ?? "",
            ModeOfTransport = Convert.ToString(header.Mode) ?? "",
            Note = (Convert.ToString(header.PONote) ?? "").Trim(),
            SpecialNote = FirstNonEmpty(header.SpecialNote1, header.SpecialNote),
            TotalAmount = ToDecimal(header.TotalAmount),
            TdsAmount = ToDecimal(header.TDSAmt),
        };

        var sn = 1;
        foreach (var item in items)
        {
            var cgstPer = ToDecimal(item.CGSTPer);
            var sgstPer = ToDecimal(item.SGSTPer);
            var igstPer = ToDecimal(item.IGSTPer);
            var gstPer = igstPer > 0 ? igstPer : cgstPer + sgstPer;

            model.Items.Add(new PurchaseOrderItem
            {
                SerialNo = sn++,
                ItemCode = Convert.ToString(item.ItemCode) ?? "",
                ItemDesc = Convert.ToString(item.ItemDesc) ?? "",
                Unit = Convert.ToString(item.Unit) ?? "",
                HsnCode = Convert.ToString(item.HSNCODE) ?? "",
                Qty = ToDecimal(item.Qty),
                Rate = ToDecimal(item.Rate),
                Discount = ToDecimal(item.Discount),
                Amount = ToDecimal(item.Total),
                GstPer = gstPer,
                CGSTAmount = ToDecimal(item.CGSTAmount),
                SGSTAmount = ToDecimal(item.SGSTAmount),
                IGSTAmount = ToDecimal(item.IGSTAmount),
            });
        }

        model.SubTotal = model.Items.Sum(i => i.Amount);
        model.CGSTAmount = model.Items.Sum(i => i.CGSTAmount);
        model.SGSTAmount = model.Items.Sum(i => i.SGSTAmount);
        model.IGSTAmount = model.Items.Sum(i => i.IGSTAmount);

        if (model.TotalAmount == 0)
            model.TotalAmount = model.SubTotal + model.CGSTAmount + model.SGSTAmount + model.IGSTAmount;

        return model;
    }

    private static decimal ToDecimal(object? value)
    {
        if (value == null || value is DBNull) return 0m;
        return Convert.ToDecimal(value);
    }

    private static string FirstNonEmpty(params object?[] values)
    {
        foreach (var value in values)
        {
            var text = Convert.ToString(value)?.Trim();
            if (!string.IsNullOrWhiteSpace(text))
                return text;
        }

        return "";
    }
}
