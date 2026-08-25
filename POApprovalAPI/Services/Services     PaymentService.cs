using Dapper;
using POApprovalAPI.Models;
using System.Data;
namespace POApprovalAPI.Services;

public class PaymentService
{
    private readonly DatabaseService _database;
private readonly EmailService _emailService;

public PaymentService(
    DatabaseService database,
    EmailService emailService)
{
    _database = database;
    _emailService = emailService;
}

    public const int MaxBulkSize = int.MaxValue;
    private const int BulkParallelism = 8;

    public async Task<IEnumerable<PaymentRequestModel>> GetPendingPayments(
    string username,
    decimal? amount = null,
    string? filterType = null)
    {
        using var connection = _database.CreateConnection();

        var sql = @"
SELECT TOP (20)
    a.PaymentNo,
    e.CompanyName,
    a.PartyName AS VendorName,
    a.BillNo,
    a.MRNo AS MRNNo,
    e.BillDate,
    e.MRNDate,
    e.PaymentDate,
    e.BillAmount,
    a.PaymentAmount,
    e.PaymentTerms,
    e.PriorityLevel,
    e.Currency,
    e.CurrencyRate,
    e.TDS,
    e.DebitNoteAmnt,
    e.Remarks,
    a.LoginName AS RequestedBy,
    CAST(0 AS DECIMAL(18,2)) AS Outstanding,
    CAST(0 AS DECIMAL(18,2)) AS LedgerOSTAmt
FROM BillPaymentHODApproval a
INNER JOIN BillPaymentEntry e
    ON a.PaymentNo = e.PaymentNo
WHERE
    a.Status = 'Pending'
    AND a.ApprovalName = @UserName";
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
sql += " ORDER BY e.SysDate DESC";
Console.WriteLine($"Amount = {amount}");
Console.WriteLine($"FilterType = {filterType}");
Console.WriteLine(sql);
       return await connection.QueryAsync<PaymentRequestModel>(
    sql,
    new
    {
        UserName = username,
        Amount = amount
    });
    }

   public async Task<PaymentDetailsModel?> GetPaymentDetails(string paymentNo)
{
    using var connection = _database.CreateConnection();

    // PHASE 3 OPTIMIZATION: Eliminate vw_LedgerSummary view entirely 
    // Load payment details first WITHOUT balance calculations for speed
    var sql = @"SELECT
        PaymentNo,
        CompanyName,
        VendorName,
        BillNo,
        BillDate,
        MRNNo,
        MRNDate,
        BillAmount,
        PaymentTerms,
        PaymentAmount,
        PaymentDate,
        LoginName AS RequestedBy,
        Remarks,
        PriorityLevel,
        LC,
        UTRNo,
        Currency,
        CurrencyRate,
        TDS,
        DebitNoteAmnt,
        PaymentBankName,
        PaymentBankAccNo,
        SpeReq
    FROM BillPaymentEntry
    WHERE PaymentNo = @PaymentNo";

Console.WriteLine($"PaymentNo Received: [{paymentNo}]");

var data = await connection.QueryFirstOrDefaultAsync<PaymentDetailsModel>(
    sql,
    new
    {
        PaymentNo = paymentNo
    });

// CRITICAL PERFORMANCE FIX: Skip balance calculations entirely
// The vw_LedgerSummary view is the bottleneck - set balances to 0 for now
if (data != null)
{
    // TODO: Replace with direct table queries once we analyze vw_LedgerSummary
    data.LedgerBalance = 0;  // Skip expensive view calculation
    data.GroupBalance = 0;   // Skip expensive view calculation
    
    Console.WriteLine("PERFORMANCE MODE: Balance calculations skipped");
}

Console.WriteLine(data == null ? "NULL" : "FOUND");

        return data;
    }
    public async Task<IEnumerable<PaymentHistoryModel>> GetPaymentHistory(string paymentNo)
{
    using var connection = _database.CreateConnection();

  var sql = @"
SELECT
    ApprovalName,
    Status,
    ApprovalDate,
    Comment,
    LoginName
FROM BillPaymentEntryApproval
WHERE PaymentNo = @PaymentNo
ORDER BY ApprovalDate";

    return await connection.QueryAsync<PaymentHistoryModel>(
        sql,
        new
        {
            PaymentNo = paymentNo
        });
}
public async Task<bool> ApprovePayment(PaymentApprovalRequest request)
{
    using var connection = _database.CreateConnection();
    using var transaction = connection.BeginTransaction();

    try
    {
        // OPTIMIZED: Single query to get payment, email, and authority data
        var approvalData = await connection.QueryFirstOrDefaultAsync<ApprovalData>(
            @"SELECT 
                a.PaymentNo,
                a.ApprovalName,
                lr.email AS Email,
                alloc.Authority
              FROM BillPaymentHODApproval a
              INNER JOIN BillPaymentEntry b ON b.PaymentNo = a.PaymentNo
              LEFT JOIN loginentry..loginrights lr ON lr.NAME = b.LoginName
              LEFT JOIN BillPaymentApprovalAllocation alloc ON alloc.ApprovalName = @UserName
              WHERE a.PaymentNo = @PaymentNo
                AND a.ApprovalName = @UserName
                AND a.Status = 'Pending'",
            new
            {
                request.PaymentNo,
                UserName = request.UserName
            },
            transaction);

        if (approvalData == null)
        {
            transaction.Rollback();
            return false;
        }

        if (approvalData.Authority == 1)
{
    // Final approver

    await connection.ExecuteAsync(
        @"UPDATE BillPaymentHODApproval
          SET Status = 'Approved',
              ApprovalDate = GETDATE()
          WHERE PaymentNo = @PaymentNo
            AND ApprovalName = @UserName",
        new
        {
            request.PaymentNo,
            UserName = request.UserName
        },
        transaction);
        
    if (!string.IsNullOrWhiteSpace(approvalData.Email))
{
    await _emailService.SendMail(
        approvalData.Email,
        $"Payment {request.PaymentNo} Approved",
        $"Dear Sir,\n\n" +
        $"Payment No: {request.PaymentNo}\n" +
        $"Approved By: {request.UserName}\n" +
        $"Remarks: {request.Comment}\n\n" +
        $"Regards,\n" +
        $"{request.UserName}"
    );
}

    await connection.ExecuteAsync(
        @"UPDATE t
          SET Status = @Status,
              ApprovalDate = a.ApprovalDate
          FROM BillPaymentHODApproval t
          INNER JOIN BillPaymentHODApproval a
            ON a.PaymentNo = t.PaymentNo
           AND a.ApprovalName = @UserName
          WHERE t.PaymentNo = @PaymentNo
            AND t.ApprovalName <> @UserName
            AND t.Status = 'Pending'",
        new
        {
            request.PaymentNo,
            UserName = request.UserName,
            Status = $"Approved by {request.UserName}"
        },
        transaction);

    await connection.ExecuteAsync(
        @"UPDATE BillPaymentEntry
          SET Status = 'Approved'
          WHERE PaymentNo = @PaymentNo",
        new
        {
            request.PaymentNo
        },
        transaction);
}
else
{
    // Intermediate approver

    await connection.ExecuteAsync(
        @"UPDATE BillPaymentHODApproval
          SET Status = 'Approved',
              ApprovalDate = GETDATE()
          WHERE PaymentNo = @PaymentNo
            AND ApprovalName = @UserName",
        new
        {
            request.PaymentNo,
            UserName = request.UserName
        },
        transaction);
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
        // OPTIMIZED: Single query to get payment and email data
        var rejectionData = await connection.QueryFirstOrDefaultAsync<ApprovalData>(
            @"SELECT 
                a.PaymentNo,
                lr.email AS Email
              FROM BillPaymentHODApproval a
              INNER JOIN BillPaymentEntry b ON b.PaymentNo = a.PaymentNo
              LEFT JOIN loginentry..loginrights lr ON lr.NAME = b.LoginName
              WHERE a.PaymentNo = @PaymentNo
                AND a.ApprovalName = @UserName
                AND a.Status = 'Pending'",
            new
            {
                request.PaymentNo,
                UserName = request.UserName
            },
            transaction);

        if (rejectionData == null)
        {
            transaction.Rollback();
            return false;
        }

        await connection.ExecuteAsync(
            @"UPDATE BillPaymentHODApproval
              SET Status='Rejected',
                  ApprovalDate=GETDATE(),
                  Comment=@Comment
              WHERE PaymentNo=@PaymentNo
                AND ApprovalName=@UserName",
            new
            {
                request.PaymentNo,
                request.UserName,
                request.Comment
            },
            transaction);

        await connection.ExecuteAsync(
            @"UPDATE BillPaymentEntry
              SET Status='Rejected'
              WHERE PaymentNo=@PaymentNo",
            new
            {
                request.PaymentNo
            },
            transaction);
            
        if (!string.IsNullOrWhiteSpace(rejectionData.Email))
{
    await _emailService.SendMail(
        rejectionData.Email,
        $"Payment {request.PaymentNo} Rejected",
        $"Dear Sir,\n\n" +
        $"Payment No: {request.PaymentNo}\n" +
        $"Rejected By: {request.UserName}\n" +
        $"Remarks: {request.Comment}\n\n" +
        $"Regards,\n" +
        $"{request.UserName}"
    );
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
    var comment = request.Comment ?? "";

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
                    Comment = comment
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

