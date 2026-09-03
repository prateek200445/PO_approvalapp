using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

/// <summary>
/// Bill Payment Entry approval queue. Uses the same BillPaymentHODApproval /
/// BillPaymentEntry flow as Payment Approval.
/// </summary>
public class AdvancePaymentService
{
    private readonly PaymentService _payments;

    public AdvancePaymentService(PaymentService payments)
    {
        _payments = payments;
    }

    public const int MaxBulkSize = PaymentService.MaxBulkSize;

    public Task<IEnumerable<PaymentRequestModel>> GetPendingPayments(
        string username,
        decimal? amount = null,
        string? filterType = null) =>
        _payments.GetPendingPayments(username, amount, filterType);

    public Task<PaymentDetailsModel?> GetPaymentDetails(string paymentNo) =>
        _payments.GetPaymentDetails(paymentNo);

    public Task<IEnumerable<PaymentHistoryModel>> GetPaymentHistory(string paymentNo) =>
        _payments.GetPaymentHistory(paymentNo);

    public Task<bool> ApprovePayment(PaymentApprovalRequest request) =>
        _payments.ApprovePayment(request);

    public Task<bool> RejectPayment(PaymentApprovalRequest request) =>
        _payments.RejectPayment(request);

    public Task<PaymentBulkApproveResponse> ApproveBulkAsync(PaymentBulkApproveRequest request) =>
        _payments.ApproveBulkAsync(request);
}
