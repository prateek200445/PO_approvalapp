using Dapper;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

/// <summary>
/// Bill Payment Entry approval queue from dbo.BillPaymentEntry
/// (not BillPaymentHODApproval).
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

    private const string PendingEntryFilter = @"
    e.status = 'Pending'
    AND ISNULL(e.IsCancel, 'no') IN ('no', 'No', '0', '')
    AND ISNULL(e.isPaid, 'no') IN ('no', 'No', '0', '')
    AND (
            EXISTS (
                SELECT 1
                FROM BillPaymentApprovalAllocation a
                WHERE a.ApprovalName = @UserName
            )
         OR EXISTS (
                SELECT 1
                FROM BillPaymentHODApproval h
                WHERE h.ApprovalName = @UserName
            )
        )";

    public async Task<IEnumerable<PaymentRequestModel>> GetPendingPayments(
        string username,
        decimal? amount = null,
        string? filterType = null)
    {
        using var connection = _database.CreateConnection();

        var sql = $@"
SELECT TOP (20)
    e.PaymentNo,
    ISNULL(e.CompanyName, '') AS CompanyName,
    ISNULL(e.VendorName, '') AS VendorName,
    ISNULL(e.BillNo, '') AS BillNo,
    ISNULL(e.MRNno, '') AS MRNNo,
    e.BillDate,
    e.MRNDate,
    e.PaymentDate,
    ISNULL(e.BillAmount, 0) AS BillAmount,
    ISNULL(e.PaymentAmount, 0) AS PaymentAmount,
    ISNULL(e.PaymentTerms, '') AS PaymentTerms,
    ISNULL(e.PriorityLevel, '') AS PriorityLevel,
    ISNULL(e.Currency, '') AS Currency,
    ISNULL(e.CurrencyRate, 0) AS CurrencyRate,
    ISNULL(e.TDS, 0) AS TDS,
    ISNULL(e.DebitNoteAmnt, 0) AS DebitNoteAmnt,
    ISNULL(e.Remarks, '') AS Remarks,
    ISNULL(e.Loginname, '') AS RequestedBy,
    CAST(0 AS DECIMAL(18,2)) AS Outstanding,
    CAST(0 AS DECIMAL(18,2)) AS LedgerOSTAmt
FROM BillPaymentEntry e
WHERE {PendingEntryFilter}";

        if (amount != null)
        {
            sql += filterType switch
            {
                "lt" => " AND e.PaymentAmount < @Amount",
                "gt" => " AND e.PaymentAmount > @Amount",
                "eq" => " AND e.PaymentAmount = @Amount",
                "lte" => " AND e.PaymentAmount <= @Amount",
                "gte" => " AND e.PaymentAmount >= @Amount",
                _ => ""
            };
        }

        sql += " ORDER BY e.Sysdate DESC, e.PaymentNo DESC";

        return await connection.QueryAsync<PaymentRequestModel>(
            sql,
            new { UserName = username, Amount = amount });
    }

    public async Task<PaymentDetailsModel?> GetPaymentDetails(string paymentNo)
    {
        using var connection = _database.CreateConnection();

        const string sql = @"
SELECT TOP (1)
    e.PaymentNo,
    ISNULL(e.CompanyName, '') AS CompanyName,
    ISNULL(e.VendorName, '') AS VendorName,
    ISNULL(e.BillNo, '') AS BillNo,
    e.BillDate,
    ISNULL(e.MRNno, '') AS MRNNo,
    e.MRNDate,
    ISNULL(e.BillAmount, 0) AS BillAmount,
    ISNULL(e.PaymentTerms, '') AS PaymentTerms,
    ISNULL(e.PaymentAmount, 0) AS PaymentAmount,
    e.PaymentDate,
    ISNULL(e.Loginname, '') AS RequestedBy,
    ISNULL(e.Remarks, '') AS Remarks,
    ISNULL(e.PriorityLevel, '') AS PriorityLevel,
    ISNULL(e.LC, '') AS LC,
    ISNULL(e.UTRno, '') AS UTRNo,
    ISNULL(e.Currency, '') AS Currency,
    ISNULL(e.CurrencyRate, 0) AS CurrencyRate,
    ISNULL(e.TDS, 0) AS TDS,
    ISNULL(e.DebitNoteAmnt, 0) AS DebitNoteAmnt,
    ISNULL(e.PaymentBankName, '') AS PaymentBankName,
    ISNULL(e.PaymentBankAccNo, '') AS PaymentBankAccNo,
    ISNULL(e.SpeReq, 0) AS SpeReq,
    CAST(0 AS DECIMAL(18,2)) AS LedgerBalance,
    CAST(0 AS DECIMAL(18,2)) AS GroupBalance
FROM BillPaymentEntry e
WHERE e.PaymentNo = @PaymentNo";

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
ORDER BY ApprovalDate";

        return await connection.QueryAsync<PaymentHistoryModel>(
            sql,
            new { PaymentNo = paymentNo });
    }

    public async Task<bool> ApprovePayment(PaymentApprovalRequest request)
    {
        using var connection = _database.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            var approvalData = await connection.QueryFirstOrDefaultAsync<ApprovalData>(
                $@"
SELECT
    e.PaymentNo,
    @UserName AS ApprovalName,
    lr.email AS Email,
    ISNULL(alloc.authority, 0) AS Authority
FROM BillPaymentEntry e
LEFT JOIN loginentry..loginrights lr ON lr.NAME = e.Loginname
LEFT JOIN BillPaymentApprovalAllocation alloc
    ON alloc.ApprovalName = @UserName
WHERE e.PaymentNo = @PaymentNo
  AND {PendingEntryFilter}",
                new { request.PaymentNo, UserName = request.UserName },
                transaction);

            if (approvalData == null)
            {
                transaction.Rollback();
                return false;
            }

            await InsertHistoryAsync(connection, transaction, request, "Approved");

            if (approvalData.Authority == 1)
            {
                await connection.ExecuteAsync(
                    @"UPDATE BillPaymentEntry
                      SET status = 'Approved'
                      WHERE PaymentNo = @PaymentNo
                        AND status = 'Pending'",
                    new { request.PaymentNo },
                    transaction);

                await connection.ExecuteAsync(
                    @"UPDATE BillPaymentHODApproval
                      SET Status = 'Approved',
                          ApprovalDate = GETDATE()
                      WHERE PaymentNo = @PaymentNo
                        AND ApprovalName = @UserName
                        AND Status = 'Pending'",
                    new { request.PaymentNo, UserName = request.UserName },
                    transaction);

                if (!string.IsNullOrWhiteSpace(approvalData.Email))
                {
                    await _emailService.SendMail(
                        approvalData.Email,
                        $"Bill payment {request.PaymentNo} Approved",
                        $"Dear Sir,\n\n" +
                        $"Payment No: {request.PaymentNo}\n" +
                        $"Approved By: {request.UserName}\n" +
                        $"Remarks: {request.Comment}\n\n" +
                        $"Regards,\n" +
                        $"{request.UserName}");
                }
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

    public async Task<bool> RejectPayment(PaymentApprovalRequest request)
    {
        using var connection = _database.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            var rejectionData = await connection.QueryFirstOrDefaultAsync<ApprovalData>(
                $@"
SELECT
    e.PaymentNo,
    lr.email AS Email
FROM BillPaymentEntry e
LEFT JOIN loginentry..loginrights lr ON lr.NAME = e.Loginname
WHERE e.PaymentNo = @PaymentNo
  AND {PendingEntryFilter}",
                new { request.PaymentNo, UserName = request.UserName },
                transaction);

            if (rejectionData == null)
            {
                transaction.Rollback();
                return false;
            }

            await InsertHistoryAsync(connection, transaction, request, "Rejected");

            await connection.ExecuteAsync(
                @"UPDATE BillPaymentEntry
                  SET status = 'Rejected'
                  WHERE PaymentNo = @PaymentNo
                    AND status = 'Pending'",
                new { request.PaymentNo },
                transaction);

            await connection.ExecuteAsync(
                @"UPDATE BillPaymentHODApproval
                  SET Status = 'Rejected',
                      ApprovalDate = GETDATE(),
                      Comment = @Comment
                  WHERE PaymentNo = @PaymentNo
                    AND ApprovalName = @UserName
                    AND Status = 'Pending'",
                new
                {
                    request.PaymentNo,
                    request.UserName,
                    request.Comment
                },
                transaction);

            if (!string.IsNullOrWhiteSpace(rejectionData.Email))
            {
                await _emailService.SendMail(
                    rejectionData.Email,
                    $"Bill payment {request.PaymentNo} Rejected",
                    $"Dear Sir,\n\n" +
                    $"Payment No: {request.PaymentNo}\n" +
                    $"Rejected By: {request.UserName}\n" +
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

    private static async Task InsertHistoryAsync(
        System.Data.IDbConnection connection,
        System.Data.IDbTransaction transaction,
        PaymentApprovalRequest request,
        string status)
    {
        await connection.ExecuteAsync(
            @"
INSERT INTO BillPaymentEntryApproval
(
    PaymentNo, ApprovalName, ApprovalDate, comment, status,
    CompanyName, VendorName, BillNo, MRNno, PaymentAmount, Loginname, Remarks
)
SELECT
    e.PaymentNo,
    @UserName,
    GETDATE(),
    @Comment,
    @Status,
    e.CompanyName,
    e.VendorName,
    e.BillNo,
    e.MRNno,
    e.PaymentAmount,
    e.Loginname,
    e.Remarks
FROM BillPaymentEntry e
WHERE e.PaymentNo = @PaymentNo",
            new
            {
                request.PaymentNo,
                UserName = request.UserName,
                Comment = request.Comment ?? "",
                Status = status
            },
            transaction);
    }
}
