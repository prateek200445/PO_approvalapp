using Dapper;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

public class AdvancePaymentService
{
    private readonly DatabaseService _database;

    public AdvancePaymentService(DatabaseService database)
    {
        _database = database;
    }

    public const int MaxBulkSize = int.MaxValue;
    private const int BulkParallelism = 8;

    public async Task<IEnumerable<AdvancePaymentRequestModel>> GetPendingPayments(
        string username,
        decimal? amount = null,
        string? filterType = null)
    {
        using var connection = _database.CreateConnection();

        var sql = @"
SELECT TOP (20)
    CAST(p.PaymentNo AS varchar(50)) AS PaymentNo,
    ISNULL(p.CompanyName, '') AS CompanyName,
    ISNULL(p.PaymentType, '') AS PaymentType,
    ISNULL(p.PaymentTYpeNo, '') AS PaymentTypeNo,
    ISNULL(p.PaymentAmt, 0) AS PaymentAmount,
    p.PaymentDate,
    ISNULL(p.Remarks, '') AS Remarks,
    ISNULL(p.PaymentRef, '') AS PaymentRef,
    ISNULL(p.BankCashPayment, '') AS BankCashPayment,
    ISNULL(p.Currency, '') AS Currency,
    ISNULL(CAST(p.exchangerate AS decimal(18, 4)), 0) AS ExchangeRate,
    ISNULL(p.paymentreqno, '') AS PaymentReqNo,
    ISNULL(p.LedgerFrom, '') AS LedgerFrom,
    ISNULL(p.LedgerTo, '') AS LedgerTo,
    ISNULL(p.vendorcode, '') AS VendorCode,
    ISNULL(p.ApprovalStatus, 0) AS ApprovalStatus
FROM Payment p
WHERE p.ApprovalStatus = 0
  AND (
        LOWER(ISNULL(p.PaymentTYpeNo, '')) LIKE '%advance%'
        OR LOWER(ISNULL(p.Remarks, '')) LIKE '%advance%'
      )
  AND EXISTS (
        SELECT 1
        FROM ApprovePaymentPlanAllocation a
        WHERE a.CompanyName = p.CompanyName
          AND a.username = @UserName
      )";

        if (amount != null)
        {
            sql += filterType switch
            {
                "lt" => " AND p.PaymentAmt < @Amount",
                "gt" => " AND p.PaymentAmt > @Amount",
                "eq" => " AND p.PaymentAmt = @Amount",
                "lte" => " AND p.PaymentAmt <= @Amount",
                "gte" => " AND p.PaymentAmt >= @Amount",
                _ => ""
            };
        }

        sql += " ORDER BY p.PaymentDate DESC, p.PaymentNo DESC";

        return await connection.QueryAsync<AdvancePaymentRequestModel>(
            sql,
            new
            {
                UserName = username,
                Amount = amount
            });
    }

    public async Task<AdvancePaymentDetailsModel?> GetPaymentDetails(string paymentNo)
    {
        using var connection = _database.CreateConnection();

        const string sql = @"
SELECT TOP (1)
    CAST(p.PaymentNo AS varchar(50)) AS PaymentNo,
    ISNULL(p.CompanyName, '') AS CompanyName,
    ISNULL(p.PaymentType, '') AS PaymentType,
    ISNULL(p.PaymentTYpeNo, '') AS PaymentTypeNo,
    ISNULL(p.PaymentAmt, 0) AS PaymentAmount,
    p.PaymentDate,
    ISNULL(p.Remarks, '') AS Remarks,
    ISNULL(p.PaymentRef, '') AS PaymentRef,
    ISNULL(p.BankCashPayment, '') AS BankCashPayment,
    ISNULL(p.Currency, '') AS Currency,
    ISNULL(CAST(p.exchangerate AS decimal(18, 4)), 0) AS ExchangeRate,
    ISNULL(p.paymentreqno, '') AS PaymentReqNo,
    ISNULL(p.LedgerFrom, '') AS LedgerFrom,
    ISNULL(p.LedgerTo, '') AS LedgerTo,
    ISNULL(p.vendorcode, '') AS VendorCode,
    ISNULL(p.ApprovalStatus, 0) AS ApprovalStatus,
    ISNULL(p.ChequeBankName, '') AS ChequeBankName,
    ISNULL(p.BankBranch, '') AS BankBranch,
    p.InstrumentDate,
    ISNULL(p.Amountwords, '') AS AmountWords,
    p.RecordLogId,
    p.companyId AS CompanyId,
    p.LedgerFromID AS LedgerFromId,
    p.LedgerToID AS LedgerToId
FROM Payment p
WHERE CAST(p.PaymentNo AS varchar(50)) = @PaymentNo
  AND (
        LOWER(ISNULL(p.PaymentTYpeNo, '')) LIKE '%advance%'
        OR LOWER(ISNULL(p.Remarks, '')) LIKE '%advance%'
      )";

        return await connection.QueryFirstOrDefaultAsync<AdvancePaymentDetailsModel>(
            sql,
            new
            {
                PaymentNo = paymentNo
            });
    }

    public async Task<IEnumerable<AdvancePaymentHistoryModel>> GetPaymentHistory(string paymentNo)
    {
        var payment = await GetPaymentDetails(paymentNo);
        if (payment == null)
            return Array.Empty<AdvancePaymentHistoryModel>();

        return new[]
        {
            new AdvancePaymentHistoryModel
            {
                ApprovalName = "Advance Payment",
                Status = payment.ApprovalStatus switch
                {
                    1 => "Approved",
                    2 => "Rejected",
                    _ => "Pending"
                },
                ApprovalDate = payment.PaymentDate,
                Comment = payment.Remarks,
                LoginName = payment.CompanyName
            }
        };
    }

    public async Task<bool> ApprovePayment(PaymentApprovalRequest request)
    {
        using var connection = _database.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            var allowed = await connection.QueryFirstOrDefaultAsync<int?>(
                @"
SELECT TOP (1) p.PaymentNo
FROM Payment p
WHERE p.PaymentNo = TRY_CONVERT(int, @PaymentNo)
  AND p.ApprovalStatus = 0
  AND (
        LOWER(ISNULL(p.PaymentTYpeNo, '')) LIKE '%advance%'
        OR LOWER(ISNULL(p.Remarks, '')) LIKE '%advance%'
      )
  AND EXISTS (
        SELECT 1
        FROM ApprovePaymentPlanAllocation a
        WHERE a.CompanyName = p.CompanyName
          AND a.username = @UserName
      )",
                new
                {
                    request.PaymentNo,
                    UserName = request.UserName
                },
                transaction);

            if (allowed == null)
            {
                transaction.Rollback();
                return false;
            }

            var rows = await connection.ExecuteAsync(
                @"
UPDATE Payment
SET ApprovalStatus = 1
WHERE PaymentNo = TRY_CONVERT(int, @PaymentNo)
  AND ApprovalStatus = 0",
                new
                {
                    request.PaymentNo
                },
                transaction);

            if (rows == 0)
            {
                transaction.Rollback();
                return false;
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
            var allowed = await connection.QueryFirstOrDefaultAsync<int?>(
                @"
SELECT TOP (1) p.PaymentNo
FROM Payment p
WHERE p.PaymentNo = TRY_CONVERT(int, @PaymentNo)
  AND p.ApprovalStatus = 0
  AND (
        LOWER(ISNULL(p.PaymentTYpeNo, '')) LIKE '%advance%'
        OR LOWER(ISNULL(p.Remarks, '')) LIKE '%advance%'
      )
  AND EXISTS (
        SELECT 1
        FROM ApprovePaymentPlanAllocation a
        WHERE a.CompanyName = p.CompanyName
          AND a.username = @UserName
      )",
                new
                {
                    request.PaymentNo,
                    UserName = request.UserName
                },
                transaction);

            if (allowed == null)
            {
                transaction.Rollback();
                return false;
            }

            var rows = await connection.ExecuteAsync(
                @"
UPDATE Payment
SET ApprovalStatus = 2
WHERE PaymentNo = TRY_CONVERT(int, @PaymentNo)
  AND ApprovalStatus = 0",
                new
                {
                    request.PaymentNo
                },
                transaction);

            if (rows == 0)
            {
                transaction.Rollback();
                return false;
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
}
