using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Documents;
using POApprovalAPI.Models;
using POApprovalAPI.Services;
using QuestPDF.Fluent;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/domestic-invoice")]
public class DomesticInvoiceController : ControllerBase
{
    private readonly DomesticInvoiceService _service;

    public DomesticInvoiceController(DomesticInvoiceService service)
    {
        _service = service;
    }

    [HttpGet("lookups")]
    public async Task<IActionResult> Lookups([FromQuery] string company, [FromQuery] string asOn, CancellationToken ct)
    {
        try { return Ok(await _service.LookupsAsync(company ?? "", asOn ?? "", ct)); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("invoices")]
    public async Task<IActionResult> Invoices([FromQuery] string company, [FromQuery] string suffix, [FromQuery] bool merchant, CancellationToken ct)
    {
        try { return Ok(await _service.InvoicesAsync(company, suffix, merchant, ct)); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("consignees")]
    public async Task<IActionResult> Consignees([FromQuery] string buyer, CancellationToken ct)
    {
        try { return Ok(await _service.ConsigneesAsync(buyer, ct)); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("commodity-tax")]
    public async Task<IActionResult> CommodityTax([FromQuery] string company, [FromQuery] string commodity, [FromQuery] string asOn, CancellationToken ct)
    {
        try { return Ok(await _service.CommodityTaxAsync(company, commodity, asOn, ct)); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("vendor-address")]
    public async Task<IActionResult> VendorAddress([FromQuery] string firm, CancellationToken ct)
    {
        try { return Ok(new { address = await _service.VendorAddressAsync(firm, ct) }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("insurance")]
    public async Task<IActionResult> Insurance([FromQuery] string company, CancellationToken ct)
    {
        try { return Ok(new { policyNo = await _service.InsurancePolicyAsync(company, ct) }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("user-company")]
    public async Task<IActionResult> UserCompany([FromQuery] string login, CancellationToken ct)
    {
        try { return Ok(await _service.UserCompanyAsync(login, ct)); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("buyer-tax")]
    public async Task<IActionResult> BuyerTax([FromQuery] string buyer, CancellationToken ct)
    {
        try { return Ok(await _service.BuyerTaxAsync(buyer, ct)); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("preview")]
    public async Task<IActionResult> Preview([FromBody] DomesticInvoiceDto invoice, CancellationToken ct)
    {
        try
        {
            List<Dictionary<string, string?>> rows = [];
            var copy = "ORIGINAL FOR RECIPIENT";
            if (invoice.InvNo > 0 && !string.IsNullOrWhiteSpace(invoice.CompanyName) && !string.IsNullOrWhiteSpace(invoice.Suffix))
            {
                var printed = await _service.PrintAsync(invoice.CompanyName, invoice.Suffix, invoice.InvNo, invoice.MerchantExport, false, "", ct);
                rows = printed.Rows;
                if (!string.IsNullOrWhiteSpace(printed.CopyTitle)) copy = printed.CopyTitle;
                invoice = printed.Invoice;
            }
            else
            {
                DomesticInvoiceMath.Calculate(invoice);
            }

            var pdf = new DomesticInvoicePdfDocument(invoice, rows, copy).GeneratePdf();
            var number = rows.Count > 0
                ? rows[0].FirstOrDefault(p => p.Key.Equals("GSTInvNo", StringComparison.OrdinalIgnoreCase)).Value
                : invoice.GstInvNo;
            var name = string.IsNullOrWhiteSpace(number) ? "domestic-invoice-preview" : number;
            return File(pdf, "application/pdf", name + ".pdf");
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("currency-value")]
    public async Task<IActionResult> CurrencyValue([FromQuery] string currency, CancellationToken ct)
    {
        try { return Ok(new { value = await _service.CurrencyValueAsync(currency, ct) }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("tcs")]
    public async Task<IActionResult> Tcs([FromQuery] string company, [FromQuery] string ledger, [FromQuery] string commodity, [FromQuery] string asOn, CancellationToken ct)
    {
        try { return Ok(new { rate = await _service.TcsRateAsync(company, ledger, commodity, asOn, ct) }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("{invNo:int}")]
    public async Task<IActionResult> Get(int invNo, [FromQuery] string company, [FromQuery] string suffix, [FromQuery] bool merchant, CancellationToken ct)
    {
        try
        {
            var invoice = await _service.GetAsync(company, suffix, invNo, merchant, ct);
            return invoice == null ? NotFound(new { message = "Invoice not found." }) : Ok(invoice);
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] DomesticInvoiceDto invoice, [FromQuery] string login, CancellationToken ct)
    {
        try { return Ok(await _service.SaveAsync(invoice, login ?? "", false, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("{invNo:int}")]
    public async Task<IActionResult> Update(int invNo, [FromBody] DomesticInvoiceDto invoice, [FromQuery] string login, CancellationToken ct)
    {
        try
        {
            invoice.InvNo = invNo;
            return Ok(await _service.SaveAsync(invoice, login ?? "", true, ct));
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("packing-lists")]
    public async Task<IActionResult> PackingLists([FromQuery] string company, CancellationToken ct)
    {
        try { return Ok(await _service.PendingPackingListsAsync(company, ct)); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("packing-lists/check")]
    public async Task<IActionResult> CheckPacking([FromBody] DomesticPackingCheck check, CancellationToken ct)
    {
        try
        {
            await _service.ValidatePackingAsync(check, ct);
            return Ok(new { ok = true, packingListNo = string.Join(",", check.PackingListNos) });
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("{invNo:int}/eway")]
    public async Task<IActionResult> Eway(int invNo, [FromQuery] string company, [FromQuery] string suffix, [FromBody] EwayBody body, CancellationToken ct)
    {
        try
        {
            await _service.UpdateEwayAsync(company, suffix, invNo, body.EwayNo ?? "", ct);
            return Ok(new { message = "E-way number updated." });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("{invNo:int}/print")]
    public async Task<IActionResult> Print(int invNo, [FromQuery] string company, [FromQuery] string suffix, [FromQuery] bool merchant, [FromQuery] bool record, [FromQuery] string login, CancellationToken ct)
    {
        try { return Ok(await _service.PrintAsync(company, suffix, invNo, merchant, record, login ?? "", ct)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    public class EwayBody
    {
        public string? EwayNo { get; set; }
    }
}
