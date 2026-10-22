using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Models;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/woven-quote")]
public class WovenQuoteController : ControllerBase
{
    private readonly WovenQuoteService _service;

    public WovenQuoteController(WovenQuoteService service)
    {
        _service = service;
    }

    [HttpGet("masters")]
    public async Task<IActionResult> Masters() => Ok(await _service.GetMastersAsync());

    [HttpGet("rates")]
    public async Task<IActionResult> Rates([FromQuery] string? sector, [FromQuery] string? code) =>
        Ok(await _service.GetRatesAsync(sector, code));

    [HttpPost("rates")]
    public async Task<IActionResult> SaveRate([FromBody] WovenQuoteRateWrite row)
    {
        try
        {
            await _service.SaveRateAsync(row);
            return Ok(new { ok = true });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("constants")]
    public async Task<IActionResult> SaveConstants([FromBody] List<WovenQuoteConstantRow> rows)
    {
        await _service.SaveConstantsAsync(rows);
        return Ok(new { ok = true });
    }

    [HttpPost("customers")]
    public async Task<IActionResult> SaveCustomer([FromBody] WovenQuoteCustomerRow row)
    {
        try
        {
            var id = await _service.SaveCustomerAsync(row);
            return Ok(new { id });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("lookups")]
    public async Task<IActionResult> SaveLookup([FromBody] WovenQuoteLookupRow row)
    {
        try
        {
            await _service.SaveLookupAsync(row.Category, row.Value);
            return Ok(new { ok = true });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("preview")]
    public async Task<IActionResult> Preview([FromBody] WovenQuoteDraft draft) =>
        Ok(await _service.PreviewAsync(draft));

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] WovenQuoteListQuery query) =>
        Ok(await _service.ListAsync(query));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var row = await _service.GetAsync(id);
        return row is null ? NotFound(new { message = "Quote not found." }) : Ok(row);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] WovenQuoteSaveRequest request, [FromQuery] string? username, [FromQuery] bool draft = false)
    {
        if (request.Draft is null) return BadRequest(new { message = "Quote details are required." });
        try
        {
            var saved = await _service.SaveAsync(null, request.Draft, username, draft);
            return StatusCode(StatusCodes.Status201Created, saved);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] WovenQuoteSaveRequest request, [FromQuery] bool draft = false)
    {
        if (request.Draft is null) return BadRequest(new { message = "Quote details are required." });
        try
        {
            return Ok(await _service.SaveAsync(id, request.Draft, null, draft));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return Ok(new { ok = true });
    }
}
