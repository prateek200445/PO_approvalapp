using Dapper;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

/// <summary>
/// Bill Payment Entry Approval queue — same source as ERP
/// FrmBillPaymentEntry.AllocateBillEntryApproval / FrmBillPaymentEntryApprovalAC:
/// dbo.BillPaymentEntryApproval assigned to the current user.
/// Payment Approval (HOD) stays on PaymentService + BillPaymentHODApproval.
/// </summary>
public class AdvancePaymentService
{
    private readonly DatabaseService _database;
    private readonly EmailService _emailService;

    public AdvancePaymentService(DatabaseService database, EmailService emailService)
    {
        _database = database;
        _emailService = emailService;
    }

    public const int MaxBulkSize = PaymentService.MaxBulkSize;
    private const int BulkParallelism = 8;

    private const string PendingApprovalFilter = @"
    a.status = 'Pending'
    AND a.ApprovalName = @UserName
    AND ISNULL(a.IsCancel, 'no') IN ('no', 'No', '0', '')";

    private const string EntrySelect = @"
    a.PaymentNo,
    ISNULL(a.CompanyName, '') AS CompanyName,
    ISNULL(a.VendorName, '') AS VendorName,
    ISNULL(a.BillNo, '') AS BillNo,
    ISNULL(a.MRNno, '') AS MRNNo,
    a.BillDate,
    a.MRNDate,
    a.PaymentDate,
    ISNULL(a.BillAmount, 0) AS BillAmount,
    ISNULL(a.PaymentAmount, 0) AS PaymentAmount,
    ISNULL(a.PaymentTerms, '') AS PaymentTerms,
    ISNULL(a.PriorityLevel, '') AS PriorityLevel,
    ISNULL(a.Currency, '') AS Currency,
    ISNULL(a.CurrencyRate, 0) AS CurrencyRate,
    ISNULL(a.TDS, 0) AS TDS,
    ISNULL(a.DebitNoteAmnt, 0) AS DebitNoteAmnt,
    ISNULL(a.Outstanding, 0) AS Outstanding,
    ISNULL(a.LedgerOSTAmt, 0) AS LedgerOSTAmt,
    ISNULL(a.Remarks, '') AS Remarks,
    ISNULL(a.Loginname, '') AS RequestedBy";

    public async Task<IEnumerable<PaymentRequestModel>> GetPendingPayments(
        string username,
        decimal? amount = null,
        string? filterType = null)
    {
        using var connection = _database.CreateConnection();

        var sql = $@"
SELECT
    {EntrySelect}
FROM BillPaymentEntryApproval a
WHERE {PendingApprovalFilter}";

        if (amount != null)
        {
            sql += filterType switch
            {
                "lt" => " AND a.PaymentAmount < @Amount",
                "gt" => " AND a.PaymentAmount > @Amount",
                "eq" => " AND a.PaymentAmount = @Amount",
                "lte" => " AND a.PaymentAmount <= @Amount",
                "gte" => " AND a.PaymentAmount >= @Amount",
                _ => ""
            };
        }

        sql += " ORDER BY a.PaymentDate DESC, a.Sysdate DESC, a.PaymentNo DESC";

        return await connection.QueryAsync<PaymentRequestModel>(
            sql,
            new { UserName = username, Amount = amount });
    }

    public async Task<PaymentDetailsModel?> GetPaymentDetails(string paymentNo)
    {
        using var connection = _database.CreateConnection();

        const string sql = @"
SELECT TOP (1)
    a.PaymentNo,
    ISNULL(a.CompanyName, '') AS CompanyName,
    ISNULL(a.VendorName, '') AS VendorName,
    ISNULL(a.BillNo, '') AS BillNo,
    a.BillDate,
    ISNULL(a.MRNno, '') AS MRNNo,
    a.MRNDate,
    ISNULL(a.BillAmount, 0) AS BillAmount,
    ISNULL(a.PaymentTerms, '') AS PaymentTerms,
    ISNULL(a.PaymentAmount, 0) AS PaymentAmount,
    a.PaymentDate,
    ISNULL(a.Loginname, '') AS RequestedBy,
    ISNULL(a.Remarks, '') AS Remarks,
    ISNULL(a.PriorityLevel, '') AS PriorityLevel,
    ISNULL(a.LC, '') AS LC,
    ISNULL(a.UTRno, '') AS UTRNo,
    ISNULL(a.Currency, '') AS Currency,
    ISNULL(a.CurrencyRate, 0) AS CurrencyRate,
    ISNULL(a.TDS, 0) AS TDS,
    ISNULL(a.DebitNoteAmnt, 0) AS DebitNoteAmnt,
    ISNULL(e.PaymentBankName, '') AS PaymentBankName,
    ISNULL(e.PaymentBankAccNo, '') AS PaymentBankAccNo,
    ISNULL(e.SpeReq, 0) AS SpeReq,
    ISNULL(a.Outstanding, 0) AS Outstanding,
    ISNULL(a.LedgerOSTAmt, 0) AS LedgerOSTAmt,
    CAST(ISNULL(a.LedgerOSTAmt, 0) AS DECIMAL(18,2)) AS LedgerBalance,
    CAST(0 AS DECIMAL(18,2)) AS GroupBalance
FROM BillPaymentEntryApproval a
LEFT JOIN BillPaymentEntry e
    ON e.VendorName = a.VendorName
   AND e.BillNo = a.BillNo
   AND ISNULL(e.MRNno, '') = ISNULL(a.MRNno, '')
   AND ISNULL(e.IsCancel, 'no') IN ('no', 'No', '0', '')
WHERE a.PaymentNo = @PaymentNo
ORDER BY CASE WHEN a.status = 'Pending' THEN 0 ELSE 1 END, a.Sysdate DESC";

        return await connection.QueryFirstOrDefaultAsync<PaymentDetailsModel>(
            sql,
            new { PaymentNo = paymentNo });
    }

    public async Task<IEnumerable<PaymentHistoryModel>> GetPaymentHistory(string paymentNo)
    {
        using var connection = _database.CreateConnection();

        const string sql = @"
SELECT
    ISNULL(ApprovalName, '') AS ApprovalName,
    ISNULL(status, '') AS Status,
    ApprovalDate,
    ISNULL(comment, '') AS Comment,
    ISNULL(Loginname, '') AS LoginName
FROM BillPaymentEntryApproval
WHERE PaymentNo = @PaymentNo
ORDER BY Sysdate, ApprovalDate";

        return await connection.QueryAsync<PaymentHistoryModel>(
            sql,
            new { PaymentNo = paymentNo });
    }

    public async Task<bool> ApprovePayment(PaymentApprovalRequest request)
    {
        return await SetApprovalStatus(request, "Approved");
    }

    public async Task<bool> RejectPayment(PaymentApprovalRequest request)
    {
        return await SetApprovalStatus(request, "Rejected");
    }

    public async Task<PaymentBulkApproveResponse> ApproveBulkAsync(PaymentBulkApproveRequest request)
    {
        var response = new PaymentBulkApproveResponse();

        if (request?.PaymentNos == null || request.PaymentNos.Count == 0)
            return response;

        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            response.Total = request.PaymentNos.Count;
            response.Failed.Add(new PaymentApproveItemResult
            {
                PaymentNo = null,
                Success = false,
                Reason = "UserName is required for bulk approve"
            });
            return response;
        }

        var distinctNos = request.PaymentNos
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        response.Total = distinctNos.Count;

        if (distinctNos.Count > MaxBulkSize)
        {
            response.Failed.Add(new PaymentApproveItemResult
            {
                PaymentNo = null,
                Success = false,
                Reason = $"Too many items. Maximum is {MaxBulkSize} per request."
            });
            return response;
        }

        var succeeded = new System.Collections.Concurrent.ConcurrentBag<PaymentApproveItemResult>();
        var failed = new System.Collections.Concurrent.ConcurrentBag<PaymentApproveItemResult>();
        var userName = request.UserName.Trim();

        await Parallel.ForEachAsync(
            distinctNos,
            new ParallelOptions { MaxDegreeOfParallelism = BulkParallelism },
            async (paymentNo, _) =>
            {
                try
                {
                    var ok = await ApprovePayment(new PaymentApprovalRequest
                    {
                        PaymentNo = paymentNo,
                        UserName = userName,
                        Comment = request.Comment ?? ""
                    });

                    if (ok)
                    {
                        succeeded.Add(new PaymentApproveItemResult
                        {
                            PaymentNo = paymentNo,
                            Success = true
                        });
                    }
                    else
                    {
                        failed.Add(new PaymentApproveItemResult
                        {
                            PaymentNo = paymentNo,
                            Success = false,
                            Reason = "Not pending or not assigned to this user"
                        });
                    }
                }
                catch (Exception ex)
                {
                    failed.Add(new PaymentApproveItemResult
                    {
                        PaymentNo = paymentNo,
                        Success = false,
                        Reason = ex.Message
                    });
                }
            });

        response.Succeeded = succeeded.OrderBy(x => x.PaymentNo).ToList();
        response.Failed = failed.OrderBy(x => x.PaymentNo).ToList();
        return response;
    }

    private async Task<bool> SetApprovalStatus(PaymentApprovalRequest request, string status)
    {
        using var connection = _database.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            var approvalData = await connection.QueryFirstOrDefaultAsync<ApprovalData>(
                $@"
SELECT
    a.PaymentNo,
    a.ApprovalName,
    lr.email AS Email
FROM BillPaymentEntryApproval a
LEFT JOIN loginentry..loginrights lr
    ON lr.NAME = a.Loginname OR lr.fullname = a.Loginname
WHERE a.PaymentNo = @PaymentNo
  AND {PendingApprovalFilter}",
                new { request.PaymentNo, UserName = request.UserName },
                transaction);

            if (approvalData == null)
            {
                transaction.Rollback();
                return false;
            }

            var updated = await connection.ExecuteAsync(
                @"
UPDATE BillPaymentEntryApproval
SET status = @Status,
    ApprovalDate = GETDATE(),
    comment = @Comment
WHERE PaymentNo = @PaymentNo
  AND ApprovalName = @UserName
  AND status = 'Pending'",
                new
                {
                    request.PaymentNo,
                    UserName = request.UserName,
                    Comment = request.Comment ?? "",
                    Status = status
                },
                transaction);

            if (updated == 0)
            {
                transaction.Rollback();
                return false;
            }

            if (!string.IsNullOrWhiteSpace(approvalData.Email))
            {
                await _emailService.SendMail(
                    approvalData.Email,
                    $"Bill Payment Entry {request.PaymentNo} {status}",
                    $"Dear Sir,\n\n" +
                    $"Payment No: {request.PaymentNo}\n" +
                    $"{status} By: {request.UserName}\n" +
                    $"Remarks: {request.Comment}\n\n" +
                    $"Regards,\n" +
                    $"{request.UserName}");
            }

            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
