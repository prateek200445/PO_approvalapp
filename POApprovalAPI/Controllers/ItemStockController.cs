using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Models;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/item-stock")]
public class ItemStockController : ControllerBase
{
    private readonly ItemStockService _service;

    public ItemStockController(ItemStockService service)
    {
        _service = service;
    }

    [HttpGet("companies")]
    public async Task<IActionResult> Companies(CancellationToken ct)
    {
        try
        {
            return Ok(await _service.GetCompaniesAsync(ct));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("query")]
    public async Task<IActionResult> Query([FromBody] ItemStockQueryRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.QueryAsync(request, ct));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
