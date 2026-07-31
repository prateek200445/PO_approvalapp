using Dapper;
using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Models;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
   private readonly PaymentService _paymentService;

public PaymentController(PaymentService paymentService)
{
    _paymentService = paymentService;
}
 [HttpGet("pending/{username}")]
public async Task<IActionResult> GetPending(
    string username,
    [FromQuery] decimal? amount,
    [FromQuery] string? filterType)
{
    try
    {
       var data = await _paymentService.GetPendingPayments(
    username,
    amount,
    filterType);

        return Ok(data);
    }
    catch (Exception ex)
    {
        return BadRequest(ex.Message);
    }
}
[HttpGet("details")]
public async Task<IActionResult> GetDetails([FromQuery] string paymentNo)
{
    paymentNo = Uri.UnescapeDataString(paymentNo);

    var data = await _paymentService.GetPaymentDetails(paymentNo);

    if (data == null)
        return NotFound("Payment Request not found.");

    return Ok(data);
}
[HttpGet("history")]
public async Task<IActionResult> GetHistory([FromQuery] string paymentNo)
{
    paymentNo = Uri.UnescapeDataString(paymentNo);

    var data = await _paymentService.GetPaymentHistory(paymentNo);

    return Ok(data);
}
[HttpPost("approve")]
public async Task<IActionResult> Approve([FromBody] PaymentApprovalRequest request)
{
    try
    {
        var result = await _paymentService.ApprovePayment(request);

        if (!result)
            return BadRequest("Approval failed.");

        return Ok("Payment approved successfully.");
    }
        catch (Exception ex)
{
    return BadRequest(ex.ToString());
}
}
[HttpPost("reject")]
public async Task<IActionResult> Reject([FromBody] PaymentApprovalRequest request)
{
    try
    {
        var result = await _paymentService.RejectPayment(request);

        if (!result)
            return BadRequest("Rejection failed.");

        return Ok("Payment rejected successfully.");
    }
    catch (Exception ex)
    {
        return BadRequest(ex.Message);
    }
}

}