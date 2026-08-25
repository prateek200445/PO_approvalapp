using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Models;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdvancePaymentController : ControllerBase
{
    private readonly AdvancePaymentService _advancePaymentService;

    public AdvancePaymentController(AdvancePaymentService advancePaymentService)
    {
        _advancePaymentService = advancePaymentService;
    }

    [HttpGet("pending/{username}")]
    public async Task<IActionResult> GetPending(
        string username,
        [FromQuery] decimal? amount,
        [FromQuery] string? filterType)
    {
        try
        {
            var data = await _advancePaymentService.GetPendingPayments(username, amount, filterType);
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

        var data = await _advancePaymentService.GetPaymentDetails(paymentNo);

        if (data == null)
            return NotFound("Advance payment not found.");

        return Ok(data);
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] string paymentNo)
    {
        paymentNo = Uri.UnescapeDataString(paymentNo);
        var data = await _advancePaymentService.GetPaymentHistory(paymentNo);
        return Ok(data);
    }

    [HttpPost("approve")]
    public async Task<IActionResult> Approve([FromBody] PaymentApprovalRequest request)
    {
        try
        {
            var result = await _advancePaymentService.ApprovePayment(request);
            if (!result)
                return BadRequest("Approval failed.");

            return Ok("Advance payment approved successfully.");
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
            var result = await _advancePaymentService.RejectPayment(request);
            if (!result)
                return BadRequest("Rejection failed.");

            return Ok("Advance payment rejected successfully.");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("approve-bulk")]
    public async Task<IActionResult> ApproveBulk([FromBody] PaymentBulkApproveRequest request)
    {
        if (request == null)
            return BadRequest(new { message = "Request body is required" });

        if (string.IsNullOrWhiteSpace(request.UserName))
            return BadRequest(new { message = "UserName is required" });

        if (request.PaymentNos == null || request.PaymentNos.Count == 0)
            return BadRequest(new { message = "At least one PaymentNo is required" });

        if (request.PaymentNos.Count > AdvancePaymentService.MaxBulkSize)
        {
            return BadRequest(new
            {
                message = $"Maximum {AdvancePaymentService.MaxBulkSize} payments allowed per bulk approve"
            });
        }

        var result = await _advancePaymentService.ApproveBulkAsync(request);
        return Ok(result);
    }
}
